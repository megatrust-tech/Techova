"use client";

import { useEffect, useMemo, useState } from "react";
import Header from "@/components/common/Header";
import ar from "@/locales/ar.json";
import en from "@/locales/en.json";
import {
  fetchPendingApprovals,
  submitHRAction,
  fetchLeaveRequestHistory,
  PendingLeaveRequest,
  LeaveAuditLogDto,
} from "@/lib/api/leaves";
import { getAccessToken } from "@/lib/api/auth";

const API_BASE_URL =
  process.env.NEXT_PUBLIC_BACKEND_URL || "http://localhost:8000";

export default function HRLeavesPage() {
  const [locale, setLocale] = useState<"en" | "ar">("en");
  const t = useMemo(() => ({ en, ar }[locale]), [locale]);

  const [requests, setRequests] = useState<PendingLeaveRequest[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [success, setSuccess] = useState<string | null>(null);
  const [activeId, setActiveId] = useState<number | null>(null);
  const [comment, setComment] = useState("");
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [submittingId, setSubmittingId] = useState<number | null>(null);
  const [history, setHistory] = useState<LeaveAuditLogDto[]>([]);
  const [loadingHistory, setLoadingHistory] = useState(false);
  const [historyModalOpen, setHistoryModalOpen] = useState(false);
  const [selectedHistoryRequestId, setSelectedHistoryRequestId] = useState<
    number | null
  >(null);

  useEffect(() => {
    document.documentElement.lang = locale;
    document.documentElement.dir = locale === "ar" ? "rtl" : "ltr";
  }, [locale]);

  useEffect(() => {
    const loadPendingApprovals = async () => {
      setLoading(true);
      setError(null);
      try {
        const data = await fetchPendingApprovals("PendingHR");
        // Handle paginated response structure: extract items if it's an object, otherwise use data directly if it's an array
        let requestsArray: PendingLeaveRequest[] = [];
        if (Array.isArray(data)) {
          requestsArray = data;
        } else if (data && typeof data === "object" && "items" in data) {
          const items = (data as { items: unknown }).items;
          requestsArray = Array.isArray(items) ? items : [];
        }
        setRequests(requestsArray);
      } catch (err) {
        setError(
          err instanceof Error
            ? err.message
            : "Failed to load pending approvals. Please refresh the page."
        );
      } finally {
        setLoading(false);
      }
    };

    loadPendingApprovals();
  }, []);

  const activeRequest = useMemo(
    () =>
      (Array.isArray(requests)
        ? requests.find((r) => r.id === activeId)
        : null) || null,
    [activeId, requests]
  );

  // Handle view history
  const handleViewHistory = async (requestId: number) => {
    setSelectedHistoryRequestId(requestId);
    setHistoryModalOpen(true);
    setLoadingHistory(true);
    setHistory([]);

    try {
      const historyData = await fetchLeaveRequestHistory(requestId);
      setHistory(historyData);
    } catch (err) {
      console.error("Failed to load history:", err);
    } finally {
      setLoadingHistory(false);
    }
  };

  // Format date as YYYY-MM-DD
  const formatDateDisplay = (dateString: string): string => {
    try {
      const date = new Date(dateString);
      return date.toISOString().split("T")[0];
    } catch {
      return dateString;
    }
  };

  // Format date and time for history display
  const formatDateTimeDisplay = (dateString: string): string => {
    try {
      const date = new Date(dateString);
      return date.toLocaleString();
    } catch {
      return dateString;
    }
  };

  // Format status name (e.g., "PendingManager" -> "Pending Manager")
  const formatStatusName = (status: string): string => {
    return status
      .replace(/([a-z])([A-Z])/g, "$1 $2")
      .replace(/([A-Z]+)([A-Z][a-z])/g, "$1 $2")
      .trim();
  };

  // Get status color
  const getStatusColor = (status: string): string => {
    const statusLower = status.toLowerCase();
    if (statusLower.includes("pending")) {
      return "#f59e0b"; // Amber/orange for pending
    } else if (statusLower === "approved") {
      return "#10b981"; // Green for approved
    } else if (statusLower === "rejected") {
      return "#ef4444"; // Red for rejected
    } else if (statusLower === "cancelled") {
      return "#6b7280"; // Gray for cancelled
    }
    return "#6b7280"; // Default gray
  };

  // Download attachment
  const handleDownloadAttachment = async (attachmentUrl: string) => {
    try {
      const accessToken = await getAccessToken();
      if (!accessToken) {
        throw new Error("Not authenticated");
      }

      const response = await fetch(`${API_BASE_URL}${attachmentUrl}`, {
        headers: {
          Authorization: `Bearer ${accessToken}`,
        },
      });

      if (!response.ok) {
        if (response.status === 401) {
          throw new Error("Unauthorized. Please log in again.");
        }
        if (response.status === 404) {
          throw new Error("Attachment not found.");
        }
        throw new Error("Failed to download attachment");
      }

      const blob = await response.blob();
      const url = window.URL.createObjectURL(blob);
      const a = document.createElement("a");
      a.href = url;
      a.download = attachmentUrl.split("/").pop() || "attachment";
      document.body.appendChild(a);
      a.click();
      window.URL.revokeObjectURL(url);
      document.body.removeChild(a);
    } catch (err) {
      setError(
        err instanceof Error
          ? err.message
          : "Failed to download attachment. Please try again."
      );
    }
  };

  // Handle approve/reject action
  const handleHRAction = async (action: "approve" | "reject") => {
    if (!activeRequest) return;

    setIsSubmitting(true);
    setSubmittingId(activeRequest.id);
    setError(null);
    setSuccess(null);

    try {
      await submitHRAction(activeRequest.id, {
        isApproved: action === "approve",
        comment: comment.trim() || undefined,
      });

      setSuccess(
        `Leave request ${
          action === "approve" ? "approved" : "rejected"
        } successfully`
      );

      // Remove the request from the list
      setRequests((prev) =>
        Array.isArray(prev) ? prev.filter((r) => r.id !== activeRequest.id) : []
      );
      setActiveId(null);
      setComment("");

      // Clear success message after 3 seconds
      setTimeout(() => setSuccess(null), 3000);
    } catch (err) {
      setError(
        err instanceof Error
          ? err.message
          : `Failed to ${action} leave request. Please try again.`
      );
    } finally {
      setIsSubmitting(false);
      setSubmittingId(null);
    }
  };

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
            <p className="eyebrow">HR</p>
            <h1>{t.awaitingAction}</h1>
            <p className="muted">{t.leavesSubtitle}</p>
          </div>
        </header>

        <section className="card list-card">
          <div className="list-head">
            <h2>{t.awaitingAction}</h2>
            {error && (
              <div
                style={{
                  color: "var(--danger)",
                  backgroundColor: "#fee",
                  padding: "0.75rem 1rem",
                  borderRadius: "6px",
                  marginBottom: "1rem",
                  fontSize: "0.875rem",
                }}
              >
                {error}
              </div>
            )}
            {success && (
              <div
                style={{
                  color: "#10b981",
                  backgroundColor: "#ecfdf3",
                  padding: "0.75rem 1rem",
                  borderRadius: "6px",
                  marginBottom: "1rem",
                  fontSize: "0.875rem",
                }}
              >
                {success}
              </div>
            )}
          </div>
          {loading ? (
            <div
              style={{
                padding: "3rem",
                textAlign: "center",
                color: "var(--text-muted)",
              }}
            >
              Loading pending approvals...
            </div>
          ) : !Array.isArray(requests) || requests.length === 0 ? (
            <div
              style={{
                padding: "3rem",
                textAlign: "center",
                color: "var(--text-muted)",
              }}
            >
              No pending leave requests
            </div>
          ) : (
            <div className="list-grid">
              {requests.map((req) => (
                <div className="request-row" key={req.id}>
                  <span>
                    <strong>Employee:</strong> {req.employeeEmail}
                  </span>
                  <span>
                    <strong>Leave Type:</strong> {req.leaveType}
                  </span>
                  <span>
                    <strong>From:</strong> {formatDateDisplay(req.startDate)}
                  </span>
                  <span>
                    <strong>To:</strong> {formatDateDisplay(req.endDate)}
                  </span>
                  <span>
                    <strong>Days:</strong> {req.numberOfDays}
                  </span>
                  <span>
                    <strong>Submitted:</strong>{" "}
                    {formatDateDisplay(req.createdAt)}
                  </span>
                  <span>
                    <span
                      className="status-chip"
                      style={{
                        backgroundColor: `${getStatusColor(req.status)}20`,
                        color: getStatusColor(req.status),
                        padding: "0.25rem 0.75rem",
                        borderRadius: "12px",
                        fontSize: "0.875rem",
                        fontWeight: "500",
                        display: "inline-block",
                      }}
                    >
                      {formatStatusName(req.status)}
                    </span>
                  </span>
                  <div className="actions">
                    <button
                      type="button"
                      className="secondary-btn slim"
                      onClick={() => handleViewHistory(req.id)}
                    >
                      History
                    </button>

                    <button
                      type="button"
                      className="primary-btn slim"
                      onClick={() => setActiveId(req.id)}
                      disabled={isSubmitting}
                    >
                      {t.review}
                    </button>
                  </div>
                </div>
              ))}
            </div>
          )}
        </section>

        {activeRequest && (
          <div
            style={{
              position: "fixed",
              top: 0,
              left: 0,
              right: 0,
              bottom: 0,
              backgroundColor: "rgba(0, 0, 0, 0.5)",
              display: "flex",
              alignItems: "center",
              justifyContent: "center",
              zIndex: 1000,
            }}
            onClick={() => !isSubmitting && setActiveId(null)}
            role="presentation"
          >
            <div
              className="modal-card"
              role="dialog"
              aria-modal="true"
              onClick={(e) => e.stopPropagation()}
              style={{
                backgroundColor: "white",
                borderRadius: "12px",
                padding: "24px",
                maxWidth: "600px",
                width: "90%",
                maxHeight: "90vh",
                overflowY: "auto",
              }}
            >
              <div className="modal-head">
                <div>
                  <p className="eyebrow">Review request</p>
                  <h2>{`${activeRequest.employeeEmail}`}</h2>
                  <p className="muted">
                    {activeRequest.leaveType} ·{" "}
                    {formatDateDisplay(activeRequest.startDate)} →{" "}
                    {formatDateDisplay(activeRequest.endDate)} (
                    {activeRequest.numberOfDays} days)
                  </p>
                  {activeRequest.notes && (
                    <p
                      style={{
                        marginTop: "0.5rem",
                        color: "var(--text-muted)",
                        fontSize: "0.875rem",
                      }}
                    >
                      <strong>Notes:</strong> {activeRequest.notes}
                    </p>
                  )}
                </div>
                <button
                  type="button"
                  className="secondary-btn"
                  onClick={() => setActiveId(null)}
                  disabled={isSubmitting}
                >
                  {t.close}
                </button>
              </div>

              {activeRequest.attachmentUrl && (
                <div style={{ marginBottom: "1rem" }}>
                  <button
                    type="button"
                    onClick={() =>
                      handleDownloadAttachment(activeRequest.attachmentUrl!)
                    }
                    className="secondary-btn"
                    style={{ width: "100%" }}
                  >
                    📄 Download Attachment
                  </button>
                </div>
              )}

              {error && (
                <div
                  style={{
                    color: "var(--danger)",
                    backgroundColor: "#fee",
                    padding: "0.75rem 1rem",
                    borderRadius: "6px",
                    marginBottom: "1rem",
                    fontSize: "0.875rem",
                  }}
                >
                  {error}
                </div>
              )}

              <label className="input-group" style={{ marginBottom: 14 }}>
                <span style={{ fontWeight: 600 }}>{t.commentOptional}</span>
                <textarea
                  className="input textarea"
                  rows={4}
                  value={comment}
                  onChange={(e) => setComment(e.target.value)}
                  placeholder={
                    t.commentPlaceholder || "Add a comment (optional)"
                  }
                  disabled={isSubmitting}
                />
              </label>

              <div className="modal-actions">
                <button
                  type="button"
                  className="secondary-btn"
                  onClick={() => handleHRAction("reject")}
                  disabled={isSubmitting || submittingId === activeRequest.id}
                  style={{
                    backgroundColor: isSubmitting ? "#e5e7eb" : undefined,
                    cursor: isSubmitting ? "not-allowed" : "pointer",
                  }}
                >
                  {isSubmitting && submittingId === activeRequest.id
                    ? "Processing..."
                    : t.reject || "Reject"}
                </button>
                <button
                  type="button"
                  className="primary-btn"
                  onClick={() => handleHRAction("approve")}
                  disabled={isSubmitting || submittingId === activeRequest.id}
                  style={{
                    opacity: isSubmitting ? 0.6 : 1,
                    cursor: isSubmitting ? "not-allowed" : "pointer",
                  }}
                >
                  {isSubmitting && submittingId === activeRequest.id
                    ? "Processing..."
                    : t.approve || "Approve"}
                </button>
              </div>
            </div>
          </div>
        )}

        {/* History Modal */}
        {historyModalOpen && (
          <div
            style={{
              position: "fixed",
              top: 0,
              left: 0,
              right: 0,
              bottom: 0,
              backgroundColor: "rgba(0, 0, 0, 0.5)",
              display: "flex",
              alignItems: "center",
              justifyContent: "center",
              zIndex: 1000,
              padding: "20px",
            }}
            onClick={() => setHistoryModalOpen(false)}
            role="presentation"
          >
            <div
              className="modal-card"
              role="dialog"
              aria-modal="true"
              onClick={(e) => e.stopPropagation()}
              style={{
                backgroundColor: "white",
                borderRadius: "12px",
                padding: "0",
                maxWidth: "800px",
                width: "100%",
                maxHeight: "85vh",
                display: "flex",
                flexDirection: "column",
                overflow: "hidden",
              }}
            >
              {/* Modal Header - Fixed */}
              <div
                style={{
                  padding: "24px",
                  borderBottom: "1px solid var(--brand-border)",
                  display: "flex",
                  justifyContent: "space-between",
                  alignItems: "center",
                  flexShrink: 0,
                }}
              >
                <div>
                  <p className="eyebrow" style={{ margin: "0 0 4px 0" }}>
                    Request History
                  </p>
                  <h2 style={{ margin: 0, fontSize: "1.5rem" }}>
                    Leave Request #{selectedHistoryRequestId}
                  </h2>
                </div>
                <button
                  type="button"
                  className="secondary-btn"
                  onClick={() => setHistoryModalOpen(false)}
                  style={{ flexShrink: 0 }}
                >
                  {t.close || "Close"}
                </button>
              </div>

              {/* Modal Content - Scrollable */}
              <div
                style={{
                  padding: "24px",
                  overflowY: "auto",
                  flex: 1,
                  minHeight: 0,
                }}
              >
                {loadingHistory ? (
                  <div
                    style={{
                      padding: "2rem",
                      textAlign: "center",
                      color: "var(--text-muted)",
                    }}
                  >
                    Loading history...
                  </div>
                ) : history.length === 0 ? (
                  <div
                    style={{
                      padding: "2rem",
                      textAlign: "center",
                      color: "var(--text-muted)",
                    }}
                  >
                    No history available for this request
                  </div>
                ) : (
                  <div
                    style={{
                      display: "flex",
                      flexDirection: "column",
                      gap: "1rem",
                    }}
                  >
                    {history.map((log) => (
                      <div
                        key={log.id}
                        style={{
                          padding: "1rem",
                          border: "1px solid var(--brand-border)",
                          borderRadius: "8px",
                          backgroundColor: "var(--brand-surface-muted)",
                        }}
                      >
                        <div
                          style={{
                            display: "flex",
                            justifyContent: "space-between",
                            alignItems: "flex-start",
                            marginBottom: "0.75rem",
                            flexWrap: "wrap",
                            gap: "0.5rem",
                          }}
                        >
                          <div style={{ flex: 1, minWidth: "200px" }}>
                            <div style={{ marginBottom: "0.5rem" }}>
                              <strong style={{ fontSize: "1rem" }}>
                                {log.action}
                              </strong>
                              {log.newStatus && (
                                <span
                                  style={{
                                    marginLeft: "0.5rem",
                                    color: "var(--text-muted)",
                                    fontSize: "0.875rem",
                                  }}
                                >
                                  → {formatStatusName(log.newStatus)}
                                </span>
                              )}
                            </div>
                            <div
                              style={{
                                fontSize: "0.875rem",
                                color: "var(--text-muted)",
                              }}
                            >
                              <strong>By:</strong> {log.actionBy}
                            </div>
                          </div>
                          <div
                            style={{
                              fontSize: "0.875rem",
                              color: "var(--text-muted)",
                              textAlign: "right",
                              whiteSpace: "nowrap",
                            }}
                          >
                            {formatDateTimeDisplay(log.actionDate)}
                          </div>
                        </div>
                        {log.comment && (
                          <div
                            style={{
                              marginTop: "0.75rem",
                              padding: "0.75rem",
                              backgroundColor: "white",
                              borderRadius: "6px",
                              fontSize: "0.875rem",
                              border: "1px solid var(--brand-border)",
                              lineHeight: "1.5",
                            }}
                          >
                            <strong
                              style={{
                                display: "block",
                                marginBottom: "0.25rem",
                              }}
                            >
                              Comment:
                            </strong>
                            <span>{log.comment}</span>
                          </div>
                        )}
                      </div>
                    ))}
                  </div>
                )}
              </div>
            </div>
          </div>
        )}
      </main>
    </>
  );
}
