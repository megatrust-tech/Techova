"use client";

import { useEffect, useMemo, useState } from "react";
import Header from "@/components/common/Header";
import ar from "@/locales/ar.json";
import en from "@/locales/en.json";

type Locale = "en" | "ar";

export default function DashboardPage() {
  const [locale, setLocale] = useState<Locale>("en");
  const t = useMemo(() => ({ en, ar }[locale]), [locale]);

  useEffect(() => {
    document.documentElement.lang = locale;
    document.documentElement.dir = locale === "ar" ? "rtl" : "ltr";
  }, [locale]);

  return (
    <>
      <Header
        locale={locale}
        onToggleLocale={setLocale}
        labels={{
          home: t.homeLink,
          why: t.whyLink,
          pricing: t.pricingLink,
          resources: t.resourcesLink,
          language: t.language,
        }}
      />
      <main className={`page-shell ${locale === "ar" ? "rtl" : ""}`}>
        <header className="page-header">
          <div>
            <p className="eyebrow">Dashboard</p>
            <h1>Welcome</h1>
            <p className="muted">{t.brandSubtext}</p>
          </div>
        </header>
      </main>
    </>
  );
}
