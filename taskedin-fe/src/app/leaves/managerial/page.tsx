"use client";

import { useEffect, useMemo, useState } from "react";
import Header from "@/components/common/Header";
import ar from "@/locales/ar.json";
import en from "@/locales/en.json";

type LeaveStatus = "pending" | "approved" | "rejected";

type ManagedRequest = {
  id: string;
  employee: string;
  title: string;
  from: string;
  to: string;
  submittedAt: string;
  status: LeaveStatus;
};

export default function ManagerialLeavesPage() {
  const [locale, setLocale] = useState<"en" | "ar">("en");
  const t = useMemo(() => ({ en, ar }[locale]), [locale]);

  useEffect(() => {
    document.documentElement.lang = locale;
    document.documentElement.dir = locale === "ar" ? "rtl" : "ltr";
  }, [locale]);

  const [requests, setRequests] = useState<ManagedRequest[]>([
    {
      id: "11",
      employee: "Sara Ali",
      title: "Annual leave",
      from: "2025-02-02",
      to: "2025-02-10",
      submittedAt: "2025-01-18",
      status: "pending",
    },
    {
      id: "12",
      employee: "Omar Hassan",
      title: "Sick leave",
      from: "2025-01-28",
      to: "2025-01-30",
      submittedAt: "2025-01-22",
      status: "pending",
    },
  ]);

  const [activeId, setActiveId] = useState<string | null>(null);
  const [comment, setComment] = useState("");

  const activeRequest = useMemo(
    () => requests.find((r) => r.id === activeId) || null,
    [activeId, requests]
  );

  const updateStatus = (status: Exclude<LeaveStatus, "pending">) => {
    if (!activeRequest) return;
    setRequests((prev) =>
      prev.map((r) => (r.id === activeRequest.id ? { ...r, status } : r))
    );
    setActiveId(null);
    setComment("");
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
      <main className={`page-shell ${locale === "ar" ? "rtl" : ""}`}>
        <header className="page-header">
          <div>
            <p className="eyebrow">Manager</p>
            <h1>{t.awaitingAction}</h1>
            <p className="muted">{t.leavesSubtitle}</p>
          </div>
        </header>

        <section className="card list-card">
          <div className="list-head">
            <h2>{t.awaitingAction}</h2>
          </div>
          <div className="list-grid">
            <div className="list-row list-headings">
              <span>{t.employeeCol}</span>
              <span>{t.titleCol}</span>
              <span>{t.fromCol}</span>
              <span>{t.toCol}</span>
              <span>{t.submittedCol}</span>
              <span>{t.statusCol}</span>
              <span>{t.review}</span>
            </div>
            {requests.map((req) => (
              <div className="list-row" key={req.id}>
                <span>{req.employee}</span>
                <span>{req.title}</span>
                <span>{req.from}</span>
                <span>{req.to}</span>
                <span>{req.submittedAt}</span>
                <span>
                  <span className={`status-chip status-${req.status}`}>
                    {req.status === "pending"
                      ? t.pending
                      : req.status === "approved"
                      ? t.approved
                      : t.rejected}
                  </span>
                </span>
                <span>
                  {req.status === "pending" ? (
                    <button
                      type="button"
                      className="primary-btn slim"
                      onClick={() => setActiveId(req.id)}
                    >
                      {t.review}
                    </button>
                  ) : (
                    "—"
                  )}
                </span>
              </div>
            ))}
          </div>
        </section>

        {activeRequest && (
          <div className="modal-backdrop" role="presentation">
            <div className="modal-card" role="dialog" aria-modal="true">
              <div className="modal-head">
                <div>
                  <p className="eyebrow">Review request</p>
                  <h2>{activeRequest.employee}</h2>
                  <p className="muted">
                    {activeRequest.title} · {activeRequest.from} →{" "}
                    {activeRequest.to}
                  </p>
                </div>
                <button
                  type="button"
                  className="secondary-btn"
                  onClick={() => setActiveId(null)}
                >
                  {t.close}
                </button>
              </div>

              <label className="input-group" style={{ marginBottom: 14 }}>
                <span style={{ fontWeight: 600 }}>{t.commentOptional}</span>
                <textarea
                  className="input textarea"
                  rows={4}
                  value={comment}
                  onChange={(e) => setComment(e.target.value)}
                  placeholder={t.commentPlaceholder}
                />
              </label>

              <div className="modal-actions">
                <button
                  type="button"
                  className="secondary-btn"
                  onClick={() => updateStatus("rejected")}
                >
                  {t.reject}
                </button>
                <button
                  type="button"
                  className="primary-btn"
                  onClick={() => updateStatus("approved")}
                >
                  {t.approve}
                </button>
              </div>
            </div>
          </div>
        )}
      </main>
    </>
  );
}
