"use client";

import Link from "next/link";
import { useEffect, useMemo, useState } from "react";
import { useRouter } from "next/navigation";
import ar from "@/locales/ar.json";
import en from "@/locales/en.json";
import Header from "@/components/common/Header";
import { login, isAuthenticated } from "@/lib/api/auth";

type Locale = "en" | "ar";

const translations: Record<Locale, typeof en> = { en, ar };

export default function LoginPage() {
  const [locale, setLocale] = useState<Locale>("en");
  const [isLoading, setIsLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const router = useRouter();
  const t = useMemo(() => translations[locale], [locale]);

  // Load language preference on mount
  useEffect(() => {
    const savedLocale = localStorage.getItem("locale") as Locale;
    if (savedLocale && (savedLocale === "en" || savedLocale === "ar")) {
      setLocale(savedLocale);
    }
  }, []);

  // Update document and storage when locale changes
  useEffect(() => {
    document.documentElement.lang = locale;
    document.documentElement.dir = locale === "ar" ? "rtl" : "ltr";
    localStorage.setItem("locale", locale);
  }, [locale]);

  useEffect(() => {
    // If already authenticated, redirect to home
    if (isAuthenticated()) {
      router.push("/");
    }
  }, [router]);

  const handleSubmit = async (e: React.FormEvent<HTMLFormElement>) => {
    e.preventDefault();
    setError(null);
    setIsLoading(true);

    const formData = new FormData(e.currentTarget);
    const email = formData.get("email") as string;
    const password = formData.get("password") as string;

    try {
      await login({ email, password });
      router.push("/");
    } catch (err) {
      setError(
        err instanceof Error ? err.message : "Login failed. Please try again."
      );
    } finally {
      setIsLoading(false);
    }
  };

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

          <form className="form" onSubmit={handleSubmit}>
            {error && (
              <div
                className="error-message"
                style={{ color: "red", marginBottom: "1rem" }}
              >
                {error}
              </div>
            )}

            <div className="input-group">
              <label htmlFor="email">{t.email}</label>
              <input
                id="email"
                name="email"
                type="email"
                className="input"
                placeholder="name@company.com"
                autoComplete="email"
                required
                disabled={isLoading}
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
                required
                disabled={isLoading}
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

            <button type="submit" className="primary-btn" disabled={isLoading}>
              {isLoading ? "Logging in..." : t.login}
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