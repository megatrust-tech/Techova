"use client";

import Link from "next/link";
import { useEffect, useMemo, useState, useRef } from "react";
import { useRouter } from "next/navigation";
import ar from "@/locales/ar.json";
import en from "@/locales/en.json";
import Header from "@/components/common/Header";
import {
  createUser,
  isAuthenticated,
  fetchRoles,
  RoleDto,
  hasRole,
} from "@/lib/api/auth";

type Locale = "en" | "ar";

const translations: Record<Locale, typeof en> = { en, ar };

export default function RegisterPage() {
  const [locale, setLocale] = useState<Locale>("en");
  const [isLoading, setIsLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [success, setSuccess] = useState<string | null>(null);
  const [roles, setRoles] = useState<RoleDto[]>([]);
  const [loadingRoles, setLoadingRoles] = useState(true);
  const formRef = useRef<HTMLFormElement>(null);
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
    // Check authentication and role
    if (isAuthenticated()) {
      // Check if user has Admin, HR, or Manager role
      const allowedRoles = ["Admin"];
      if (!hasRole(allowedRoles)) {
        // User doesn't have required role, redirect to home
        router.push("/");
      }
    }
  }, [router]);

  useEffect(() => {
    // Fetch roles on component mount
    const loadRoles = async () => {
      try {
        const rolesData = await fetchRoles();
        setRoles(rolesData);
      } catch (err) {
        setError(
          err instanceof Error
            ? err.message
            : "Failed to load roles. Please refresh the page."
        );
      } finally {
        setLoadingRoles(false);
      }
    };

    loadRoles();
  }, []);

  const handleSubmit = async (e: React.FormEvent<HTMLFormElement>) => {
    e.preventDefault();
    setError(null);
    setSuccess(null);
    setIsLoading(true);

    const formData = new FormData(e.currentTarget);
    const firstName = formData.get("firstName") as string;
    const lastName = formData.get("lastName") as string;
    const email = formData.get("email") as string;
    const password = formData.get("password") as string;
    const confirmPassword = formData.get("confirmPassword") as string;
    const roleId = formData.get("roleId") as string;

    // Validate password match
    if (password !== confirmPassword) {
      setError("Passwords do not match");
      setIsLoading(false);
      return;
    }

    // Validate role selection
    if (!roleId || roleId === "") {
      setError("Please select a role");
      setIsLoading(false);
      return;
    }

    try {
      await createUser({
        firstName,
        lastName,
        email,
        password,
        roleId: parseInt(roleId, 10),
      });

      // Show success message
      setSuccess("User created successfully");

      // Reset form fields
      if (formRef.current) {
        formRef.current.reset();
      }
    } catch (err) {
      setError(
        err instanceof Error
          ? err.message
          : "Registration failed. Please try again."
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
          <h1>{t.registerTitle}</h1>
          <p>{t.registerSubtitle}</p>

          <form className="form" ref={formRef} onSubmit={handleSubmit}>
            {error && (
              <div
                className="error-message"
                style={{ color: "red", marginBottom: "1rem" }}
              >
                {error}
              </div>
            )}
            {success && (
              <div
                className="success-message"
                style={{ color: "green", marginBottom: "1rem" }}
              >
                {success}
              </div>
            )}

            <div className="input-group">
              <label htmlFor="firstName">First Name</label>
              <input
                id="firstName"
                name="firstName"
                type="text"
                className="input"
                placeholder="John"
                autoComplete="given-name"
                required
                disabled={isLoading}
              />
            </div>
            <div className="input-group">
              <label htmlFor="lastName">Last Name</label>
              <input
                id="lastName"
                name="lastName"
                type="text"
                className="input"
                placeholder="Doe"
                autoComplete="family-name"
                required
                disabled={isLoading}
              />
            </div>

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
              <label htmlFor="roleId">Role</label>
              <select
                id="roleId"
                name="roleId"
                className="input"
                required
                disabled={isLoading || loadingRoles}
              >
                <option value="">Select a role</option>
                {roles.map((role) => (
                  <option key={role.roleId} value={role.roleId}>
                    {role.roleName}
                  </option>
                ))}
              </select>
            </div>

            <div className="input-group">
              <label htmlFor="password">{t.password}</label>
              <input
                id="password"
                name="password"
                type="password"
                className="input"
                placeholder="••••••••"
                autoComplete="new-password"
                required
                disabled={isLoading}
              />
            </div>

            <div className="input-group">
              <label htmlFor="confirmPassword">{t.confirmPassword}</label>
              <input
                id="confirmPassword"
                name="confirmPassword"
                type="password"
                className="input"
                placeholder="••••••••"
                autoComplete="new-password"
                required
                disabled={isLoading}
              />
            </div>

            <button type="submit" className="primary-btn" disabled={isLoading}>
              {isLoading ? "Creating account..." : t.createAccount}
            </button>
          </form>

          <div className="actions-row" style={{ justifyContent: "center" }}>
            <span className="checkbox-row" style={{ gap: 6 }}>
              {t.haveAccount}
              <Link className="link" href="/login">
                {t.backToLogin}
              </Link>
            </span>
          </div>
        </section>
      </main>
    </>
  );
}