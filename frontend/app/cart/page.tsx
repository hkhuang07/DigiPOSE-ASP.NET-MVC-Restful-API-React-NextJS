"use client";

import React, { useState } from "react";
import Link from "next/link";
import {
  ShoppingCart,
  Trash2,
  Plus,
  Minus,
  ShieldCheck,
  Zap,
  ArrowLeft,
  CreditCard,
  AlertOctagon,
  CheckCircle2,
  Lock,
} from "lucide-react";
import { useCartStore } from "@/store/cartStore";
import { useAuthStore } from "@/store/authStore";
import { formatCurrency } from "@/utils/formatters";

export default function ShoppingCartPage() {
  const {
    cartId,
    cartState,
    items,
    totalQuantity,
    grossPrice,
    totalTaxAmount,
    totalPrice,
    increaseProduct,
    decreaseProduct,
    deleteProduct,
    removeAllItems,
    checkout,
    isLoading,
  } = useCartStore();
  const { username, rewardPoints } = useAuthStore();

  const [paymentMethod, setPaymentMethod] = useState<number>(1); // 1 = Bank Wire / QR Pay, 2 = Corporate Credit, 3 = Cash on Delivery
  const [phone, setPhone] = useState<string>("");
  const [checkoutResult, setCheckoutResult] = useState<{ orderId: number; message: string } | null>(null);

  const handleExecuteCheckout = async () => {
    try {
      const res = await checkout(paymentMethod, undefined, phone || "0987654321");
      setCheckoutResult({ orderId: res.orderId, message: res.message });
    } catch (err) {
      alert(">>> [CHECKOUT_FAULT]: Unable to process transaction at this instant.");
    }
  };

  // SUCCESSFUL CHECKOUT STATE (RECEIVING E-INVOICE CONFIRMATION)
  if (checkoutResult) {
    return (
      <div className="max-w-3xl mx-auto mt-12 cyber-panel-emerald p-8 text-center space-y-6">
        <CheckCircle2 size={64} className="text-[#00FF66] mx-auto animate-bounce" />
        <div className="space-y-2">
          <span className="font-mono text-xs text-[#00E5FF] tracking-widest">
            >>> [ACID_TRANSACTION_COMMITTED]: SERIALIZABLE LOCK EXECUTED IN O(1)
          </span>
          <h1 className="font-orbitron font-black text-3xl text-[#00FF66] uppercase">
            CHECKOUT SUCCESSFUL // ORDER #{checkoutResult.orderId}
          </h1>
        </div>
        <p className="font-mono text-sm text-[#EEEEEE] bg-[#000000] p-4 border border-[#00FF66]/40">
          {checkoutResult.message}
          <br />
          <span className="text-[#00E5FF] font-bold mt-2 inline-block">
            * E-Invoice PDF and MailKit SMTP asynchronous dispatch queue triggered (&lt; 15ms latency).
          </span>
        </p>
        <div className="flex justify-center gap-4 pt-4">
          <Link href="/" className="btn-cyber !px-6">
            <ArrowLeft size={18} />
            <span>RETURN TO ONLINE STOREFRONT</span>
          </Link>
          <button
            onClick={() => setCheckoutResult(null)}
            className="btn-emerald !px-6 font-orbitron font-bold"
          >
            <span>NEW SHOPPING SESSION</span>
          </button>
        </div>
      </div>
    );
  }

  // RENDER STATE: 'CardEmpty' (EMPTY SHOPPING CART WARN)
  if (cartState === "CardEmpty" || totalQuantity === 0) {
    return (
      <div className="max-w-4xl mx-auto mt-8 cyber-panel-danger p-12 text-center space-y-6 border-l-4 border-l-[#FF3333]">
        <span className="reticle-tl">+</span>
        <span className="reticle-br">+</span>
        <AlertOctagon size={72} className="text-[#FF3333] mx-auto animate-pulse" />
        <div className="space-y-2">
          <span className="font-mono text-xs text-[#FF3333] tracking-widest">
            >>> [TELEMETRY_STATUS]: CART BUFFER DEPLETED
          </span>
          <h1 className="font-orbitron font-black text-4xl text-[#FF3333] uppercase">
            CARD EMPTY
          </h1>
        </div>
        <p className="font-mono text-md text-[#EEEEEE] max-w-lg mx-auto bg-[#000000] p-4 border border-[#FF3333]/40">
          No line item assets registered in your active session telemetry buffer.
          <br />
          <span className="text-[#777777] text-xs">
            * In accordance with Phase 6.2 domain isolation rules, unpopulated carts do not generate database records or accounting anomalies.
          </span>
        </p>
        <div className="pt-4">
          <Link href="/" className="btn-cyber !px-8 !py-3 !text-md">
            <ArrowLeft size={20} />
            <span>BROWSE ONLINE STOREFRONT CATALOG</span>
          </Link>
        </div>
      </div>
    );
  }

  // RENDER STATE: 'Card' (ACTIVE POPULATED CART MATRIX)
  return (
    <div className="max-w-6xl mx-auto space-y-6">
      {/* HUD Header */}
      <div className="cyber-panel flex items-center justify-between !p-6 border-l-4 border-l-[#00E5FF]">
        <span className="reticle-tl">+</span>
        <div>
          <div className="flex items-center gap-2 text-[#00E5FF] mb-1">
            <ShoppingCart size={22} />
            <h1 className="font-orbitron font-black text-2xl uppercase tracking-wider">
              SHOPPING CART RADAR // STATE: [ CARD ]
            </h1>
          </div>
          <p className="font-mono text-sm text-[#777777]">
            SESSION CLIENT: <span className="text-[#00FF66] font-bold">{username}</span> // LOYALTY POINTS: <span className="text-[#FFB000]">{rewardPoints} PTS</span>
          </p>
        </div>

        <button
          onClick={removeAllItems}
          disabled={isLoading}
          className="btn-danger !py-2 !px-4 text-xs flex items-center gap-2"
        >
          <Trash2 size={16} />
          <span>CLEAR ALL ITEMS (CLEAR CART)</span>
        </button>
      </div>

      <div className="grid grid-cols-1 lg:grid-cols-3 gap-6">
        {/* LINE ITEMS MATRIX TABLE */}
        <div className="lg:col-span-2 cyber-panel !p-0 overflow-hidden border-[#00E5FF]">
          <table className="cyber-table w-full">
            <thead>
              <tr className="bg-[#0A0A0A] border-b border-[#00E5FF] text-left">
                <th className="!p-4">SKU / ITEM NAME</th>
                <th className="!p-4 text-center">QUANTITY</th>
                <th className="!p-4 text-right">UNIT PRICE</th>
                <th className="!p-4 text-right">LINE TOTAL</th>
                <th className="!p-4 text-center">VOI</th>
              </tr>
            </thead>
            <tbody className="divide-y divide-white/10 font-mono text-sm">
              {items.map((item) => (
                <tr key={item.productId} className="hover:bg-[#00E5FF]/5 transition-colors">
                  <td className="p-4">
                    <div className="font-bold text-[#EEEEEE]">{item.productName}</div>
                    <div className="text-xs text-[#00E5FF]">{item.sku} // UNIT: {item.unitName}</div>
                  </td>

                  {/* Quantity controls (increaseProduct / decreaseProduct / updateQuantity) */}
                  <td className="p-4 text-center">
                    <div className="inline-flex items-center gap-2 bg-[#0A0A0A] border border-[#00E5FF]/50 px-2 py-1">
                      <button
                        onClick={() => decreaseProduct(item.productId)}
                        className="text-[#EEEEEE] hover:text-[#00E5FF] transition-colors p-1"
                        title="Decrease quantity"
                      >
                        <Minus size={14} />
                      </button>
                      <span className="font-bold text-md text-[#00FF66] w-8 text-center">
                        {item.quantity}
                      </span>
                      <button
                        onClick={() => increaseProduct(item.productId)}
                        className="text-[#EEEEEE] hover:text-[#00E5FF] transition-colors p-1"
                        title="Increase quantity"
                      >
                        <Plus size={14} />
                      </button>
                    </div>
                  </td>

                  <td className="p-4 text-right text-[#EEEEEE]">
                    {formatCurrency(item.unitPrice)}
                  </td>

                  <td className="p-4 text-right font-bold text-[#00FF66]">
                    {formatCurrency(item.lineTotal)}
                  </td>

                  {/* removeItem / deleteProduct */}
                  <td className="p-4 text-center">
                    <button
                      onClick={() => deleteProduct(item.productId)}
                      className="text-[#777777] hover:text-[#FF3333] transition-colors p-1"
                      title="Remove item"
                    >
                      <Trash2 size={16} />
                    </button>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>

        {/* SETTLEMENT & CHECKOUT TELEMETRY PANEL */}
        <div className="cyber-panel space-y-5 border-[#00FF66]/80 bg-[#0A0A0A] !p-6">
          <span className="reticle-tr">+</span>
          <span className="reticle-bl">+</span>

          <div className="flex items-center gap-2 text-[#00FF66] border-b border-[#00FF66]/30 pb-3">
            <CreditCard size={20} />
            <h2 className="font-orbitron font-bold text-lg uppercase">SETTLEMENT RADAR</h2>
          </div>

          {/* Financial Calculation (Gross, Tax, Discount, Total) */}
          <div className="space-y-2 font-mono text-sm">
            <div className="flex justify-between text-[#777777]">
              <span>GROSS PRICE:</span>
              <span className="text-[#EEEEEE]">{formatCurrency(grossPrice)}</span>
            </div>
            <div className="flex justify-between text-[#777777]">
              <span>VAT TAX (10%):</span>
              <span className="text-[#FFB000]">+ {formatCurrency(totalTaxAmount)}</span>
            </div>
            <div className="flex justify-between text-[#777777]">
              <span>CRM LOYALTY DISCOUNT:</span>
              <span className="text-[#00E5FF]">- 0.00 VND</span>
            </div>
            <div className="border-t border-[#00FF66]/40 pt-3 flex justify-between items-baseline">
              <span className="font-orbitron font-bold text-[#EEEEEE]">FINAL TOTAL:</span>
              <span className="font-mono font-black text-2xl text-[#00FF66]">
                {formatCurrency(totalPrice)}
              </span>
            </div>
          </div>

          {/* Payment Method Selector */}
          <div className="space-y-3 pt-2">
            <label className="block font-mono text-xs text-[#00E5FF] font-bold uppercase">
              SELECT PAYMENT METHOD:
            </label>
            <select
              value={paymentMethod}
              onChange={(e) => setPaymentMethod(Number(e.target.value))}
              className="cyber-select font-mono text-sm"
            >
              <option value="1">[01] BANK TRANSFER / QR VIETQR PAY</option>
              <option value="2">[02] CORPORATE B2B CREDIT LINE</option>
              <option value="3">[03] CASH AT RETAIL COUNTER / COD</option>
            </select>

            <label className="block font-mono text-xs text-[#00E5FF] font-bold uppercase pt-2">
              CONTACT PHONE (FOR E-INVOICE SMS/EMAIL):
            </label>
            <input
              type="text"
              value={phone}
              onChange={(e) => setPhone(e.target.value)}
              placeholder="E.G. 0987654321..."
              className="cyber-input font-mono text-sm"
            />
          </div>

          {/* Atomic Checkout Execute Button */}
          <div className="pt-4">
            <button
              onClick={handleExecuteCheckout}
              disabled={isLoading}
              className="btn-emerald w-full !py-3 font-orbitron font-black text-sm flex items-center justify-center gap-2 uppercase shadow-[0_0_15px_rgba(0,255,102,0.4)] hover:shadow-[0_0_25px_rgba(0,255,102,0.8)]"
            >
              <Lock size={18} />
              <span>EXECUTE ATOMIC CHECKOUT</span>
            </button>
            <div className="text-center font-mono text-[10px] text-[#777777] mt-2 flex items-center justify-center gap-1">
              <ShieldCheck size={12} className="text-[#00E5FF]" />
              <span>WRAPPED IN EF CORE 8 SERIALIZABLE TRANSACTION</span>
            </div>
          </div>
        </div>
      </div>
    </div>
  );
}
