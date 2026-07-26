"use client";

import React, { useState, useEffect, useRef } from "react";
import {
  Terminal,
  Scan,
  DollarSign,
  Printer,
  Trash2,
  Activity,
  CheckCircle2,
  AlertCircle,
  Plus,
  Minus,
  Database,
  Zap
} from "lucide-react";
import { posApi } from "@/services/api/client";
import { formatCurrency } from "@/utils/formatters";

interface DraftLineItem {
  productId: number;
  sku: string;
  productName: string;
  quantity: number;
  unitPrice: number;
  lineTotal: number;
}

export default function PosTerminalPage() {
  const [draftOrderId, setDraftOrderId] = useState<number>(0);
  const [items, setItems] = useState<DraftLineItem[]>([]);
  const [barcodeInput, setBarcodeInput] = useState<string>("");
  const [shiftCash, setShiftCash] = useState<number>(5000000); // Initial register float balance
  const [cashReceived, setCashReceived] = useState<string>("");
  const [scanStatus, setScanStatus] = useState<string>("SYSTEM READY FOR BARCODE OR SKU INPUT");
  const [receiptPrinted, setReceiptPrinted] = useState<boolean>(false);
  const [isLoading, setIsLoading] = useState<boolean>(false);

  const inputRef = useRef<HTMLInputElement>(null);

  // Initialize live database-backed draft order on terminal boot
  useEffect(() => {
    const initializeDraftOrder = async () => {
      try {
        const savedOrderId = typeof window !== "undefined" ? Number(localStorage.getItem("digipose_active_draft_id") || 0) : 0;
        if (savedOrderId > 0) {
          try {
            const existingDraft = await posApi.getDraftOrder(savedOrderId);
            setDraftOrderId(existingDraft.orderId);
            setItems(existingDraft.items || []);
            setScanStatus(`>>> [SESSION_RECOVERED]: ACTIVE DRAFT ORDER #${existingDraft.orderId} SYNCED FROM DATABASE.`);
            return;
          } catch (e) {
            console.warn(">>> [DRAFT_SYNC]: Existing draft closed or expired, initializing fresh order.");
          }
        }

        const res = await posApi.createDraftOrder(1, 1, 1);
        if (res.orderId) {
          setDraftOrderId(res.orderId);
          if (typeof window !== "undefined") {
            localStorage.setItem("digipose_active_draft_id", String(res.orderId));
          }
          setScanStatus(`>>> [TERMINAL_INIT]: NEW DRAFT ORDER #${res.orderId} CREATED IN DATABASE.`);
        }
      } catch (err) {
        console.warn(">>> [LAN_OFFLINE]: Backend unreachable, enabling terminal local operational reserve.");
        setDraftOrderId(10001);
        setScanStatus(">>> [LOCAL_RESERVE]: OPERATING IN STANDALONE RESERVE MODE.");
      }
    };

    initializeDraftOrder();
    if (inputRef.current) inputRef.current.focus();
  }, []);

  const handleBarcodeScan = async (e: React.FormEvent) => {
    e.preventDefault();
    const cleanSku = barcodeInput.trim().toUpperCase();
    if (!cleanSku) return;

    setIsLoading(true);
    setScanStatus(`>>> [INVENTORY_QUERY]: VERIFYING SKU [${cleanSku}] IN DATABASE...`);

    try {
      // Step 1: Real-time SKU lookup in database
      const foundProduct = await posApi.lookupSku(cleanSku, 1);

      // Step 2: Push item into database-backed draft order
      const updateRes = await posApi.addItemToDraft(draftOrderId, foundProduct.productId, 1);
      
      if (updateRes && updateRes.items && updateRes.items.length > 0) {
        setItems(updateRes.items);
      } else {
        // Optimistic UI state sync
        setItems((prev) => {
          const idx = prev.findIndex((i) => i.productId === foundProduct.productId || i.sku.toUpperCase() === cleanSku);
          if (idx >= 0) {
            const copy = [...prev];
            copy[idx].quantity += 1;
            copy[idx].lineTotal = copy[idx].quantity * copy[idx].unitPrice;
            return copy;
          }
          return [
            ...prev,
            {
              productId: foundProduct.productId,
              sku: foundProduct.sku,
              productName: foundProduct.productName,
              quantity: 1,
              unitPrice: foundProduct.unitPrice,
              lineTotal: foundProduct.unitPrice,
            },
          ];
        });
      }

      setScanStatus(`>>> [ITEM_REGISTERED]: ${foundProduct.productName} ADDED TO ACTIVE DRAFT.`);
      setBarcodeInput("");
      setReceiptPrinted(false);
    } catch (err: any) {
      const errorMsg = err?.response?.data?.Error || err?.response?.data?.error;
      if (errorMsg === "OUT_OF_STOCK") {
        setScanStatus(`>>> [WARNING]: INSUFFICIENT STOCK FOR SKU [${cleanSku}]. TRANSACTION HELD.`);
        alert(`>>> [STOCK ALERT]: Product ${cleanSku} has insufficient balance in active branch warehouse!`);
      } else {
        setScanStatus(`>>> [CATALOG_FAULT]: SKU [${cleanSku}] NOT FOUND IN REPOSITORY OR SERVER OFFLINE.`);
      }
    } finally {
      setIsLoading(false);
      if (inputRef.current) inputRef.current.focus();
    }
  };

  const handleQtyAdjust = async (index: number, delta: number) => {
    const item = items[index];
    if (!item) return;

    try {
      if (delta > 0) {
        await posApi.addItemToDraft(draftOrderId, item.productId, 1);
      } else if (item.quantity <= 1) {
        await posApi.removeItemFromDraft(draftOrderId, item.productId);
      } else {
        // Adjust quantity in UI buffer when decrementing above 1
        setItems((prev) => {
          const copy = [...prev];
          copy[index].quantity += delta;
          copy[index].lineTotal = copy[index].quantity * copy[index].unitPrice;
          return copy;
        });
        return;
      }

      // Synchronize exact totals from server
      const draftData = await posApi.getDraftOrder(draftOrderId);
      if (draftData.items) setItems(draftData.items);
    } catch (e) {
      // Local state adjustment fallback
      setItems((prev) => {
        const copy = [...prev];
        const newQty = copy[index].quantity + delta;
        if (newQty <= 0) {
          return copy.filter((_, i) => i !== index);
        }
        copy[index].quantity = newQty;
        copy[index].lineTotal = newQty * copy[index].unitPrice;
        return copy;
      });
    }
  };

  const handleClearDraft = async () => {
    try {
      for (const item of items) {
        await posApi.removeItemFromDraft(draftOrderId, item.productId);
      }
    } catch (e) {
      console.warn(">>> [VOID_WARN]: Offline draft clearing applied.");
    }
    setItems([]);
    setScanStatus(">>> [ORDER_VOIDED]: CURRENT DRAFT ORDER CLEARED. TERMINAL RESET.");
    if (inputRef.current) inputRef.current.focus();
  };

  const grossTotal = items.reduce((acc, i) => acc + i.lineTotal, 0);
  const vatTax = grossTotal * 0.1;
  const grandTotal = grossTotal + vatTax;
  const customerChange = (Number(cashReceived) || 0) - grandTotal;

  const handleExecutePosCheckout = async () => {
    if (items.length === 0) {
      alert(">>> [OPERATION ERROR]: Cannot finalize payment on an empty order!");
      return;
    }

    setIsLoading(true);
    setScanStatus(">>> [SETTLEMENT]: COMMITTING FINANCIAL TRANSACTION & UPDATING INVENTORY LEDGER...");

    try {
      await posApi.checkoutPaid(draftOrderId, 1);
      
      setShiftCash((prev) => prev + grandTotal);
      setItems([]);
      setCashReceived("");
      setReceiptPrinted(true);
      
      if (typeof window !== "undefined") {
        localStorage.removeItem("digipose_active_draft_id");
      }

      // Automatically launch subsequent draft order for consecutive cashier operations
      try {
        const newDraft = await posApi.createDraftOrder(1, 1, 1);
        if (newDraft.orderId) {
          setDraftOrderId(newDraft.orderId);
          if (typeof window !== "undefined") {
            localStorage.setItem("digipose_active_draft_id", String(newDraft.orderId));
          }
        }
      } catch (e) {
        setDraftOrderId((prev) => prev + 1);
      }

      setScanStatus(">>> [TRANSACTION COMPLETE]: ELECTRONIC INVOICE ISSUED & SHIFT LEDGER UPDATED.");
    } catch (err: any) {
      const errMsg = err?.response?.data?.Error || "Communication error during settlement commit.";
      setScanStatus(`>>> [TRANSACTION REJECTED]: ${errMsg}`);
      alert(`>>> [CHECKOUT FAILED]: ${errMsg}`);
    } finally {
      setIsLoading(false);
      if (inputRef.current) inputRef.current.focus();
    }
  };

  return (
    <div className="space-y-6 max-w-7xl mx-auto">
      {/* TERMINAL HEADER RADAR */}
      <div className="cyber-panel-emerald flex flex-col md:flex-row items-start md:items-center justify-between gap-4 !p-6 border-l-4 border-l-[#00FF66]">
        <span className="reticle-tl">+</span>
        <span className="reticle-br">+</span>
        <div>
          <div className="flex items-center gap-2 text-[#00FF66] mb-1">
            <Terminal className="animate-bounce" size={24} />
            <h1 className="font-orbitron font-black text-2xl uppercase tracking-wider">
              ENTERPRISE POS RETAIL TERMINAL
            </h1>
          </div>
          <p className="font-mono text-sm text-[#777777]">
            [REGISTER_ID]: TERM_01 // BRANCH: MAIN_STORE // ACTIVE SHIFT FLOAT: <span className="text-[#00FF66] font-bold">{formatCurrency(shiftCash)}</span>
          </p>
        </div>

        {/* Status indicator */}
        <div className="flex items-center gap-3 bg-[#000000] border border-[#00FF66]/50 px-4 py-2 font-mono">
          <Activity className="text-[#00FF66] animate-pulse" size={20} />
          <div className="flex flex-col">
            <span className="text-xs text-[#00FF66]">ACTIVE DRAFT ORDER</span>
            <span className="text-lg font-black text-[#00E5FF]">#{draftOrderId || "CONNECTING..."}</span>
          </div>
        </div>
      </div>

      {/* BARCODE SCANNER FORM & DIAGNOSTIC BAR */}
      <div className="cyber-panel grid grid-cols-1 lg:grid-cols-4 gap-4 items-center !p-4 border-[#00FF66]/60 bg-[#0A0A0A]">
        <form onSubmit={handleBarcodeScan} className="lg:col-span-3 flex items-center gap-2">
          <div className="relative flex-1">
            <input
              ref={inputRef}
              type="text"
              value={barcodeInput}
              onChange={(e) => setBarcodeInput(e.target.value)}
              placeholder="SCAN BARCODE OR ENTER ASSET SKU HERE..."
              disabled={isLoading}
              className="cyber-input !pl-10 !py-3 !text-lg !font-bold text-[#00FF66] uppercase border-[#00FF66] disabled:opacity-50"
            />
            <Scan className="absolute left-3 top-3.5 text-[#00FF66]" size={22} />
          </div>
          <button type="submit" disabled={isLoading} className="btn-emerald !py-3 !px-6 font-orbitron font-bold uppercase disabled:opacity-50">
            <Zap size={18} />
            <span>SCAN ASSET</span>
          </button>
        </form>

        <div className="text-right font-mono text-xs text-[#777777] border-l border-[#00FF66]/30 pl-3">
          <span>AUDIBLE ALERT:</span> <span className="text-[#00FF66] font-bold">ACTIVE</span>
          <br />
          <span>RESILIENCE LEDGER:</span> <span className="text-[#00FF66] font-bold">DATABASE SYNCED</span>
        </div>
      </div>

      {/* SCAN TELEMETRY FEEDBACK */}
      <div className="bg-[#000000] border border-white/20 p-3 font-mono text-xs flex items-center justify-between">
        <span className="text-[#00E5FF]">{scanStatus}</span>
        {receiptPrinted && (
          <span className="text-[#00FF66] font-bold animate-pulse flex items-center gap-1">
            <Printer size={14} /> [ELECTRONIC INVOICE ISSUED & AUDIT RECORDED]
          </span>
        )}
      </div>

      {/* POS TERMINAL INTERFACE MATRIX */}
      <div className="grid grid-cols-1 lg:grid-cols-3 gap-6">
        {/* RETAIL DRAFT ITEMS LIST */}
        <div className="lg:col-span-2 cyber-panel !p-0 overflow-hidden border-[#00FF66]/40">
          <div className="p-3 bg-[#0A0A0A] border-b border-[#00FF66]/40 flex items-center justify-between font-orbitron text-xs text-[#00FF66]">
            <span>ACTIVE DRAFT ORDER LINE ITEMS</span>
            <span>TOTAL ITEMS: {items.reduce((a, b) => a + b.quantity, 0)}</span>
          </div>

          <div className="max-h-[420px] overflow-y-auto">
            {items.length === 0 ? (
              <div className="p-16 text-center font-mono text-[#777777] space-y-2">
                <Scan size={48} className="mx-auto text-[#00FF66]/40" />
                <div className="font-orbitron text-lg text-[#EEEEEE]">REGISTER DRAFT IS EMPTY</div>
                <p className="text-xs">Awaiting barcode scan or SKU entry from retail operator.</p>
              </div>
            ) : (
              <table className="cyber-table w-full">
                <thead>
                  <tr className="bg-[#000000] border-b border-white/20">
                    <th className="!p-3 text-[#00FF66]">SKU // DESCRIPTION</th>
                    <th className="!p-3 text-center text-[#00FF66]">QTY</th>
                    <th className="!p-3 text-right text-[#00FF66]">UNIT PRICE (VND)</th>
                    <th className="!p-3 text-right text-[#00FF66]">LINE TOTAL</th>
                    <th className="!p-3 text-center">DEL</th>
                  </tr>
                </thead>
                <tbody className="divide-y divide-white/10 font-mono text-sm">
                  {items.map((item, idx) => (
                    <tr key={item.sku + idx} className="hover:bg-[#00FF66]/5 transition-colors">
                      <td className="p-3">
                        <div className="font-bold text-[#EEEEEE]">{item.productName}</div>
                        <div className="text-xs text-[#00E5FF]">{item.sku}</div>
                      </td>
                      <td className="p-3 text-center">
                        <div className="inline-flex items-center gap-1 bg-[#000000] border border-[#00FF66]/40 px-2 py-0.5">
                          <button onClick={() => handleQtyAdjust(idx, -1)} disabled={isLoading} className="text-[#EEEEEE] hover:text-[#FF3333] disabled:opacity-50">
                            <Minus size={14} />
                          </button>
                          <span className="text-[#00FF66] font-bold w-6 text-center">{item.quantity}</span>
                          <button onClick={() => handleQtyAdjust(idx, 1)} disabled={isLoading} className="text-[#EEEEEE] hover:text-[#00FF66] disabled:opacity-50">
                            <Plus size={14} />
                          </button>
                        </div>
                      </td>
                      <td className="p-3 text-right text-[#EEEEEE]">{formatCurrency(item.unitPrice)}</td>
                      <td className="p-3 text-right font-bold text-[#00FF66]">{formatCurrency(item.lineTotal)}</td>
                      <td className="p-3 text-center">
                        <button onClick={() => handleQtyAdjust(idx, -item.quantity)} disabled={isLoading} className="text-[#777777] hover:text-[#FF3333] disabled:opacity-50">
                          <Trash2 size={16} />
                        </button>
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            )}
          </div>
        </div>

        {/* CASH SETTLEMENT & CHECKOUT CONTROLLER */}
        <div className="cyber-panel-emerald bg-[#0A0A0A] !p-6 space-y-5">
          <span className="reticle-tr">+</span>
          <span className="reticle-bl">+</span>

          <div className="flex items-center gap-2 text-[#00FF66] border-b border-[#00FF66]/40 pb-3">
            <DollarSign size={22} />
            <h2 className="font-orbitron font-black text-xl uppercase">SETTLEMENT CONTROLLER</h2>
          </div>

          {/* Totals Box */}
          <div className="bg-[#000000] p-4 border border-[#00FF66]/50 space-y-2 font-mono text-sm">
            <div className="flex justify-between text-[#777777]">
              <span>SUBTOTAL (GROSS):</span>
              <span className="text-[#EEEEEE]">{formatCurrency(grossTotal)}</span>
            </div>
            <div className="flex justify-between text-[#777777]">
              <span>ESTIMATED VAT (10%):</span>
              <span className="text-[#FFB000]">+ {formatCurrency(vatTax)}</span>
            </div>
            <div className="border-t border-[#00FF66] pt-2 flex justify-between items-baseline">
              <span className="font-orbitron font-bold text-lg text-[#00E5FF]">GRAND TOTAL:</span>
              <span className="font-mono font-black text-2xl text-[#00FF66]">
                {formatCurrency(grandTotal)}
              </span>
            </div>
          </div>

          {/* Cash received calculator */}
          <div className="space-y-2">
            <label className="block font-mono text-xs text-[#00FF66] font-bold uppercase">
              TENDERED AMOUNT (VND):
            </label>
            <input
              type="number"
              value={cashReceived}
              onChange={(e) => setCashReceived(e.target.value)}
              placeholder="ENTER AMOUNT TENDERED..."
              disabled={isLoading}
              className="cyber-input !py-2.5 !text-lg !font-bold text-[#EEEEEE] border-[#00FF66] disabled:opacity-50"
            />
            {Number(cashReceived) > 0 && (
              <div className="flex justify-between items-center p-2 bg-[#000000] border border-white/20 font-mono text-sm">
                <span className="text-[#777777]">CHANGE DUE:</span>
                <span className={`font-bold text-md ${customerChange >= 0 ? "text-[#00FF66]" : "text-[#FF3333]"}`}>
                  {formatCurrency(Math.max(0, customerChange))}
                </span>
              </div>
            )}
          </div>

          {/* Action buttons */}
          <div className="space-y-3 pt-3">
            <button
              onClick={handleExecutePosCheckout}
              disabled={items.length === 0 || isLoading}
              className="btn-emerald w-full !py-4 font-orbitron font-black text-lg flex items-center justify-center gap-2 uppercase shadow-[0_0_20px_rgba(0,255,102,0.6)] hover:shadow-[0_0_30px_rgba(0,255,102,1)] disabled:opacity-50"
            >
              <Printer size={22} />
              <span>{isLoading ? "PROCESSING..." : "COMPLETE TRANSACTION"}</span>
            </button>
            <button
              onClick={handleClearDraft}
              disabled={items.length === 0 || isLoading}
              className="btn-danger w-full !py-2 font-orbitron font-bold text-xs uppercase disabled:opacity-50"
            >
              <span>VOID ORDER // RESET TERMINAL</span>
            </button>
          </div>
        </div>
      </div>
    </div>
  );
}

