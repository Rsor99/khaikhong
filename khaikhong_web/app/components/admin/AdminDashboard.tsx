"use client";

import { useState } from "react";
import { ProductManager } from "./ProductManager";
import { BundleManager } from "./BundleManager";

const tabs = [
  { id: "products", label: "จัดการสินค้า" },
  { id: "bundles", label: "จัดการ Bundles" },
] as const;

type TabId = (typeof tabs)[number]["id"];

export function AdminDashboard() {
  const [activeTab, setActiveTab] = useState<TabId>("products");

  return (
    <div className="space-y-6 text-white">
      <header className="rounded-3xl border border-white/10 bg-white/10 p-8 shadow-2xl backdrop-blur-xl">
        <h1 className="text-3xl font-semibold">แดชบอร์ดผู้ดูแลระบบ</h1>
        <p className="mt-2 text-sm text-white/70">
          จัดการสินค้าและ Bundle พร้อมควบคุมข้อมูลสินค้าทั้งหมดในระบบ
        </p>

        <nav className="mt-6 flex flex-wrap gap-2">
          {tabs.map((tab) => {
            const isActive = activeTab === tab.id;
            return (
              <button
                key={tab.id}
                type="button"
                onClick={() => setActiveTab(tab.id)}
                className={`rounded-2xl px-5 py-2 text-sm font-semibold transition ${
                  isActive
                    ? "bg-indigo-500 text-white shadow-lg shadow-indigo-500/30"
                    : "border border-white/20 text-white/70 hover:border-white/40 hover:text-white"
                }`}
              >
                {tab.label}
              </button>
            );
          })}
        </nav>
      </header>

      <main>
        {activeTab === "products" ? <ProductManager /> : <BundleManager />}
      </main>
    </div>
  );
}
