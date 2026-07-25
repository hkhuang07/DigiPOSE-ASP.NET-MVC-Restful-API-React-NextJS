"use client";

import React from "react";
import Link from "next/link";
import { usePathname } from "next/navigation";
import {
  Terminal,
  Store,
  ShoppingCart,
  ShieldCheck,
  Server,
  Database,
  Cpu,
  Link as LinkIcon,
  ExternalLink,
  ChevronRight,
} from "lucide-react";

export function CyberSidebar() {
  const pathname = usePathname();

  return (
    <aside className="w-64 bg-[#000000] border-r border-[#00E5FF] min-h-[calc(100vh-65px)] flex flex-col justify-between p-3 shrink-0 shadow-[2px_0_15px_rgba(0,229,255,0.15)] relative">
      <span className="reticle-tr">+</span>
      <span className="reticle-br">+</span>

      {/* Navigation Modules */}
      <div className="space-y-6">
        {/* MODULE 5: LINKS & TERMINALS (DUAL SALES CHANNELS) */}
        <div>
          <div className="flex items-center gap-2 font-orbitron text-xs font-bold text-[#00FF66] border-b border-[#00FF66]/30 pb-1 mb-2 tracking-wider">
            <LinkIcon size={14} />
            <span>LINKS & TERMINALS</span>
          </div>

          <ul className="space-y-2">
            {/* Launch POS Machine (In-Store Channel 1) */}
            <li>
              <Link
                href="/pos"
                className={`flex items-center justify-between p-2 border transition-all duration-200 ${
                  pathname === "/pos"
                    ? "bg-[#00FF66]/20 border-[#00FF66] text-[#00FF66] shadow-[0_0_12px_rgba(0,255,102,0.4)]"
                    : "bg-[#0A0A0A] border-[#00FF66]/60 text-[#00FF66] hover:bg-[#00FF66]/10"
                }`}
              >
                <div className="flex items-center gap-2 font-rajdhani font-bold text-sm tracking-wide">
                  <Terminal size={18} />
                  <span>LAUNCH POS MACHINE</span>
                </div>
                <span className="px-1.5 py-0.5 text-[10px] font-mono bg-[#00FF66] text-black font-black uppercase">
                  POS
                </span>
              </Link>
            </li>

            {/* Online Storefront (E-Commerce Channel 2) */}
            <li>
              <Link
                href="/"
                className={`flex items-center justify-between p-2 border transition-all duration-200 ${
                  pathname === "/"
                    ? "bg-[#00E5FF]/20 border-[#00E5FF] text-[#00E5FF] shadow-[0_0_12px_rgba(0,229,255,0.4)]"
                    : "bg-[#0A0A0A] border-[#00E5FF]/60 text-[#00E5FF] hover:bg-[#00E5FF]/10"
                }`}
              >
                <div className="flex items-center gap-2 font-rajdhani font-bold text-sm tracking-wide">
                  <Store size={18} />
                  <span>ONLINE STOREFRONT</span>
                </div>
                <span className="px-1.5 py-0.5 text-[10px] font-mono bg-[#00E5FF] text-black font-black uppercase">
                  WEB
                </span>
              </Link>
            </li>

            {/* Cart & Checkout Radar */}
            <li>
              <Link
                href="/cart"
                className={`flex items-center justify-between p-2 border transition-all duration-200 ${
                  pathname === "/cart"
                    ? "bg-[#FFB000]/20 border-[#FFB000] text-[#FFB000] shadow-[0_0_12px_rgba(255,176,0,0.4)]"
                    : "bg-[#0A0A0A] border-[#FFB000]/50 text-[#FFB000] hover:bg-[#FFB000]/10"
                }`}
              >
                <div className="flex items-center gap-2 font-rajdhani font-bold text-sm tracking-wide">
                  <ShoppingCart size={18} />
                  <span>SHOPPING CART RADAR</span>
                </div>
                <ChevronRight size={16} />
              </Link>
            </li>
          </ul>
        </div>

        {/* SYSTEM INTEGRATION & SERVER GATEWAY */}
        <div>
          <div className="flex items-center gap-2 font-orbitron text-xs font-bold text-[#00E5FF] border-b border-[#00E5FF]/30 pb-1 mb-2 tracking-wider">
            <Server size={14} />
            <span>BACKOFFICE GATEWAY</span>
          </div>
          
          <ul className="space-y-1.5 font-mono text-xs">
            <li>
              <a
                href="http://localhost:5128/Administrator/Home"
                target="_blank"
                rel="noopener noreferrer"
                className="flex items-center justify-between p-2 bg-[#0A0A0A] border border-white/20 text-[#EEEEEE] hover:border-[#00E5FF] hover:text-[#00E5FF] transition-all"
              >
                <div className="flex items-center gap-2">
                  <Database size={15} className="text-[#00E5FF]" />
                  <span>ERP ADMIN CMS</span>
                </div>
                <ExternalLink size={13} />
              </a>
            </li>
          </ul>
        </div>
      </div>

      {/* Footer System Diagnostic Badge */}
      <div className="p-3 bg-[#0A0A0A] border border-[#FF3333]/40 text-xs font-mono">
        <div className="flex items-center justify-between text-[#FF3333] mb-1 font-bold">
          <span>SECURITY PROTOCOL</span>
          <ShieldCheck size={16} />
        </div>
        <div className="text-[11px] text-[#777777] leading-tight">
          TENANT ISOLATION ENABLED //
          <br />
          NO MOCK DATA ALLOWED //
          <br />
          ACID TRANSACTION LOCK: OK
        </div>
      </div>
    </aside>
  );
}
