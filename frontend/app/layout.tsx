import type { Metadata } from "next";
import "./globals.css";
import { CyberNavbar } from "@/components/layout/CyberNavbar";
import { CyberSidebar } from "@/components/layout/CyberSidebar";

export const metadata = {
  title: "DigiPOSE Cyber HUD ERP - Dual Sales Subsystems Portal",
  description:
    "Next-Generation High-Density Cyber-Cinematic B2B/Retail Point of Sale and E-Commerce Web Storefront. Engineered for production scalability and real-time operations.",
  keywords: "POS, ERP, E-Commerce, Retail Portal, SaaS Subscriptions, Shopping Cart, Cyber HUD, Low-Latency O(1)",
  authors: [{ name: "DigiPOSE Systems Architecture Team" }],
};

export default function RootLayout({
  children,
}: Readonly<{
  children: React.ReactNode;
}>) {
  return (
    <html lang="en" className="dark">
      <body className="bg-[#000000] text-[#EEEEEE] font-rajdhani selection:bg-[#00E5FF] selection:text-black">
        <div className="flex flex-col min-h-screen">
          <CyberNavbar />
          <div className="flex flex-1 overflow-x-hidden">
            <CyberSidebar />
            <main className="flex-1 p-4 md:p-6 overflow-y-auto bg-[#000000] min-h-[calc(100vh-65px)] relative">
              {children}
            </main>
          </div>
        </div>
      </body>
    </html>
  );
}
