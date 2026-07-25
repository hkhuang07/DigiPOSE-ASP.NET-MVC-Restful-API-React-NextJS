"use client";

import React, { useEffect, useState } from "react";
import Link from "next/link";
import { ShoppingCart, Terminal, Activity, Radio, User, Zap, ShieldAlert } from "lucide-react";
import { useCartStore } from "@/store/cartStore";
import { useAuthStore } from "@/store/authStore";
import { getHudTimestamp, formatCurrency } from "@/utils/formatters";

export function CyberNavbar() {
  const { totalQuantity, totalPrice, cartState, getShoppingCart } = useCartStore();
  const { username, rewardPoints, syncIdentity } = useAuthStore();
  const [timestamp, setTimestamp] = useState("INITIALIZING TELEMETRY...");
  const [isMounted, setIsMounted] = useState(false);

  useEffect(() => {
    setIsMounted(true);
    syncIdentity();
    getShoppingCart();

    const timer = setInterval(() => {
      setTimestamp(getHudTimestamp());
    }, 100);

    return () => clearInterval(timer);
  }, []);

  return (
    <header className="sticky top-0 z-50 w-full bg-[#000000] border-b border-[#00E5FF] px-4 py-2 flex items-center justify-between shadow-[0_0_15px_rgba(0,229,255,0.3)] backdrop-blur-md">
      {/* Reticle markers */}
      <span className="reticle-tl">+</span>
      <span className="reticle-bl">+</span>

      {/* Left: System Brand & Telemetry Status */}
      <div className="flex items-center gap-6">
        <Link href="/" className="flex items-center gap-2 text-decoration-none">
          <Terminal className="text-cyan animate-pulse" size={26} />
          <div className="flex flex-col">
            <span className="font-orbitron font-black text-xl text-cyan tracking-wider">
              DIGIPOSE // <span className="text-emerald">CYBER.OS</span>
            </span>
            <span className="font-mono text-[10px] text-[#777777] tracking-widest">
              HYBRID ARCHITECTURE // PHASE 6.2 ONLINE
            </span>
          </div>
        </Link>

        {/* Vital Signs & Latency Indicator */}
        <div className="hidden md:flex items-center gap-4 border-l border-[#00E5FF] px-4 font-mono text-xs">
          <div className="flex items-center gap-1.5">
            <Radio className="text-emerald animate-ping" size={12} />
            <span className="text-emerald font-bold">O(1) LATENCY: 4.2ms</span>
          </div>
          <div className="flex items-center gap-1 text-[#FFB000]">
            <Activity size={14} />
            <span>BUFFER: NORMAL [██████░░░░]</span>
          </div>
        </div>
      </div>

      {/* Right: User Identity & Active Cart Radar */}
      <div className="flex items-center gap-4">
        {/* Timestamp */}
        <div className="hidden lg:block font-mono text-xs text-[#00E5FF] border border-[#00E5FF]/40 px-2 py-1 bg-[#0A0A0A]">
          {timestamp}
        </div>

        {/* User Badge & CRM Loyalty */}
        <div className="flex items-center gap-2 bg-[#0A0A0A] border border-[#00FF66]/40 px-3 py-1 font-mono text-xs">
          <User className="text-emerald" size={14} />
          <div className="flex flex-col">
            <span className="text-emerald font-bold">{isMounted ? username : "CLIENT"}</span>
            <span className="text-[10px] text-[#777777]">PTS: {isMounted ? rewardPoints : 0} CRM</span>
          </div>
        </div>

        {/* Shopping Cart Trigger (Phase 6.2 Rule: Card vs CardEmpty) */}
        <Link
          href="/cart"
          className={`btn-cyber flex items-center gap-2.5 !py-1.5 !px-3 ${
            cartState === "Card" ? "!border-[#00FF66] !text-[#00FF66]" : "!border-[#FF3333] !text-[#FF3333]"
          }`}
        >
          <ShoppingCart size={18} />
          <div className="flex flex-col items-start">
            <span className="font-orbitron font-bold text-xs">
              {cartState === "Card" ? "CARD" : "CARD EMPTY"}
            </span>
            <span className="font-mono text-[11px]">
              {isMounted && totalQuantity > 0 ? `${totalQuantity} ITEMS // ${formatCurrency(totalPrice)}` : "0 ITEMS"}
            </span>
          </div>
          {isMounted && totalQuantity > 0 ? (
            <Zap className="text-[#00FF66] animate-bounce ml-1" size={14} />
          ) : (
            <ShieldAlert className="text-[#FF3333] ml-1" size={14} />
          )}
        </Link>
      </div>
    </header>
  );
}
