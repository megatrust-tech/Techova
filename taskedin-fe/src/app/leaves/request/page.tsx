"use client";

import { useEffect, useMemo, useState } from "react";
import DateRangePicker from "@/components/common/DateRangePicker";
import Header from "@/components/common/Header";
import ar from "@/locales/ar.json";
import en from "@/locales/en.json";

type LeaveStatus = "pending" | "approved" | "rejected";

type LeaveRequest = {
  id: string;
  title: string;
  submittedAt: string;
  from: string;
  to: string;
  status: LeaveStatus;
};

export default function LeaveRequestPage() {
  const [locale, setLocale] = useState<"en" | "ar">("en");
  const t = useMemo(() => ({ en, ar }[locale]), [locale]);

  useEffect(() => {
    document.documentElement.lang = locale;
    document.documentElement.dir = locale === "ar" ? "rtl" : "ltr";
  }, [locale]);

  const [balance] = useState<number>(14);
  const [pickerOpen, setPickerOpen] = useState(false);
  const [dateRange, setDateRange] = useState<{
    start: Date | null;
    end: Date | null;
  }>({
    start: null,
    end: null,
  });
  const [requests] = useState<LeaveRequest[]>([
    {
      id: "1",
      title: "Annual leave",
      submittedAt: "2025-01-05",
      from: "2025-02-10",
      to: "2025-02-14",
      status: "pending",
    },
    {
      id: "2",
      title: "Sick leave",
      submittedAt: "2024-12-18",
      from: "2025-01-03",
      to: "2025-01-04",
      status: "approved",
    },
    {
      id: "3",
      title: "Emergency leave",
      submittedAt: "2024-11-02",
      from: "2024-11-05",
      to: "2024-11-06",
      status: "rejected",
    },
  ]);

  const statusLabel = useMemo(
    () =>
      ({
        pending: t.pending,
        approved: t.approved,
        rejected: t.rejected,
      } satisfies Record<LeaveStatus, string>),
    [t]
  );

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
            <p className="eyebrow">{t.leavesHeader}</p>
            <h1>{t.leavesHeader}</h1>
            <p className="muted">{t.leavesSubtitle}</p>
          </div>
          <div className="stat-card">
            <div className="stat-card-content">
              <div>
                <p className="muted">{t.remainingBalance}</p>
                <p className="stat-value">
                  {balance} <span className="stat-unit">{t.daysUnit}</span>
                </p>
              </div>
              <div className="stack picker-anchor">
                <button
                  type="button"
                  className="primary-btn slim"
                  onClick={() => setPickerOpen((prev) => !prev)}
                >
                  {pickerOpen ? t.closePicker : t.submitRequest}
                </button>
                {dateRange.start && dateRange.end && (
                  <p className="muted small">
                    {t.selectDates} {dateRange.start.toISOString().slice(0, 10)}{" "}
                    → {dateRange.end.toISOString().slice(0, 10)}
                  </p>
                )}
                {pickerOpen && (
                  <div className="picker-popover">
                    <div className="picker-head">
                      <h2>{t.selectDates}</h2>
                      <button
                        type="button"
                        className="secondary-btn"
                        onClick={() => setPickerOpen(false)}
                      >
                        {t.close}
                      </button>
                    </div>
                    <DateRangePicker
                      monthsToShow={1}
                      selectionMode="range"
                      weekStartsOn={6}
                      value={dateRange}
                      onChange={setDateRange}
                    />
                  </div>
                )}
              </div>
            </div>
          </div>
        </header>

        <section className="card list-card">
          <div className="list-head">
            <h2>{t.myRequests}</h2>
          </div>
          <div className="list-grid">
            <div className="list-row list-headings">
              <span>{t.titleCol}</span>
              <span>{t.fromCol}</span>
              <span>{t.toCol}</span>
              <span>{t.submittedCol}</span>
              <span>{t.statusCol}</span>
            </div>
            {requests.map((req) => (
              <div className="list-row" key={req.id}>
                <span>{req.title}</span>
                <span>{req.from}</span>
                <span>{req.to}</span>
                <span>{req.submittedAt}</span>
                <span>
                  <span className={`status-chip status-${req.status}`}>
                    {statusLabel[req.status]}
                  </span>
                </span>
              </div>
            ))}
          </div>
        </section>
      </main>
    </>
  );
}
