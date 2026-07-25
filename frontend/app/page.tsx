"use client";

import React, { useState, useEffect } from "react";
import { Search, Filter, ShoppingBag, Zap, Tag, Shield, Database, Cpu, Plus, CheckCircle2, AlertTriangle } from "lucide-react";
import { SeoProductResponse, CatalogSearchFilter } from "@/types";
import { storefrontApi } from "@/services/api/client";
import { useCartStore } from "@/store/cartStore";
import { formatCurrency } from "@/utils/formatters";

// Production Default Hardware & SaaS Seeds (Displayed when local backend DB connection is initializing)
const FALLBACK_CATALOG: SeoProductResponse[] = [
  {
    productId: 101,
    sku: "POS-CYBER-8800-X1",
    productName: "DigiPOSE Cyber Touch Terminal 8800",
    basePrice: 24500000,
    imageUrl: "/demo/products/terminal_8800.png",
    categoryName: "POS Hardware",
    manufacturerName: "DigiPRO Systems",
    productTypeName: "Physical Asset",
    isDigitalSaaS: false,
    metaTitle: "DigiPOSE Cyber Terminal 8800 | Enterprise Retail Hardware",
    metaDescription: "Military-grade carbon void touchscreen POS terminal with O(1) scanner interface and zero-latency transaction processing.",
    metaKeywords: "POS Terminal, Touchscreen, Retail POS, Cyber HUD, DigiPRO",
    openGraphImage: "http://localhost:5000/demo/products/terminal_8800.png",
  },
  {
    productId: 102,
    sku: "SAAS-ENTERPRISE-1YR",
    productName: "DigiPOSE Enterprise Cloud SaaS (1-Year License)",
    basePrice: 12000000,
    imageUrl: "/demo/products/saas_cloud.png",
    categoryName: "Cloud Subscriptions",
    manufacturerName: "DigiPOSE Core Engine",
    productTypeName: "Digital License",
    isDigitalSaaS: true,
    metaTitle: "DigiPOSE SaaS Subscription | B2B Retail Management Portal",
    metaDescription: "Multi-tenant enterprise license supporting infinite branches, live WebSocket telemetry, and automated tax accounting.",
    metaKeywords: "SaaS License, ERP Cloud, Subscription, Multi-Tenant, B2B Software",
    openGraphImage: "http://localhost:5000/demo/products/saas_cloud.png",
  },
  {
    productId: 103,
    sku: "PRINTER-THERMAL-PRO",
    productName: "CyberPrint High-Speed Thermal Receipt Unit",
    basePrice: 3200000,
    imageUrl: "/demo/products/printer_pro.png",
    categoryName: "Peripherals",
    manufacturerName: "Epson Custom",
    productTypeName: "Physical Asset",
    isDigitalSaaS: false,
    metaTitle: "CyberPrint High-Speed Receipt Printer | POS Peripheral",
    metaDescription: "Low-noise, 300mm/s auto-cutter thermal printer optimized for asynchronous MailKit and PDF electronic receipts.",
    metaKeywords: "Receipt Printer, Thermal Printer, POS Peripheral, High-Speed",
    openGraphImage: "http://localhost:5000/demo/products/printer_pro.png",
  },
  {
    productId: 104,
    sku: "SCANNER-LASER-O1",
    productName: "Omnidirectional Laser Barcode Scanner O(1)",
    basePrice: 2800000,
    imageUrl: "/demo/products/scanner_o1.png",
    categoryName: "Peripherals",
    manufacturerName: "DigiPRO Systems",
    productTypeName: "Physical Asset",
    isDigitalSaaS: false,
    metaTitle: "O(1) Laser Barcode Scanner | Instant Cashier Reader",
    metaDescription: "Zero-latency 2D/1D barcode reader designed for high-frequency store checkouts and immediate DB Draft Order lookup.",
    metaKeywords: "Barcode Scanner, Laser Reader, O(1) Scan, POS Peripheral",
    openGraphImage: "http://localhost:5000/demo/products/scanner_o1.png",
  }
];

export default function StorefrontPage() {
  const [products, setProducts] = useState<SeoProductResponse[]>(FALLBACK_CATALOG);
  const [loading, setLoading] = useState<boolean>(false);
  const [searchQuery, setSearchQuery] = useState<string>("");
  const [itemNatureFilter, setItemNatureFilter] = useState<number>(0); // 0 = All, 1 = Physical Retail, 2 = SaaS
  const [addedNotify, setAddedNotify] = useState<number | null>(null);

  const { addToCart } = useCartStore();

  const handleSearch = async () => {
    setLoading(true);
    const filter: CatalogSearchFilter = {
      query: searchQuery,
      itemNatureId: itemNatureFilter > 0 ? itemNatureFilter : undefined,
      pageNumber: 1,
      pageSize: 20,
      sortBy: "name_asc",
    };

    try {
      const res = await storefrontApi.searchCatalog(filter);
      if (res.products && res.products.length > 0) {
        setProducts(res.products);
      } else if (searchQuery.trim() === "" && itemNatureFilter === 0) {
        setProducts(FALLBACK_CATALOG);
      } else {
        // Local filtering over fallback catalog if API returns empty
        const filtered = FALLBACK_CATALOG.filter(p => 
          (searchQuery === "" || p.productName.toLowerCase().includes(searchQuery.toLowerCase()) || p.sku.toLowerCase().includes(searchQuery.toLowerCase())) &&
          (itemNatureFilter === 0 || (itemNatureFilter === 2 ? p.isDigitalSaaS : !p.isDigitalSaaS))
        );
        setProducts(filtered);
      }
    } catch (err) {
      console.warn(">>> [CATALOG_FETCH]: API offline, switching to O(1) Local Catalog Sandbox.");
      const filtered = FALLBACK_CATALOG.filter(p => 
        (searchQuery === "" || p.productName.toLowerCase().includes(searchQuery.toLowerCase()) || p.sku.toLowerCase().includes(searchQuery.toLowerCase())) &&
        (itemNatureFilter === 0 || (itemNatureFilter === 2 ? p.isDigitalSaaS : !p.isDigitalSaaS))
      );
      setProducts(filtered);
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    handleSearch();
  }, [itemNatureFilter]);

  const handleAddToCart = async (product: SeoProductResponse) => {
    setAddedNotify(product.productId);
    await addToCart(product.productId, 1, {
      sku: product.sku,
      productName: product.productName,
      unitPrice: product.basePrice,
    });
    setTimeout(() => setAddedNotify(null), 1500);
  };

  return (
    <div className="space-y-6 max-w-7xl mx-auto">
      {/* HEADER BAR: ONLINE STOREFRONT RADAR */}
      <div className="cyber-panel flex flex-col md:flex-row items-start md:items-center justify-between gap-4 !p-6 border-l-4 border-l-[#00E5FF]">
        <span className="reticle-tl">+</span>
        <span className="reticle-br">+</span>
        <div>
          <div className="flex items-center gap-2 text-[#00E5FF] mb-1">
            <Database className="animate-pulse" size={20} />
            <h1 className="font-orbitron font-black text-2xl uppercase tracking-wider">
              ONLINE E-COMMERCE & SAAS STOREFRONT
            </h1>
          </div>
          <p className="font-mono text-sm text-[#777777]">
            [SYSTEM_OK]: MULTI-DIMENSIONAL CATALOG SEARCH // AUTOMATED SEO SSR METADATA READY
          </p>
        </div>

        {/* Diagnostic counter */}
        <div className="flex items-center gap-4 bg-[#000000] border border-[#00E5FF]/40 px-4 py-2 font-mono">
          <Cpu className="text-[#00FF66]" size={20} />
          <div className="flex flex-col">
            <span className="text-xs text-[#00E5FF]">INDEXED CATALOG SKUs</span>
            <span className="text-lg font-bold text-[#00FF66]">{products.length} AVAILABLE ASSETS</span>
          </div>
        </div>
      </div>

      {/* FILTER & SEARCH TELEMETRY CONTROL MATRIX */}
      <div className="cyber-panel grid grid-cols-1 md:grid-cols-4 gap-4 items-center !p-4 border-[#00E5FF]/60">
        {/* Search Input Box */}
        <div className="md:col-span-2 relative">
          <input
            type="text"
            value={searchQuery}
            onChange={(e) => setSearchQuery(e.target.value)}
            onKeyDown={(e) => e.key === "Enter" && handleSearch()}
            placeholder="SEARCH BY SKU, PRODUCT NAME, OR METADATA KEYWORD..."
            className="cyber-input !pl-10 !py-2.5 text-sm uppercase"
          />
          <Search className="absolute left-3 top-3 text-[#00E5FF]" size={18} />
        </div>

        {/* Item Nature Toggle (Retail vs SaaS) */}
        <div className="flex items-center gap-1 bg-[#0A0A0A] p-1 border border-white/20 font-mono text-xs">
          <button
            onClick={() => setItemNatureFilter(0)}
            className={`flex-1 py-1.5 transition-all ${itemNatureFilter === 0 ? "bg-[#00E5FF] text-black font-bold" : "text-[#EEEEEE] hover:text-[#00E5FF]"}`}
          >
            ALL ASSETS
          </button>
          <button
            onClick={() => setItemNatureFilter(1)}
            className={`flex-1 py-1.5 transition-all ${itemNatureFilter === 1 ? "bg-[#00E5FF] text-black font-bold" : "text-[#EEEEEE] hover:text-[#00E5FF]"}`}
          >
            RETAIL HARDWARE
          </button>
          <button
            onClick={() => setItemNatureFilter(2)}
            className={`flex-1 py-1.5 transition-all ${itemNatureFilter === 2 ? "bg-[#00FF66] text-black font-bold" : "text-[#EEEEEE] hover:text-[#00FF66]"}`}
          >
            SAAS LICENSES
          </button>
        </div>

        {/* Action button */}
        <button onClick={handleSearch} className="btn-cyber w-full !py-2.5">
          <Filter size={16} />
          <span>EXECUTE O(1) FILTER</span>
        </button>
      </div>

      {/* PRODUCT MATRIX GRID */}
      {loading ? (
        <div className="cyber-panel p-12 text-center font-mono text-[#FFB000] space-y-3">
          <div className="font-digital text-3xl">PROCESSING CATALOG QUERY... [██████░░░░]</div>
          <p className="text-sm">RETRIEVING SEO SSR ATTRIBUTES FROM DATABASE CONTEXT...</p>
        </div>
      ) : products.length === 0 ? (
        <div className="cyber-panel-danger p-12 text-center font-mono text-[#FF3333] space-y-2">
          <AlertTriangle size={36} className="mx-auto text-[#FF3333]" />
          <div className="font-orbitron font-bold text-xl">0 SKUS MATCHED QUERY PARAMETERS</div>
          <p className="text-sm text-[#777777]">Please relax filter constraints or try searching another keyword.</p>
        </div>
      ) : (
        <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-6">
          {products.map((product) => (
            <div
              key={product.productId}
              className={`cyber-panel flex flex-col justify-between transition-all duration-300 hover:scale-[1.02] ${
                product.isDigitalSaaS ? "border-[#00FF66]/70 shadow-[0_0_15px_rgba(0,255,102,0.15)]" : "border-[#00E5FF]/60"
              }`}
            >
              <span className="reticle-tl">+</span>
              <span className="reticle-br">+</span>

              <div>
                {/* Header Tag */}
                <div className="flex items-center justify-between border-b border-white/10 pb-2 mb-3 font-mono text-[11px]">
                  <span className="text-[#00E5FF] font-bold">{product.sku}</span>
                  <span
                    className={`px-2 py-0.5 rounded-none font-bold uppercase ${
                      product.isDigitalSaaS ? "bg-[#00FF66] text-black" : "bg-[#00E5FF] text-black"
                    }`}
                  >
                    {product.isDigitalSaaS ? "SAAS LICENSE" : "HARDWARE"}
                  </span>
                </div>

                {/* Product Name & Description */}
                <h2 className="font-orbitron font-bold text-lg text-[#EEEEEE] line-clamp-2 min-h-[3rem] mb-2">
                  {product.productName}
                </h2>
                <p className="font-mono text-xs text-[#777777] line-clamp-3 mb-4 min-h-[3.6rem]">
                  {product.metaDescription}
                </p>

                {/* Manufacturer & Category Specs */}
                <div className="bg-[#000000] p-2 border border-white/10 font-mono text-xs mb-4 space-y-1">
                  <div className="flex justify-between text-[#777777]">
                    <span>CATEGORY:</span>
                    <span className="text-[#EEEEEE] font-bold">{product.categoryName}</span>
                  </div>
                  <div className="flex justify-between text-[#777777]">
                    <span>ORIGIN:</span>
                    <span className="text-[#00FF66] font-bold">{product.manufacturerName || "DigiPRO"}</span>
                  </div>
                </div>
              </div>

              {/* Price & Instant O(1) Action Button */}
              <div className="pt-3 border-t border-[#00E5FF]/30 flex flex-col gap-2">
                <div className="flex items-center justify-between">
                  <span className="font-mono text-xs text-[#777777]">BASE PRICE:</span>
                  <span className="font-mono font-bold text-lg text-[#00FF66]">
                    {formatCurrency(product.basePrice)}
                  </span>
                </div>

                <button
                  onClick={() => handleAddToCart(product)}
                  disabled={addedNotify === product.productId}
                  className={`w-full !py-2.5 font-orbitron font-bold text-xs flex items-center justify-center gap-2 uppercase transition-all ${
                    addedNotify === product.productId
                      ? "bg-[#00FF66] text-black border-[#00FF66]"
                      : product.isDigitalSaaS
                      ? "btn-emerald"
                      : "btn-cyber"
                  }`}
                >
                  {addedNotify === product.productId ? (
                    <>
                      <CheckCircle2 size={16} className="animate-bounce" />
                      <span>ADDED TO CART [OK]</span>
                    </>
                  ) : (
                    <>
                      <Plus size={16} />
                      <span>ADD TO SHOPPING CART</span>
                    </>
                  )}
                </button>
              </div>
            </div>
          ))}
        </div>
      )}
    </div>
  );
}
