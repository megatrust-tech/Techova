"use client";

import { useEffect, useMemo, useState, useRef } from "react";
import DateRangePicker from "@/components/common/DateRangePicker";
import Header from "@/components/common/Header";
import ar from "@/locales/ar.json";
import en from "@/locales/en.json";
import {
  fetchLeaveTypes,
  fetchLeaveStatuses,
  submitLeaveRequest,
  fetchMyLeaves,
  fetchRemainingLeaves,
  cancelLeaveRequest,
  fetchLeaveRequestHistory,
  LeaveType,
  LeaveStatus,
  MyLeaveRequest,
  LeaveBalanceSummaryDto,
  LeaveAuditLogDto,
} from "@/lib/api/leaves";

export default function LeaveRequestPage() {
  const [locale, setLocale] = useState<"en" | "ar">("en");
  const t = useMemo(() => ({ en, ar }[locale]), [locale]);

  const [leaveBalance, setLeaveBalance] = useState<LeaveBalanceSummaryDto[]>(
    []
  );
  const [loadingBalance, setLoadingBalance] = useState(true);
  const [modalOpen, setModalOpen] = useState(false);
  const [balanceDetailModalOpen, setBalanceDetailModalOpen] = useState(false);
  const [datePickerExpanded, setDatePickerExpanded] = useState(false);
  const [dateRange, setDateRange] = useState<{
    start: Date | null;
    end: Date | null;
  }>({
    start: null,
    end: null,
  });
  const [leaveTypes, setLeaveTypes] = useState<LeaveType[]>([]);
  const [leaveStatuses, setLeaveStatuses] = useState<LeaveStatus[]>([]);
  const [myLeaves, setMyLeaves] = useState<MyLeaveRequest[]>([]);
  const [loadingLeaves, setLoadingLeaves] = useState(true);
  const [selectedLeaveType, setSelectedLeaveType] = useState<number | "">("");
  const [notes, setNotes] = useState<string>("");
  const [attachment, setAttachment] = useState<File | null>(null);
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [success, setSuccess] = useState<string | null>(null);
  const [selectedStatusFilter, setSelectedStatusFilter] = useState<
    number | "all"
  >("all");
  const [cancellingId, setCancellingId] = useState<number | null>(null);
  const [historyModalOpen, setHistoryModalOpen] = useState(false);
  const [selectedRequestId, setSelectedRequestId] = useState<number | null>(
    null
  );
  const [history, setHistory] = useState<LeaveAuditLogDto[]>([]);
  const [loadingHistory, setLoadingHistory] = useState(false);
  const formRef = useRef<HTMLFormElement>(null);
  const fileInputRef = useRef<HTMLInputElement>(null);
  const dateInputRef = useRef<HTMLDivElement>(null);

  useEffect(() => {
    document.documentElement.lang = locale;
    document.documentElement.dir = locale === "ar" ? "rtl" : "ltr";
  }, [locale]);

  useEffect(() => {
    // Fetch leave types, statuses, my leaves, and balance
    const loadData = async () => {
      try {
        const [types, statuses, leaves, balance] = await Promise.all([
          fetchLeaveTypes(),
          fetchLeaveStatuses(),
          fetchMyLeaves(),
          fetchRemainingLeaves(),
        ]);
        setLeaveTypes(types);
        setLeaveStatuses(statuses);
        // Handle paginated response structure: extract items if it's an object, otherwise use data directly if it's an array
        let leavesArray: MyLeaveRequest[] = [];
        if (Array.isArray(leaves)) {
          leavesArray = leaves;
        } else if (leaves && typeof leaves === "object" && "items" in leaves) {
          const items = (leaves as { items: unknown }).items;
          leavesArray = Array.isArray(items) ? items : [];
        }
        setMyLeaves(leavesArray);
        setLeaveBalance(balance);
      } catch (err) {
        setError(
          err instanceof Error
            ? err.message
            : "Failed to load leave data. Please refresh the page."
        );
      } finally {
        setLoadingLeaves(false);
        setLoadingBalance(false);
      }
    };

    loadData();
  }, []);

  const handleSubmit = async (e: React.FormEvent<HTMLFormElement>) => {
    e.preventDefault();
    setError(null);
    setSuccess(null);

    if (
      selectedLeaveType === "" ||
      selectedLeaveType === null ||
      selectedLeaveType === undefined
    ) {
      setError("Please select a leave type");
      return;
    }

    if (!dateRange.start || !dateRange.end) {
      setError("Please select start and end dates");
      return;
    }

    // Calculate working days (excluding weekends)
    const workingDays = calculateWorkingDays(dateRange.start, dateRange.end);

    if (workingDays <= 0) {
      setError(
        "Please select dates that include at least one working day (excluding weekends)"
      );
      return;
    }

    // Check if selected leave type has enough remaining days
    const remainingDays = getRemainingDaysForType(selectedLeaveType as number);
    if (workingDays > remainingDays) {
      setError(
        `Insufficient balance. You have ${remainingDays} day(s) remaining for this leave type, but you're requesting ${workingDays} working day(s).`
      );
      return;
    }

    if (notes && notes.length > 1000) {
      setError("Notes cannot exceed 1000 characters");
      return;
    }

    // Validate attachment file size (5 MB = 5 * 1024 * 1024 bytes)
    const maxFileSize = 5 * 1024 * 1024; // 5 MB
    if (attachment && attachment.size > maxFileSize) {
      setError(
        `Attachment file size (${(attachment.size / (1024 * 1024)).toFixed(
          2
        )} MB) exceeds the maximum allowed size of 5 MB. Please choose a smaller file.`
      );
      return;
    }

    setIsSubmitting(true);

    try {
      await submitLeaveRequest({
        type: selectedLeaveType as number,
        startDate: dateRange.start.toISOString(),
        endDate: dateRange.end.toISOString(),
        notes: notes || undefined,
        attachment: attachment || undefined,
      });

      setSuccess("Leave request submitted successfully");

      // Reset form
      setSelectedLeaveType("");
      setDateRange({ start: null, end: null });
      setNotes("");
      setAttachment(null);
      if (formRef.current) {
        formRef.current.reset();
      }
      if (fileInputRef.current) {
        fileInputRef.current.value = "";
      }
      setModalOpen(false);
      setDatePickerExpanded(false);

      // Refresh leave requests and balance
      try {
        const [leaves, balance] = await Promise.all([
          fetchMyLeaves(),
          fetchRemainingLeaves(),
        ]);
        // Handle paginated response structure: extract items if it's an object, otherwise use data directly if it's an array
        let leavesArray: MyLeaveRequest[] = [];
        if (Array.isArray(leaves)) {
          leavesArray = leaves;
        } else if (leaves && typeof leaves === "object" && "items" in leaves) {
          const items = (leaves as { items: unknown }).items;
          leavesArray = Array.isArray(items) ? items : [];
        }
        setMyLeaves(leavesArray);
        setLeaveBalance(balance);
      } catch (err) {
        console.error("Failed to refresh leaves:", err);
      }
    } catch (err) {
      setError(
        err instanceof Error
          ? err.message
          : "Failed to submit leave request. Please try again."
      );
    } finally {
      setIsSubmitting(false);
    }
  };

  // Check if a leave request can be cancelled (only pending statuses)
  const canCancelRequest = (status: string): boolean => {
    const statusLower = status.toLowerCase();
    return statusLower.includes("pending");
  };

  // Handle cancel leave request
  const handleCancelRequest = async (leaveId: number) => {
    setCancellingId(leaveId);
    setError(null);
    setSuccess(null);

    try {
      await cancelLeaveRequest(leaveId);

      setSuccess("Leave request cancelled successfully");

      // Refresh leave requests and balance
      try {
        const [leaves, balance] = await Promise.all([
          fetchMyLeaves(),
          fetchRemainingLeaves(),
        ]);
        // Handle paginated response structure: extract items if it's an object, otherwise use data directly if it's an array
        let leavesArray: MyLeaveRequest[] = [];
        if (Array.isArray(leaves)) {
          leavesArray = leaves;
        } else if (leaves && typeof leaves === "object" && "items" in leaves) {
          const items = (leaves as { items: unknown }).items;
          leavesArray = Array.isArray(items) ? items : [];
        }
        setMyLeaves(leavesArray);
        setLeaveBalance(balance);
      } catch (err) {
        console.error("Failed to refresh leaves:", err);
      }

      // Clear success message after 3 seconds
      setTimeout(() => setSuccess(null), 3000);
    } catch (err) {
      setError(
        err instanceof Error
          ? err.message
          : "Failed to cancel leave request. Please try again."
      );
    } finally {
      setCancellingId(null);
    }
  };

  // Handle view history
  const handleViewHistory = async (requestId: number) => {
    setSelectedRequestId(requestId);
    setHistoryModalOpen(true);
    setLoadingHistory(true);
    setHistory([]);

    try {
      const historyData = await fetchLeaveRequestHistory(requestId);
      setHistory(historyData);
    } catch (err) {
      console.error("Failed to load history:", err);
      setError(
        err instanceof Error
          ? err.message
          : "Failed to load request history. Please try again."
      );
    } finally {
      setLoadingHistory(false);
    }
  };

  // Format status name (e.g., "PendingManager" -> "Pending Manager", "PendingHR" -> "Pending HR")
  const formatStatusName = (status: string): string => {
    // Insert space before capital letters, but handle consecutive capitals (like HR) as a single unit
    return status
      .replace(/([a-z])([A-Z])/g, "$1 $2")
      .replace(/([A-Z]+)([A-Z][a-z])/g, "$1 $2")
      .trim();
  };

  // Get status color class
  const getStatusColorClass = (status: string): string => {
    const statusLower = status.toLowerCase();
    if (statusLower.includes("pending")) {
      return "status-pending";
    } else if (statusLower === "approved") {
      return "status-approved";
    } else if (statusLower === "rejected") {
      return "status-rejected";
    } else if (statusLower === "cancelled") {
      return "status-cancelled";
    }
    return "status-default";
  };

  // Get status color style
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

  // Calculate working days excluding weekends (Friday=5, Saturday=6)
  const calculateWorkingDays = (start: Date, end: Date): number => {
    let count = 0;
    const current = new Date(start);
    current.setHours(0, 0, 0, 0);
    const endDate = new Date(end);
    endDate.setHours(0, 0, 0, 0);

    while (current <= endDate) {
      const dayOfWeek = current.getDay();
      // Skip Friday (5) and Saturday (6)
      if (dayOfWeek !== 5 && dayOfWeek !== 6) {
        count++;
      }
      current.setDate(current.getDate() + 1);
    }

    return count;
  };

  // Calculate total remaining days
  const totalRemainingDays = useMemo(() => {
    return leaveBalance.reduce(
      (sum, balance) => sum + balance.remainingDays,
      0
    );
  }, [leaveBalance]);

  // Get remaining days for a specific leave type
  const getRemainingDaysForType = (typeValue: number): number => {
    const type = leaveTypes.find((t) => t.value === typeValue);
    if (!type) return 0;

    // Find balance by matching type name
    const balance = leaveBalance.find(
      (b) => b.type.toLowerCase() === type.name.toLowerCase()
    );
    return balance?.remainingDays || 0;
  };

  const filteredRequests = useMemo(() => {
    // FIX: Add safety check to ensure myLeaves is an array
    if (!Array.isArray(myLeaves)) {
      console.warn("myLeaves is not an array:", myLeaves);
      return [];
    }

    if (selectedStatusFilter === "all") {
      return myLeaves;
    }
    // Find the status enum value that matches the filter
    const statusEnum = leaveStatuses.find(
      (s) => s.value === selectedStatusFilter
    );
    if (!statusEnum) return myLeaves;

    // Filter by matching the status string
    return myLeaves.filter((req) => {
      // Compare status strings (e.g., "PendingManager" vs status enum name)
      return (
        req.status === statusEnum.name ||
        req.status.toLowerCase() === statusEnum.name.toLowerCase()
      );
    });
  }, [myLeaves, selectedStatusFilter, leaveStatuses]);

  // Format date as DD/MM/YYYY
  const formatDate = (date: Date): string => {
    const day = String(date.getDate()).padStart(2, "0");
    const month = String(date.getMonth() + 1).padStart(2, "0");
    const year = date.getFullYear();
    return `${day}/${month}/${year}`;
  };

  // Get formatted date range string
  const dateRangeString = useMemo(() => {
    if (dateRange.start && dateRange.end) {
      return `${formatDate(dateRange.start)} - ${formatDate(dateRange.end)}`;
    }
    return "";
  }, [dateRange]);

  // Close date picker when clicking outside
  useEffect(() => {
    const handleClickOutside = (event: MouseEvent) => {
      if (
        dateInputRef.current &&
        !dateInputRef.current.contains(event.target as Node)
      ) {
        setDatePickerExpanded(false);
      }
    };

    if (datePickerExpanded) {
      document.addEventListener("mousedown", handleClickOutside);
      return () => {
        document.removeEventListener("mousedown", handleClickOutside);
      };
    }
  }, [datePickerExpanded]);

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
            <p className="eyebrow">{t.leavesHeader}</p>
            <h1>{t.leavesHeader}</h1>
            <p className="muted">{t.leavesSubtitle}</p>
          </div>
          <div
            style={{
              backgroundColor: "white",
              borderRadius: "12px",
              padding: "24px",
              boxShadow: "0 1px 3px rgba(0, 0, 0, 0.1)",
              minWidth: "320px",
            }}
          >
            <h3
              style={{
                margin: "0 0 20px 0",
                fontSize: "1rem",
                fontWeight: "500",
                color: "var(--text-muted)",
              }}
            >
              {t.remainingBalance}
            </h3>

            {loadingBalance ? (
              <div
                style={{
                  padding: "20px",
                  textAlign: "center",
                  color: "var(--text-muted)",
                }}
              >
                Loading...
              </div>
            ) : (
              <>
                <div
                  style={{
                    display: "flex",
                    alignItems: "baseline",
                    gap: "8px",
                    marginBottom: "24px",
                  }}
                >
                  <span
                    style={{
                      fontSize: "2.5rem",
                      fontWeight: "700",
                      color: "var(--text-main)",
                      lineHeight: "1",
                    }}
                  >
                    {totalRemainingDays}
                  </span>
                  <span
                    style={{
                      fontSize: "1rem",
                      fontWeight: "500",
                      color: "var(--text-muted)",
                    }}
                  >
                    days
                  </span>
                </div>

                <button
                  type="button"
                  onClick={() => setBalanceDetailModalOpen(true)}
                  style={{
                    width: "100%",
                    padding: "8px 16px",
                    background: "transparent",
                    color: "var(--brand-primary)",
                    border: "1px solid var(--brand-primary)",
                    borderRadius: "6px",
                    fontSize: "0.875rem",
                    fontWeight: "500",
                    cursor: "pointer",
                    marginBottom: "16px",
                    transition: "all 0.2s",
                  }}
                  onMouseEnter={(e) => {
                    e.currentTarget.style.background =
                      "var(--brand-accent-soft)";
                  }}
                  onMouseLeave={(e) => {
                    e.currentTarget.style.background = "transparent";
                  }}
                >
                  Details
                </button>
              </>
            )}

            <button
              type="button"
              onClick={() => setModalOpen(true)}
              className="primary-btn"
              style={{
                padding: "8px 16px",
                fontSize: "0.875rem",
                borderRadius: "6px",
                fontWeight: 500,
              }}
            >
              {t.submitRequest}
            </button>
          </div>
        </header>

        <section className="card list-card">
          <div className="list-head">
            <h2>{t.myRequests}</h2>
            <div style={{ display: "flex", gap: "1rem", alignItems: "center" }}>
              <label htmlFor="statusFilter" style={{ margin: 0 }}>
                Filter by Status:
              </label>
              <select
                id="statusFilter"
                value={selectedStatusFilter}
                onChange={(e) =>
                  setSelectedStatusFilter(
                    e.target.value === "all"
                      ? "all"
                      : parseInt(e.target.value, 10)
                  )
                }
                className="input"
                style={{ minWidth: "150px" }}
              >
                <option value="all">All Statuses</option>
                {leaveStatuses.map((status) => (
                  <option key={status.value} value={status.value}>
                    {formatStatusName(status.name)}
                  </option>
                ))}
              </select>
            </div>
          </div>
          <div className="list-grid">
            <div className="list-row list-headings">
              <span>{t.titleCol}</span>
              <span>{t.fromCol}</span>
              <span>{t.toCol}</span>
              <span>{t.submittedCol}</span>
              <span>{t.statusCol}</span>
              <span>Audit</span>
              <span>Cancel</span>
            </div>
            {loadingLeaves ? (
              <div className="list-row">
                <div
                  style={{
                    gridColumn: "1 / -1",
                    textAlign: "center",
                    padding: "2rem",
                  }}
                >
                  Loading...
                </div>
              </div>
            ) : filteredRequests.length === 0 ? (
              <div className="list-row">
                <div
                  style={{
                    gridColumn: "1 / -1",
                    textAlign: "center",
                    padding: "2rem",
                  }}
                >
                  No leave requests found
                </div>
              </div>
            ) : (
              filteredRequests.map((req) => (
                <div className="list-row" key={req.id}>
                  <span>{req.leaveType}</span>
                  <span>{formatDateDisplay(req.startDate)}</span>
                  <span>{formatDateDisplay(req.endDate)}</span>
                  <span>{formatDateDisplay(req.createdAt)}</span>
                  <span>
                    <span
                      className={`status-chip ${getStatusColorClass(
                        req.status
                      )}`}
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
                  <span
                    style={{
                      minWidth: "80px",
                      display: "flex",
                      justifyContent: "flex-start",
                    }}
                  >
                    <button
                      type="button"
                      className="secondary-btn slim"
                      onClick={() => handleViewHistory(req.id)}
                      style={{
                        padding: "6px 12px",
                        fontSize: "0.875rem",
                      }}
                    >
                      History
                    </button>
                  </span>
                  <span
                    style={{
                      minWidth: "80px",
                      display: "flex",
                      justifyContent: "flex-start",
                    }}
                  >
                    {canCancelRequest(req.status) && (
                      <button
                        type="button"
                        className="secondary-btn slim"
                        onClick={() => handleCancelRequest(req.id)}
                        disabled={cancellingId === req.id}
                        style={{
                          opacity: cancellingId === req.id ? 0.6 : 1,
                          cursor:
                            cancellingId === req.id ? "not-allowed" : "pointer",
                          padding: "6px 12px",
                          fontSize: "0.875rem",
                        }}
                      >
                        {cancellingId === req.id ? "Cancelling..." : "Cancel"}
                      </button>
                    )}
                  </span>
                </div>
              ))
            )}
          </div>
        </section>
      </main>

      {/* Modal Popup */}
      {modalOpen && (
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
          onClick={() => setModalOpen(false)}
        >
          <div
            style={{
              backgroundColor: "white",
              borderRadius: "8px",
              padding: "2rem",
              maxWidth: "600px",
              width: "90%",
              maxHeight: "90vh",
              overflowY: "auto",
              position: "relative",
            }}
            onClick={(e) => e.stopPropagation()}
          >
            <div
              style={{
                display: "flex",
                justifyContent: "space-between",
                alignItems: "center",
                marginBottom: "1.5rem",
              }}
            >
              <h2 style={{ margin: 0 }}>Submit Leave Request</h2>
              <button
                type="button"
                onClick={() => {
                  setModalOpen(false);
                  setDatePickerExpanded(false);
                }}
                style={{
                  background: "none",
                  border: "none",
                  fontSize: "1.5rem",
                  cursor: "pointer",
                  color: "#666",
                }}
              >
                ×
              </button>
            </div>

            <form ref={formRef} onSubmit={handleSubmit}>
              {error && (
                <div
                  style={{
                    color: "red",
                    marginBottom: "1rem",
                    padding: "0.5rem",
                    backgroundColor: "#fee",
                    borderRadius: "4px",
                  }}
                >
                  {error}
                </div>
              )}
              {success && (
                <div
                  style={{
                    color: "green",
                    marginBottom: "1rem",
                    padding: "0.5rem",
                    backgroundColor: "#efe",
                    borderRadius: "4px",
                  }}
                >
                  {success}
                </div>
              )}

              <div style={{ marginBottom: "1rem" }}>
                <label
                  htmlFor="leaveType"
                  style={{
                    display: "block",
                    marginBottom: "0.5rem",
                    fontWeight: "500",
                  }}
                >
                  Leave Type
                </label>
                <select
                  id="leaveType"
                  name="leaveType"
                  value={selectedLeaveType}
                  onChange={(e) =>
                    setSelectedLeaveType(
                      e.target.value === "" ? "" : parseInt(e.target.value, 10)
                    )
                  }
                  className="input"
                  required
                  disabled={isSubmitting}
                  style={{ width: "100%" }}
                >
                  <option value="">Select leave type</option>
                  {leaveTypes.map((type) => (
                    <option key={type.value} value={type.value}>
                      {type.name}
                    </option>
                  ))}
                </select>
              </div>

              <div style={{ marginBottom: "1rem", position: "relative" }}>
                <label
                  style={{
                    display: "block",
                    marginBottom: "0.5rem",
                    fontWeight: "500",
                  }}
                >
                  Date
                </label>
                <div ref={dateInputRef} style={{ position: "relative" }}>
                  <div
                    style={{
                      display: "flex",
                      alignItems: "center",
                      border: "1px solid #ddd",
                      borderRadius: "4px",
                      padding: "0.5rem",
                      backgroundColor: "white",
                      cursor: "pointer",
                    }}
                    onClick={() => setDatePickerExpanded(!datePickerExpanded)}
                  >
                    <input
                      type="text"
                      readOnly
                      value={dateRangeString}
                      placeholder="Select date range"
                      style={{
                        border: "none",
                        outline: "none",
                        flex: 1,
                        cursor: "pointer",
                        fontSize: "0.875rem",
                      }}
                    />
                    {dateRangeString && (
                      <button
                        type="button"
                        onClick={(e) => {
                          e.stopPropagation();
                          setDateRange({ start: null, end: null });
                        }}
                        style={{
                          background: "none",
                          border: "none",
                          cursor: "pointer",
                          color: "#999",
                          padding: "0 0.5rem",
                          fontSize: "1.2rem",
                        }}
                      >
                        ×
                      </button>
                    )}
                    <span
                      style={{
                        color: "#999",
                        padding: "0 0.5rem",
                        fontSize: "1.2rem",
                      }}
                    >
                      📅
                    </span>
                  </div>
                  {datePickerExpanded && (
                    <div
                      style={{
                        position: "absolute",
                        top: "100%",
                        left: 0,
                        right: 0,
                        marginTop: "0.5rem",
                        backgroundColor: "white",
                        border: "1px solid #ddd",
                        borderRadius: "4px",
                        padding: "1rem",
                        boxShadow: "0 4px 6px rgba(0, 0, 0, 0.1)",
                        zIndex: 1001,
                      }}
                      onClick={(e) => e.stopPropagation()}
                    >
                      <DateRangePicker
                        monthsToShow={1}
                        selectionMode="range"
                        weekStartsOn={6}
                        value={dateRange}
                        onChange={(value) => {
                          setDateRange(value);
                          if (value.start && value.end) {
                            setDatePickerExpanded(false);
                          }
                        }}
                      />
                    </div>
                  )}
                </div>
                {dateRange.start && dateRange.end && (
                  <div
                    style={{
                      marginTop: "0.5rem",
                      padding: "0.75rem",
                      backgroundColor: "var(--brand-accent-soft)",
                      borderRadius: "6px",
                      fontSize: "0.875rem",
                    }}
                  >
                    <div style={{ fontWeight: "500", marginBottom: "0.25rem" }}>
                      <strong>
                        {calculateWorkingDays(dateRange.start, dateRange.end)}
                      </strong>{" "}
                      working day(s) selected (weekends excluded)
                    </div>
                    {selectedLeaveType !== "" && (
                      <div
                        style={{
                          marginTop: "0.5rem",
                          paddingTop: "0.5rem",
                          borderTop: "1px solid var(--brand-border)",
                          color:
                            calculateWorkingDays(
                              dateRange.start,
                              dateRange.end
                            ) >
                            getRemainingDaysForType(selectedLeaveType as number)
                              ? "var(--danger)"
                              : "var(--text-muted)",
                        }}
                      >
                        Remaining balance:{" "}
                        <strong>
                          {getRemainingDaysForType(selectedLeaveType as number)}{" "}
                          day(s)
                        </strong>
                        {calculateWorkingDays(dateRange.start, dateRange.end) >
                          getRemainingDaysForType(
                            selectedLeaveType as number
                          ) && (
                          <span
                            style={{ display: "block", marginTop: "0.25rem" }}
                          >
                            ⚠️ Requested days exceed available balance
                          </span>
                        )}
                      </div>
                    )}
                  </div>
                )}
              </div>

              <div style={{ marginBottom: "1rem" }}>
                <label
                  htmlFor="notes"
                  style={{
                    display: "block",
                    marginBottom: "0.5rem",
                    fontWeight: "500",
                  }}
                >
                  Notes (optional, max 1000 characters)
                </label>
                <textarea
                  id="notes"
                  name="notes"
                  value={notes}
                  onChange={(e) => setNotes(e.target.value)}
                  className="input"
                  rows={4}
                  maxLength={1000}
                  disabled={isSubmitting}
                  style={{ width: "100%", resize: "vertical" }}
                />
                <p className="muted small" style={{ marginTop: "0.25rem" }}>
                  {notes.length}/1000 characters
                </p>
              </div>

              <div style={{ marginBottom: "1.5rem" }}>
                <label
                  htmlFor="attachment"
                  style={{
                    display: "block",
                    marginBottom: "0.5rem",
                    fontWeight: "500",
                  }}
                >
                  Attachment (optional, max 5 MB)
                </label>
                <input
                  ref={fileInputRef}
                  id="attachment"
                  name="attachment"
                  type="file"
                  onChange={(e) => {
                    const file = e.target.files?.[0] || null;
                    if (file) {
                      const maxFileSize = 5 * 1024 * 1024; // 5 MB
                      if (file.size > maxFileSize) {
                        setError(
                          `File size (${(file.size / (1024 * 1024)).toFixed(
                            2
                          )} MB) exceeds the maximum allowed size of 5 MB. Please choose a smaller file.`
                        );
                        e.target.value = ""; // Clear the input
                        setAttachment(null);
                        return;
                      }
                      setError(null); // Clear any previous errors
                    }
                    setAttachment(file);
                  }}
                  disabled={isSubmitting}
                  style={{ width: "100%" }}
                />
              </div>

              <button
                type="submit"
                className="primary-btn"
                disabled={isSubmitting}
                style={{ width: "100%" }}
              >
                {isSubmitting ? "Submitting..." : "Submit Leave Request"}
              </button>
            </form>
          </div>
        </div>
      )}

      {/* Balance Detail Modal */}
      {balanceDetailModalOpen && (
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
          onClick={() => setBalanceDetailModalOpen(false)}
        >
          <div
            style={{
              backgroundColor: "white",
              borderRadius: "12px",
              padding: "24px",
              maxWidth: "500px",
              width: "90%",
              maxHeight: "90vh",
              overflowY: "auto",
              position: "relative",
            }}
            onClick={(e) => e.stopPropagation()}
          >
            <div
              style={{
                display: "flex",
                justifyContent: "space-between",
                alignItems: "center",
                marginBottom: "20px",
              }}
            >
              <h2 style={{ margin: 0, fontSize: "1.25rem", fontWeight: "600" }}>
                Leave Balance Details
              </h2>
              <button
                type="button"
                onClick={() => setBalanceDetailModalOpen(false)}
                style={{
                  background: "none",
                  border: "none",
                  fontSize: "1.5rem",
                  cursor: "pointer",
                  color: "#666",
                  padding: "0",
                  width: "32px",
                  height: "32px",
                  display: "flex",
                  alignItems: "center",
                  justifyContent: "center",
                }}
              >
                ×
              </button>
            </div>

            {loadingBalance ? (
              <div
                style={{
                  padding: "20px",
                  textAlign: "center",
                  color: "var(--text-muted)",
                }}
              >
                Loading...
              </div>
            ) : leaveBalance.length > 0 ? (
              <div
                style={{
                  display: "flex",
                  flexDirection: "column",
                  gap: "16px",
                }}
              >
                {leaveBalance.map((balance, index) => (
                  <div
                    key={index}
                    style={{
                      padding: "16px",
                      backgroundColor: "var(--brand-surface-muted)",
                      borderRadius: "8px",
                      border: "1px solid var(--brand-border)",
                    }}
                  >
                    <div
                      style={{
                        display: "flex",
                        alignItems: "baseline",
                        gap: "8px",
                        marginBottom: "8px",
                      }}
                    >
                      <span
                        style={{
                          fontSize: "2rem",
                          fontWeight: "700",
                          color: "var(--text-main)",
                          lineHeight: "1",
                        }}
                      >
                        {balance.remainingDays}
                      </span>
                      <span
                        style={{
                          fontSize: "1rem",
                          fontWeight: "500",
                          color: "var(--text-main)",
                        }}
                      >
                        {balance.type}
                      </span>
                    </div>
                    <p
                      style={{
                        margin: 0,
                        fontSize: "0.875rem",
                        color: "var(--text-muted)",
                      }}
                    >
                      {balance.usedDays} / {balance.totalDays} days used
                    </p>
                  </div>
                ))}
              </div>
            ) : (
              <div
                style={{
                  padding: "20px",
                  textAlign: "center",
                  color: "var(--text-muted)",
                }}
              >
                No balance data available
              </div>
            )}
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
                  Leave Request #{selectedRequestId}
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
    </>
  );
}
