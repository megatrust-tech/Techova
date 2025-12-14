"use client";

import Link from "next/link";
import { useEffect, useMemo, useState } from "react";
import ar from "@/locales/ar.json";
import en from "@/locales/en.json";
import Header from "@/components/common/Header";

type Locale = "en" | "ar";

const translations: Record<Locale, typeof en> = { en, ar };

export default function LoginPage() {
  const [locale, setLocale] = useState<Locale>("en");
  const t = useMemo(() => translations[locale], [locale]);

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
      <main className={`login-shell ${locale === "ar" ? "rtl" : ""}`}>
        <section className="brand-panel">
          <div>
            <div className="pill">
              <span className="pill-dot" />
              TaskedIn Portal
            </div>
            <h2 className="brand-heading">{t.brandHeadline}</h2>
            <p className="brand-subtext">{t.brandSubtext}</p>
          </div>
        </section>

        <section className="card">
          <h1>{t.title}</h1>
          <p>{t.subtitle}</p>

          <form className="form">
            <div className="input-group">
              <label htmlFor="email">{t.email}</label>
              <input
                id="email"
                name="email"
                type="email"
                className="input"
                placeholder="name@company.com"
                autoComplete="email"
              />
            </div>

            <div className="input-group">
              <label htmlFor="password">{t.password}</label>
              <input
                id="password"
                name="password"
                type="password"
                className="input"
                placeholder="••••••••"
                autoComplete="current-password"
              />
            </div>

            <div className="actions-row">
              <label className="checkbox-row">
                <input type="checkbox" name="remember" />
                {t.remember}
              </label>
              <a className="link" href="#forgot">
                {t.forgot}
              </a>
            </div>

            <button type="submit" className="primary-btn">
              {t.login}
            </button>
          </form>

          <p className="login-footer">{t.footer}</p>

          <div className="actions-row" style={{ justifyContent: "center" }}>
            <span className="checkbox-row" style={{ gap: 6 }}>
              {t.noAccount}
              <Link className="link" href="/register">
                {t.createAccount}
              </Link>
            </span>
          </div>
        </section>
      </main>
    </>
  );
}
