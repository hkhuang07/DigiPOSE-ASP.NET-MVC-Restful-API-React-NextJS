// ====================================================================
// [POS CLIENT CHECKOUT & ASYNCHRONOUS EDGE RESILIENCE ENGINE]
// Target: React / Next.js / Vite TypeScript client running in LAN POS terminal
// Architecture: Asynchronous IndexedDB via Dexie.js (Zero Main-Thread Blocking, Unlimited Quota)
// ====================================================================
import { v4 as uuidv4 } from 'uuid';
import Dexie, { type Table } from 'dexie';

export interface CheckoutResponseDto {
  orderId: number;
  invoiceNumber: string;
  processedAt: string;
  isReplay: boolean;
  liveStockBalances: Record<number, number>;
}

export interface CheckoutRequest {
  orderId: number;
  paymentMethodId: number;
  customerId?: number;
  idempotencyKey: string;
}

// >>> [ENTERPRISE INDEXEDDB SCHEMA]: Asynchronous offline transactional queue storage
class PosOfflineDatabase extends Dexie {
  checkoutQueue!: Table<CheckoutRequest, string>; // Primary key is idempotencyKey

  constructor() {
    super('DigiPOSE_Offline_Database');
    this.version(1).stores({
      checkoutQueue: 'idempotencyKey, orderId, paymentMethodId',
    });
  }
}

const db = new PosOfflineDatabase();

/**
 * Persists failed checkout transactions asynchronously without blocking browser UI rendering thread.
 */
async function enqueueOfflineRequest(request: CheckoutRequest): Promise<void> {
  try {
    await db.checkoutQueue.put(request);
    console.warn(`>>> [DEXIE_BUFFER_SAVED]: Order #${request.orderId} committed to asynchronous IndexedDB edge storage.`);
  } catch (error) {
    console.error('>>> [INDEXEDDB_FAULT]: Failed to store transaction offline:', error);
  }
}

/**
 * Removes an successfully synced order from IndexedDB offline storage.
 */
async function dequeueOfflineRequest(idempotencyKey: string): Promise<void> {
  await db.checkoutQueue.delete(idempotencyKey);
}

/**
 * Executes resilient checkout to ASP.NET Core API with preserved client UUID and asynchronous fallback.
 */
export async function executePosCheckout(
  orderId: number,
  paymentMethodId: number,
  customerId?: number
): Promise<CheckoutResponseDto | null> {
  const idempotencyKey = uuidv4();
  const payload: CheckoutRequest = { orderId, paymentMethodId, customerId, idempotencyKey };

  if (!navigator.onLine) {
    console.warn(`>>> [OFFLINE_INTERCEPTION]: Terminal disconnected. Diverting order #${orderId} directly to Dexie IndexedDB queue.`);
    await enqueueOfflineRequest(payload);
    return null;
  }

  const maxRetries = 3;
  let attempt = 0;

  while (attempt < maxRetries) {
    attempt++;
    try {
      console.log(`>>> [CHECKOUT_INIT]: Transmitting payload (Attempt ${attempt}/${maxRetries}). Key: ${idempotencyKey}`);
      
      const response = await fetch('http://localhost:5000/api/v1/pos/checkout/paid', {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
          'Authorization': `Bearer ${localStorage.getItem('pos_jwt_token') || ''}`
        },
        body: JSON.stringify(payload),
      });

      if (!response.ok) {
        const errData = await response.json().catch(() => ({ Error: 'Server rejected transaction' }));
        throw new Error(`Server Rejected: ${JSON.stringify(errData)}`);
      }

      const data: CheckoutResponseDto = await response.json();
      
      if (data.isReplay) {
        console.warn(`>>> [O(1)_REPLAY_CAUGHT]: Server returned idempotent reply for Order #${data.orderId}. Zero double-billing!`);
      } else {
        console.log(`>>> [CHECKOUT_SUCCESS]: Invoice ${data.invoiceNumber} established cleanly.`);
      }

      updateLocalInventoryCache(data.liveStockBalances);
      return data;

    } catch (error) {
      console.error(`>>> [NETWORK_FAULT]: Attempt ${attempt} interrupted. Error:`, error);
      
      if (attempt >= maxRetries) {
        console.error('>>> [MAX_RETRIES_EXHAUSTED]: Persisting transaction to asynchronous Dexie IndexedDB.');
        await enqueueOfflineRequest(payload);
        return null;
      }
      
      // Exponential backoff retaining exact same idempotencyKey
      await new Promise((res) => setTimeout(res, 250 * Math.pow(2, attempt)));
    }
  }

  return null;
}

function updateLocalInventoryCache(stockDelta: Record<number, number>) {
  console.log('>>> [UI_SYNC]: Synchronizing terminal stock display via DOM Event O(1):', stockDelta);
  window.dispatchEvent(new CustomEvent('pos:inventory-updated', { detail: stockDelta }));
}

// >>> [EDGE_CONNECTIVITY_MONITOR]: Automatic background recovery from IndexedDB when LAN restores
if (typeof window !== 'undefined') {
  window.addEventListener('online', async () => {
    console.log('>>> [LAN_RESTORED]: Network connectivity detected. Sweeping Dexie IndexedDB queue...');
    const queue = await db.checkoutQueue.toArray();
    if (queue.length === 0) {
      console.log('>>> [SYNC_COMPLETE]: Offline IndexedDB queue is clean.');
      return;
    }

    for (const item of queue) {
      try {
        console.log(`>>> [AUTO_SYNC_RETRY]: Replaying Order #${item.orderId} (Key: ${item.idempotencyKey})...`);
        const response = await fetch('http://localhost:5000/api/v1/pos/checkout/paid', {
          method: 'POST',
          headers: {
            'Content-Type': 'application/json',
            'Authorization': `Bearer ${localStorage.getItem('pos_jwt_token') || ''}`
          },
          body: JSON.stringify(item),
        });

        if (response.ok) {
          const result: CheckoutResponseDto = await response.json();
          await dequeueOfflineRequest(item.idempotencyKey);
          updateLocalInventoryCache(result.liveStockBalances);
          console.log(`>>> [SYNC_SUCCESS]: Order #${item.orderId} restored cleanly from Dexie offline cache.`);
        }
      } catch (e) {
        console.error(`>>> [SYNC_STALL]: Order #${item.orderId} retained in Dexie DB for next recovery heartbeat.`);
      }
    }
  });

  window.addEventListener('offline', () => {
    console.warn('>>> [LAN_SEVERED]: Connection severed. Terminal switched to Dexie Asynchronous Offline Buffer Mode.');
  });
}
