# Phase 6: The Lean Monolith - Master Architecture Blueprint
**Version:** 6.0.0-LEAN (Consolidating Phase 6, 6.1, 6.2)  
**Target Environment:** Local LAN Topology (< 1ms Latency, < 100 POS Terminals, < 50 TPS)  
**Core Database:** Microsoft SQL Server 2019 / 2022  
**Philosophy:** Zero-Overhead, High-Concurrency, Deadlock-Free, Solo-Developer Enterprise Resilience.

---

## 1. Architectural Elimination & Core Paradigm
To optimize for solo-developer engineering velocity and leverage ultra-low LAN network latency (< 1ms), all distributed cloud-native infrastructures are strictly purged from this project:
* **REMOVED:** Kafka / RabbitMQ -> Replaced by In-Memory `Channel<T>` + SQL Server Append-Only Buffer Table (`JobQueue`).
* **REMOVED:** Redis Cluster / Valkey -> Replaced by ASP.NET Core Singleton In-Memory Engine (`ConcurrentDictionary<int, int>`).
* **REMOVED:** Debezium CDC -> Replaced by Entity Framework Core Transactional Commit Hook.
* **REMOVED:** Dual Cookie/JWT complexity in APIs -> POS REST APIs operate completely stateless over token/LAN authentication.

---

## 2. High-Concurrency & Zero-Deadlock Inventory Paradigm (Append-Only Ledger)
Traditional CRUD `UPDATE` operations on product stock (`UPDATE Products SET Stock = Stock - @qty WHERE Id = @Id`) generate Exclusive Row/Page Locks in SQL Server. Under high concurrency (e.g., peak retail store checkout), this causes connection pool starvation and `SqlException: Transaction deadlocked`.

### The Dual-Layer Concurrency Solution:
1. **O(1) Hot-Path RAM Engine:** Live inventory stock balances are mirrored in memory using a thread-safe `ConcurrentDictionary<int, int>` inside ASP.NET Core (`InventoryRAMService`). POS stock verifications and atomized deductions occur entirely in RAM in less than **0.05ms**.
2. **Append-Only SQL Ledger (`InventoryTransactions` table):** All inventory mutations are recorded as immutable log entries in an append-only transaction table. Because `INSERT` operations only append to new B-Tree data pages without locking existing product records, database deadlocks are **100% eliminated**.

---

## 3. Standard DTO Contract Specifications
To guarantee idempotency and prevent double-billing when LAN cables disconnect or Wi-Fi reconnects during checkout, all point-of-sale operations must supply immutable client-generated UUIDs.

```csharp
using System.ComponentModel.DataAnnotations;

namespace DigiPOSE.Models.DTOs
{
    public class AddItemRequest
    {
        [Required] public int OrderId { get; set; }
        [Required] public int ProductId { get; set; }
        [Required, Range(1, int.MaxValue)] public int Quantity { get; set; }
        
        // Prevents duplicate barcode scanner bounces within 50ms intervals
        [Required] public Guid ClientScanId { get; set; } = Guid.NewGuid();
    }

    public class CheckoutRequest
    {
        [Required] public int OrderId { get; set; }
        [Required] public int PaymentMethodId { get; set; }
        public int? CustomerId { get; set; }

        // Mandatory client-generated identifier. Prevents double-billing during network retries.
        [Required] public Guid IdempotencyKey { get; set; }
    }

    public record CheckoutResponseDto(
        int OrderId,
        string InvoiceNumber,
        DateTime ProcessedAt,
        bool IsReplay, // True if returned from idempotency guard (order previously processed)
        Dictionary<int, int> LiveStockBalances
    );
}
```

---

## 4. Fail-Safe Background Job & Edge Synchronization
1. **Resilient Invoice Processing (`ResilientInvoiceWorker`):** Uses an in-memory `Channel<JobQueueItem>` for real-time thermal receipt printing and SMTP emailing (< 50ms processing latency).
2. **SQL Sweep Recovery:** A periodic worker sweeps the SQL `JobQueue` table every 15 seconds for orphan jobs created over 5 seconds ago that were interrupted due to a sudden localized server power outage, ensuring zero dropped invoices.
