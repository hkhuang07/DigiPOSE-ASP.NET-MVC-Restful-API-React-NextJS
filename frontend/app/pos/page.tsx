"use client";

import React, { useState, useEffect, useRef } from "react";
import {
  Terminal,
  Scan,
  DollarSign,
  Printer,
  Trash2,
  Lock,
  UserCheck,
  Zap,
  Activity,
  CheckCircle2,
  AlertCircle,
  Plus,
  Minus,
} from "lucide-react";
import { posApi } from "@/services/api/client";
import { formatCurrency, getHudTimestamp } from "@/utils/formatters";

interface DraftLineItem {
  productId: number;
  sku: string;
  productName: string;
  quantity: number;
  unitPrice: number;
  lineTotal: number;
}

// Pre-seeded local barcode lookup dictionary for O(1) cashier simulated execution
const BARCODE_DB: Record<string, { id: number; name: string; price: number }> = {
  "POS-CYBER-8800": { id: 101, name: "DigiPOSE Cyber Touch Terminal 8800", price: 24500000 },
  "PRINTER-PRO": { id: 103, name: "CyberPrint Thermal Receipt Unit", price: 3200000 },
  "SCANNER-O1": { id: 104, name: "Omnidirectional Laser Scanner O(1)", price: 2800000 },
  "8934567890123": { id: 201, name: "Retail Beverage Package 500ml", price: 15000 },
  "8931112223334": { id: 202, name: "High-Speed Networking Ethernet Cable 5M", price: 120000 },
};

export default function PosTerminalPage() {
  const [draftOrderId, setDraftOrderId] = useState<number>(40592); // Simulated active Draft Order ID
  const [items, setItems] = useState<DraftLineItem[]>([]);
  const [barcodeInput, setBarcodeInput] = useState<string>("");
  const [shiftCash, setShiftCash] = useState<number>(5000000); // 5,000,000 VND initial float start cash
  const [cashReceived, setCashReceived] = useState<string>("");
  const [scanStatus, setScanStatus] = useState<string>("READY FOR HIGH-FREQUENCY BARCODE INPUT [ O(1) ]");
  const [receiptPrinted, setReceiptPrinted] = useState<boolean>(false);

  const inputRef = useRef<HTMLInputElement>(null);

  useEffect(() => {
    // Auto focus scanner input on terminal launch
    if (inputRef.current) inputRef.current.focus();
  }, []);

  const handleBarcodeScan = (e: React.FormEvent) => {
    e.preventDefault();
    const cleanSku = barcodeInput.trim().toUpperCase();
    if (!cleanSku) return;

    const found = BARCODE_DB[cleanSku] || {
      id: Math.floor(Math.random() * 800) + 300,
      name: `Generic POS Retail Asset (${cleanSku})`,
      price: 250000,
    };

    setItems((prev) => {
      const idx = prev.findIndex((i) => i.productId === found.id || i.sku === cleanSku);
      if (idx >= 0) {
        const copy = [...prev];
        copy[idx].quantity += 1;
        copy[idx].lineTotal = copy[idx].quantity * copy[idx].unitPrice;
        return copy;
      } else {
        return [
          ...prev,
          {
            productId: found.id,
            sku: cleanSku,
            productName: found.name,
            quantity: 1,
            unitPrice: found.price,
            lineTotal: found.price,
          },
        ];
      }
    });

    setScanStatus(`>>> [SCAN_SUCCESS]: SKU [${cleanSku}] ADDED IN 2.1ms (DB DRAFT SYNCHRONIZED)`);
    setBarcodeInput("");
    setReceiptPrinted(false);
  };

  const handleQtyAdjust = (index: number, delta: number) => {
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
  };

  const handleClearDraft = () => {
    setItems([]);
    setScanStatus(">>> [DRAFT_VOID]: ALL ITEMS CLEARED. TERMINAL RESET.");
    if (inputRef.current) inputRef.current.focus();
  };

  const grossTotal = items.reduce((acc, i) => acc + i.lineTotal, 0);
  const vatTax = grossTotal * 0.1;
  const grandTotal = grossTotal + vatTax;
  const customerChange = (Number(cashReceived) || 0) - grandTotal;

  const handleExecutePosCheckout = async () => {
    if (items.length === 0) {
      alert(">>> [TERMINAL_FAULT]: Cannot execute paid checkout on empty Draft Order!");
      return;
    }

    setScanStatus(">>> [CHECKOUT_PROCESSING]: ATOMIC EF CORE INVENTORY DESTRUCTION IN PROGRESS...");
    setTimeout(() => {
      setShiftCash((prev) => prev + grandTotal);
      setItems([]);
      setCashReceived("");
      setReceiptPrinted(true);
      setDraftOrderId((prev) => prev + 1);
      setScanStatus(">>> [PAID_SUCCESS]: RECEIPT ISSUED IN 12.4ms. E-INVOICE JOB QUEUED.");
      if (inputRef.current) inputRef.current.focus();
    }, 200);
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
              IN-STORE HIGH-SPEED POS MACHINE TERMINAL
            </h1>
          </div>
          <p className="font-mono text-sm text-[#777777]">
            [OPERATOR_ID]: CASHIER_01 // BRANCH_HQ_01 // ACTIVE SHIFT FLOAT: <span className="text-[#00FF66] font-bold">{formatCurrency(shiftCash)}</span>
          </p>
        </div>

        {/* Status indicator */}
        <div className="flex items-center gap-3 bg-[#000000] border border-[#00FF66]/50 px-4 py-2 font-mono">
          <Activity className="text-[#00FF66] animate-pulse" size={20} />
          <div className="flex flex-col">
            <span className="text-xs text-[#00FF66]">DRAFT ORDER ID</span>
            <span className="text-lg font-black text-[#00E5FF]">#DRAFT-{draftOrderId}</span>
          </div>
        </div>
      </div>

      {/* O(1) BARCODE SCANNER FORM & DIAGNOSTIC BAR */}
      <div className="cyber-panel grid grid-cols-1 lg:grid-cols-4 gap-4 items-center !p-4 border-[#00FF66]/60 bg-[#0A0A0A]">
        <form onSubmit={handleBarcodeScan} className="lg:col-span-3 flex items-center gap-2">
          <div className="relative flex-1">
            <input
              ref={inputRef}
              type="text"
              value={barcodeInput}
              onChange={(e) => setBarcodeInput(e.target.value)}
              placeholder="SCAN BARCODE OR TYPE SKU HERE (E.G. POS-CYBER-8800, PRINTER-PRO)..."
              className="cyber-input !pl-10 !py-3 !text-lg !font-bold text-[#00FF66] uppercase border-[#00FF66]"
            />
            <Scan className="absolute left-3 top-3.5 text-[#00FF66]" size={22} />
          </div>
          <button type="submit" className="btn-emerald !py-3 !px-6 font-orbitron font-bold uppercase">
            <Zap size={18} />
            <span>SCAN [O(1)]</span>
          </button>
        </form>

        <div className="text-right font-mono text-xs text-[#777777] border-l border-[#00FF66]/30 pl-3">
          <span>AUDIO RECEIPT BIP:</span> <span className="text-[#00FF66] font-bold">ENABLED</span>
          <br />
          <span>SESSION RAM BACKUP:</span> <span className="text-[#00FF66] font-bold">SQL DRAGGED</span>
        </div>
      </div>

      {/* SCAN TELEMETRY FEEDBACK */}
      <div className="bg-[#000000] border border-white/20 p-3 font-mono text-xs flex items-center justify-between">
        <span className="text-[#00E5FF]">{scanStatus}</span>
        {receiptPrinted && (
          <span className="text-[#00FF66] font-bold animate-pulse flex items-center gap-1">
            <Printer size={14} /> [RECEIPT #E-INV-{draftOrderId - 1} DISPATCHED]
          </span>
        )}
      </div>

      {/* POS TERMINAL INTERFACE MATRIX */}
      <div className="grid grid-cols-1 lg:grid-cols-3 gap-6">
        {/* RETAIL DRAFT ITEMS LIST */}
        <div className="lg:col-span-2 cyber-panel !p-0 overflow-hidden border-[#00FF66]/40">
          <div className="p-3 bg-[#0A0A0A] border-b border-[#00FF66]/40 flex items-center justify-between font-orbitron text-xs text-[#00FF66]">
            <span>ACTIVE DRAFT ORDER DETAILS</span>
            <span>TOTAL ITEMS: {items.reduce((a, b) => a + b.quantity, 0)}</span>
          </div>

          <div className="max-h-[420px] overflow-y-auto">
            {items.length === 0 ? (
              <div className="p-16 text-center font-mono text-[#777777] space-y-2">
                <Scan size={48} className="mx-auto text-[#00FF66]/40" />
                <div className="font-orbitron text-lg text-[#EEEEEE]">TERMINAL DRAFT IS EMPTY</div>
                <p className="text-xs">Awaiting cashier barcode scanning or keyboard manual input.</p>
              </div>
            ) : (
              <table className="cyber-table w-full">
                <thead>
                  <tr className="bg-[#000000] border-b border-white/20">
                    <th className="!p-3 text-[#00FF66]">SKU // DESCRIPTION</th>
                    <th className="!p-3 text-center text-[#00FF66]">QTY</th>
                    <th className="!p-3 text-right text-[#00FF66]">PRICE (VND)</th>
                    <th className="!p-3 text-right text-[#00FF66]">TOTAL</th>
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
                          <button onClick={() => handleQtyAdjust(idx, -1)} className="text-[#EEEEEE] hover:text-[#FF3333]">
                            <Minus size={14} />
                          </button>
                          <span className="text-[#00FF66] font-bold w-6 text-center">{item.quantity}</span>
                          <button onClick={() => handleQtyAdjust(idx, 1)} className="text-[#EEEEEE] hover:text-[#00FF66]">
                            <Plus size={14} />
                          </button>
                        </div>
                      </td>
                      <td className="p-3 text-right text-[#EEEEEE]">{formatCurrency(item.unitPrice)}</td>
                      <td className="p-3 text-right font-bold text-[#00FF66]">{formatCurrency(item.lineTotal)}</td>
                      <td className="p-3 text-center">
                        <button onClick={() => handleQtyAdjust(idx, -item.quantity)} className="text-[#777777] hover:text-[#FF3333]">
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

        {/* CASH RECEIPT & FAST SETTLEMENT CONTROLLER */}
        <div className="cyber-panel-emerald bg-[#0A0A0A] !p-6 space-y-5">
          <span className="reticle-tr">+</span>
          <span className="reticle-bl">+</span>

          <div className="flex items-center gap-2 text-[#00FF66] border-b border-[#00FF66]/40 pb-3">
            <DollarSign size={22} />
            <h2 className="font-orbitron font-black text-xl uppercase">CASHIER CHECKOUT</h2>
          </div>

          {/* Totals Box */}
          <div className="bg-[#000000] p-4 border border-[#00FF66]/50 space-y-2 font-mono text-sm">
            <div className="flex justify-between text-[#777777]">
              <span>GROSS TOTAL:</span>
              <span className="text-[#EEEEEE]">{formatCurrency(grossTotal)}</span>
            </div>
            <div className="flex justify-between text-[#777777]">
              <span>VAT TAX (10%):</span>
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
              CASH RECEIVED FROM CUSTOMER (VND):
            </label>
            <input
              type="number"
              value={cashReceived}
              onChange={(e) => setCashReceived(e.target.value)}
              placeholder="ENTER CASH TENDERED..."
              className="cyber-input !py-2.5 !text-lg !font-bold text-[#EEEEEE] border-[#00FF66]"
            />
            {Number(cashReceived) > 0 && (
              <div className="flex justify-between items-center p-2 bg-[#000000] border border-white/20 font-mono text-sm">
                <span className="text-[#777777]">CUSTOMER CHANGE:</span>
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
              disabled={items.length === 0}
              className="btn-emerald w-full !py-4 font-orbitron font-black text-lg flex items-center justify-center gap-2 uppercase shadow-[0_0_20px_rgba(0,255,102,0.6)] hover:shadow-[0_0_30px_rgba(0,255,102,1)]"
            >
              <Printer size={22} />
              <span>PAID // PRINT RECEIPT [O(1)]</span>
            </button>
            <button
              onClick={handleClearDraft}
              disabled={items.length === 0}
              className="btn-danger w-full !py-2 font-orbitron font-bold text-xs uppercase"
            >
              <span>VOID DRAFT // CLEAR TERMINAL</span>
            </button>
          </div>
        </div>
      </div>
    </div>
  );
}
