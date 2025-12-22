"use client";

import { useEffect, useMemo, useState } from "react";
import { useRouter } from "next/navigation";
import Header from "@/components/common/Header";
import ar from "@/locales/ar.json";
import en from "@/locales/en.json";
import { getUserRole, hasRole } from "@/lib/api/auth";

type Locale = "en" | "ar";

export default function HomePage() {
  const [locale, setLocale] = useState<Locale>("en");
  const router = useRouter();
  const t = useMemo(() => ({ en, ar }[locale]), [locale]);
  const userRole = getUserRole();
  const isManagerOrHR = hasRole(["Manager", "HR"]);

  useEffect(() => {
    document.documentElement.lang = locale;
    document.documentElement.dir = locale === "ar" ? "rtl" : "ltr";
  }, [locale]);

  // --- Admin Redirect ---
  useEffect(() => {
    if (userRole === "Admin") {
      router.push("/home/admin");
    }
  }, [userRole, router]);

  const handlePendingApproval = () => {
    if (userRole === "Manager") {
      router.push("/leaves/managerial");
    } else if (userRole === "HR") {
      router.push("/leaves/hr");
    }
  };

  if (userRole === "Admin") {
    return (
      <div className="flex items-center justify-center min-h-screen">
        <div className="text-center">
          <p className="text-gray-500">Redirecting to Admin Dashboard...</p>
        </div>
      </div>
    );
  }

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
      <main className={`page-shell ${locale === "ar" ? "rtl" : ""}`}>
        <header className="page-header">
          <div>
            <p className="eyebrow">Dashboard</p>
            <h1>Welcome</h1>
            <p className="muted">Manage your leaves and approvals</p>
          </div>
        </header>

        <section
          style={{
            display: "grid",
            gridTemplateColumns: "repeat(auto-fit, minmax(280px, 1fr))",
            gap: "1.5rem",
            marginTop: "2rem",
          }}
        >
          <div
            className="card"
            style={{
              padding: "2rem",
              cursor: "pointer",
              transition: "transform 0.2s, box-shadow 0.2s",
            }}
            onClick={() => router.push("/leaves/request")}
            onMouseEnter={(e) => {
              e.currentTarget.style.transform = "translateY(-4px)";
              e.currentTarget.style.boxShadow =
                "0 10px 25px rgba(0, 0, 0, 0.1)";
            }}
            onMouseLeave={(e) => {
              e.currentTarget.style.transform = "translateY(0)";
              e.currentTarget.style.boxShadow = "";
            }}
          >
            <div style={{ fontSize: "3rem", marginBottom: "1rem" }}>📋</div>
            <h2 style={{ marginBottom: "0.5rem" }}>My Leaves</h2>
            <p style={{ color: "var(--text-muted)", fontSize: "0.875rem" }}>
              View and submit leave requests
            </p>
          </div>

          {isManagerOrHR && (
            <div
              className="card"
              style={{
                padding: "2rem",
                cursor: "pointer",
                transition: "transform 0.2s, box-shadow 0.2s",
              }}
              onClick={handlePendingApproval}
              onMouseEnter={(e) => {
                e.currentTarget.style.transform = "translateY(-4px)";
                e.currentTarget.style.boxShadow =
                  "0 10px 25px rgba(0, 0, 0, 0.1)";
              }}
              onMouseLeave={(e) => {
                e.currentTarget.style.transform = "translateY(0)";
                e.currentTarget.style.boxShadow = "";
              }}
            >
              <div style={{ fontSize: "3rem", marginBottom: "1rem" }}>✅</div>
              <h2 style={{ marginBottom: "0.5rem" }}>Pending Approval</h2>
              <p style={{ color: "var(--text-muted)", fontSize: "0.875rem" }}>
                Review and approve leave requests
              </p>
            </div>
          )}
        </section>
      </main>
    </>
  );
}