"use client";

import { useEffect, useMemo, useState } from "react";
import Header from "@/components/common/Header";
import ar from "@/locales/ar.json";
import en from "@/locales/en.json";

type Locale = "en" | "ar";

export default function HomePage() {
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
        showLogout={true}
        labels={{
          home: t.homeLink,
          why: t.whyLink,
          pricing: t.pricingLink,
          resources: t.resourcesLink,
          language: t.language,
        }}
      />
      <main className="flex items-center justify-center min-h-screen">
        <div className="text-center">
          <div className="text-8xl mb-4">⏳</div>
          <h1 className="text-5xl font-bold">Under Construction</h1>
        </div>
      </main>
    </>
  );
}
