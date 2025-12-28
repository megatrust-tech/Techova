"use client";

import { useEffect, useState, useRef } from "react";
import { useRouter } from "next/navigation";
import Header from "@/components/common/Header";
import { getUserRole, searchUsers, UserSearchResult } from "@/lib/api/auth";
import {
    fetchUserBalances,
    updateUserBalances,
    UserBalanceDto,
    PaginatedUserBalancesDto,
    BalanceUpdateItem,
} from "@/lib/api/leaves";

const PAGE_SIZE_OPTIONS = [25, 50, 100];
const DEFAULT_PAGE_SIZE = 50;
const CURRENT_YEAR = new Date().getFullYear();
const YEAR_OPTIONS = [CURRENT_YEAR - 1, CURRENT_YEAR, CURRENT_YEAR + 1];

// Leave type IDs for consistent ordering
const LEAVE_TYPES = [
    { id: 0, name: "Annual" },
    { id: 1, name: "Sick" },
    { id: 2, name: "Emergency" },
    { id: 3, name: "Unpaid" },
    { id: 4, name: "Maternity" },
    { id: 5, name: "Paternity" },
];

export default function LeaveBalancesPage() {
    const [paginatedData, setPaginatedData] =
        useState<PaginatedUserBalancesDto | null>(null);
    const [selectedUserIds, setSelectedUserIds] = useState<Set<number>>(
        new Set()
    );
    const [currentPage, setCurrentPage] = useState(1);
    const [pageSize, setPageSize] = useState(DEFAULT_PAGE_SIZE);
    const [selectedYear, setSelectedYear] = useState(CURRENT_YEAR);
    const [loading, setLoading] = useState(true);
    const [updating, setUpdating] = useState(false);
    const [error, setError] = useState<string | null>(null);
    const [successMsg, setSuccessMsg] = useState<string | null>(null);
    const [showModal, setShowModal] = useState(false);
    const [updateValues, setUpdateValues] = useState<Record<number, number>>({});
    const [enabledLeaveTypes, setEnabledLeaveTypes] = useState<Set<number>>(
        new Set()
    );
    const [searchQuery, setSearchQuery] = useState("");
    const [searchResults, setSearchResults] = useState<UserSearchResult[]>([]);
    const [showSearchDropdown, setShowSearchDropdown] = useState(false);
    const [searchLoading, setSearchLoading] = useState(false);
    const [highlightedUserId, setHighlightedUserId] = useState<number | null>(null);
    const searchTimeoutRef = useRef<NodeJS.Timeout | null>(null);
    const searchContainerRef = useRef<HTMLDivElement>(null);
    const router = useRouter();

    useEffect(() => {
        const role = getUserRole();
        if (role !== "Admin") {
            router.push("/");
            return;
        }
        loadUsers(1, DEFAULT_PAGE_SIZE, CURRENT_YEAR);
    }, [router]);

    const loadUsers = async (page: number, size: number, year: number) => {
        try {
            setLoading(true);
            setError(null);
            const data = await fetchUserBalances(page, size, year);
            setPaginatedData(data);
            setCurrentPage(page);
            setSelectedUserIds(new Set());
        } catch (err) {
            console.error("Fetch error:", err);
            setError(
                err instanceof Error ? err.message : "An error occurred fetching users"
            );
        } finally {
            setLoading(false);
        }
    };

    const handleYearChange = (year: number) => {
        setSelectedYear(year);
        setHighlightedUserId(null);
        loadUsers(1, pageSize, year);
    };

    // Search handler with debounce
    const handleSearchChange = (query: string) => {
        setSearchQuery(query);

        if (searchTimeoutRef.current) {
            clearTimeout(searchTimeoutRef.current);
        }

        if (query.length < 2) {
            setSearchResults([]);
            setShowSearchDropdown(false);
            return;
        }

        setSearchLoading(true);
        searchTimeoutRef.current = setTimeout(async () => {
            try {
                const results = await searchUsers(query);
                setSearchResults(results);
                setShowSearchDropdown(true);
            } catch (err) {
                console.error("Search error:", err);
                setSearchResults([]);
            } finally {
                setSearchLoading(false);
            }
        }, 300);
    };

    // Handle selecting a user from search results
    const handleSelectSearchResult = (user: UserSearchResult) => {
        setSearchQuery("");
        setSearchResults([]);
        setShowSearchDropdown(false);

        // Select this user and open the update modal
        setSelectedUserIds(new Set([user.id]));
        setUpdateValues({});
        setEnabledLeaveTypes(new Set());
        setShowModal(true);
    };

    // Close search dropdown when clicking outside
    useEffect(() => {
        const handleClickOutside = (event: MouseEvent) => {
            if (searchContainerRef.current && !searchContainerRef.current.contains(event.target as Node)) {
                setShowSearchDropdown(false);
            }
        };
        document.addEventListener("mousedown", handleClickOutside);
        return () => document.removeEventListener("mousedown", handleClickOutside);
    }, []);

    const users: UserBalanceDto[] = paginatedData?.items ?? [];

    const handleSelectAll = () => {
        if (selectedUserIds.size === users.length) {
            setSelectedUserIds(new Set());
        } else {
            setSelectedUserIds(new Set(users.map((u) => u.userId)));
        }
    };

    const handleSelectUser = (userId: number) => {
        const newSet = new Set(selectedUserIds);
        if (newSet.has(userId)) {
            newSet.delete(userId);
        } else {
            newSet.add(userId);
        }
        setSelectedUserIds(newSet);
    };

    const handlePageChange = (newPage: number) => {
        if (newPage < 1 || (paginatedData && newPage > paginatedData.totalPages)) {
            return;
        }
        loadUsers(newPage, pageSize, selectedYear);
    };

    const handlePageSizeChange = (newSize: number) => {
        setPageSize(newSize);
        loadUsers(1, newSize, selectedYear);
    };

    const openUpdateModal = () => {
        setUpdateValues({});
        setEnabledLeaveTypes(new Set());
        setShowModal(true);
    };

    const handleToggleLeaveType = (leaveTypeId: number) => {
        const newSet = new Set(enabledLeaveTypes);
        if (newSet.has(leaveTypeId)) {
            newSet.delete(leaveTypeId);
        } else {
            newSet.add(leaveTypeId);
        }
        setEnabledLeaveTypes(newSet);
    };

    const handleUpdateValue = (leaveTypeId: number, value: string) => {
        const numValue = parseInt(value) || 0;
        setUpdateValues((prev) => ({ ...prev, [leaveTypeId]: numValue }));
    };

    const handleSubmitUpdate = async () => {
        if (selectedUserIds.size === 0 || enabledLeaveTypes.size === 0) return;

        const updates: BalanceUpdateItem[] = Array.from(enabledLeaveTypes).map(
            (leaveTypeId) => ({
                leaveTypeId,
                newTotalDays: updateValues[leaveTypeId] || 0,
            })
        );

        try {
            setUpdating(true);
            setError(null);
            setSuccessMsg(null);

            const result = await updateUserBalances({
                userIds: Array.from(selectedUserIds),
                year: selectedYear,
                updates,
            });

            setSuccessMsg(result.message);
            setTimeout(() => setSuccessMsg(null), 5000);
            setShowModal(false);

            // Reload current page
            await loadUsers(currentPage, pageSize, selectedYear);
        } catch (err) {
            setError(
                err instanceof Error ? err.message : "Failed to update balances"
            );
        } finally {
            setUpdating(false);
        }
    };

    const getBalanceDisplay = (user: UserBalanceDto, leaveTypeId: number) => {
        const balance = user.balances.find((b) => b.leaveTypeId === leaveTypeId);
        if (!balance) return "-";
        return `${balance.remainingDays}/${balance.totalDays}`;
    };

    if (loading && !paginatedData) {
        return (
            <div
                style={{
                    display: "flex",
                    justifyContent: "center",
                    alignItems: "center",
                    height: "100vh",
                }}
            >
                <p className="muted">Loading users...</p>
            </div>
        );
    }

    return (
        <>
            <Header
                locale="en"
                onToggleLocale={() => { }}
                showLogout={true}
                labels={{
                    home: "Home",
                    why: "Why",
                    pricing: "Pricing",
                    resources: "Resources",
                    language: "Language",
                }}
            />
            <main className="page-shell">
                {/* Header Section */}
                <header className="page-header" style={{ marginBottom: "32px" }}>
                    <div>
                        <button
                            onClick={() => router.push("/home/admin")}
                            style={{
                                background: "none",
                                border: "none",
                                color: "#7b58ca",
                                cursor: "pointer",
                                fontSize: "14px",
                                padding: 0,
                                marginBottom: "8px",
                                display: "flex",
                                alignItems: "center",
                                gap: "4px",
                            }}
                        >
                            ← Back to Admin Dashboard
                        </button>
                        <p className="eyebrow">Administration</p>
                        <h1>Leave Balances for {selectedYear}</h1>
                        <p className="muted">
                            View and update leave balances for all users.
                        </p>
                    </div>
                    <div style={{ display: "flex", gap: "12px", alignItems: "center", flexWrap: "wrap" }}>
                        <label
                            style={{
                                display: "flex",
                                alignItems: "center",
                                gap: "8px",
                                fontSize: "14px",
                                color: "#64748b",
                            }}
                        >
                            Year:
                            <select
                                value={selectedYear}
                                onChange={(e) => handleYearChange(parseInt(e.target.value))}
                                style={{
                                    padding: "8px 12px",
                                    borderRadius: "8px",
                                    border: "1px solid #e2e8f0",
                                    fontSize: "14px",
                                    fontWeight: "500",
                                }}
                            >
                                {YEAR_OPTIONS.map((year) => (
                                    <option key={year} value={year}>
                                        {year}
                                    </option>
                                ))}
                            </select>
                        </label>
                        {selectedUserIds.size > 0 && (
                            <span
                                style={{
                                    fontSize: "14px",
                                    color: "#7b58ca",
                                    fontWeight: "500",
                                }}
                            >
                                {selectedUserIds.size} user{selectedUserIds.size !== 1 ? "s" : ""}{" "}
                                selected
                            </span>
                        )}
                        <button
                            onClick={openUpdateModal}
                            disabled={selectedUserIds.size === 0}
                            className="primary-btn"
                            style={{
                                minWidth: "160px",
                                opacity: selectedUserIds.size === 0 ? 0.6 : 1,
                            }}
                        >
                            Update Selected
                        </button>
                    </div>
                </header>

                {/* Error / Success Messages */}
                {error && (
                    <div
                        style={{
                            padding: "16px",
                            background: "#FEF2F2",
                            color: "#B91C1C",
                            borderRadius: "12px",
                            marginBottom: "24px",
                            border: "1px solid #FCA5A5",
                        }}
                    >
                        ⚠️ {error}
                    </div>
                )}

                {successMsg && (
                    <div
                        style={{
                            padding: "16px",
                            background: "#ECFDF5",
                            color: "#047857",
                            borderRadius: "12px",
                            marginBottom: "24px",
                            border: "1px solid #6EE7B7",
                        }}
                    >
                        ✅ {successMsg}
                    </div>
                )}

                {/* Main Content Card */}
                <section className="card">
                    <div
                        style={{
                            display: "flex",
                            justifyContent: "space-between",
                            alignItems: "center",
                            marginBottom: "24px",
                            flexWrap: "wrap",
                            gap: "16px",
                        }}
                    >
                        <h2 style={{ fontSize: "20px", fontWeight: "bold", margin: 0 }}>
                            All Users
                        </h2>
                        <div style={{ display: "flex", alignItems: "center", gap: "16px" }}>
                            <span
                                className="pill"
                                style={{ background: "#f1f5f9", color: "#64748b" }}
                            >
                                {paginatedData?.totalCount ?? 0} Total Users
                            </span>
                            <label
                                style={{
                                    display: "flex",
                                    alignItems: "center",
                                    gap: "8px",
                                    fontSize: "14px",
                                    color: "#64748b",
                                }}
                            >
                                Show:
                                <select
                                    value={pageSize}
                                    onChange={(e) =>
                                        handlePageSizeChange(parseInt(e.target.value))
                                    }
                                    style={{
                                        padding: "6px 12px",
                                        borderRadius: "8px",
                                        border: "1px solid #e2e8f0",
                                        fontSize: "14px",
                                    }}
                                >
                                    {PAGE_SIZE_OPTIONS.map((size) => (
                                        <option key={size} value={size}>
                                            {size}
                                        </option>
                                    ))}
                                </select>
                            </label>
                        </div>
                    </div>

                    {/* Search Box */}
                    <div
                        ref={searchContainerRef}
                        style={{ position: "relative", marginBottom: "16px" }}
                    >
                        <div style={{ position: "relative" }}>
                            <input
                                type="text"
                                placeholder="Search users by name or email (min 2 characters)..."
                                value={searchQuery}
                                onChange={(e) => handleSearchChange(e.target.value)}
                                style={{
                                    width: "100%",
                                    padding: "12px 16px",
                                    paddingLeft: "40px",
                                    borderRadius: "8px",
                                    border: "1px solid #e2e8f0",
                                    fontSize: "14px",
                                    outline: "none",
                                }}
                                onFocus={() => searchResults.length > 0 && setShowSearchDropdown(true)}
                            />
                            <span
                                style={{
                                    position: "absolute",
                                    left: "14px",
                                    top: "50%",
                                    transform: "translateY(-50%)",
                                    color: "#94a3b8",
                                }}
                            >
                                🔍
                            </span>
                            {searchLoading && (
                                <span
                                    style={{
                                        position: "absolute",
                                        right: "14px",
                                        top: "50%",
                                        transform: "translateY(-50%)",
                                        color: "#94a3b8",
                                        fontSize: "12px",
                                    }}
                                >
                                    Loading...
                                </span>
                            )}
                        </div>
                        {showSearchDropdown && searchResults.length > 0 && (
                            <div
                                style={{
                                    position: "absolute",
                                    top: "100%",
                                    left: 0,
                                    right: 0,
                                    background: "#fff",
                                    border: "1px solid #e2e8f0",
                                    borderRadius: "8px",
                                    boxShadow: "0 4px 12px rgba(0,0,0,0.1)",
                                    zIndex: 50,
                                    maxHeight: "300px",
                                    overflowY: "auto",
                                    marginTop: "4px",
                                }}
                            >
                                {searchResults.map((user) => (
                                    <button
                                        key={user.id}
                                        type="button"
                                        onClick={() => handleSelectSearchResult(user)}
                                        style={{
                                            width: "100%",
                                            padding: "12px 16px",
                                            textAlign: "left",
                                            border: "none",
                                            background: "transparent",
                                            cursor: "pointer",
                                            borderBottom: "1px solid #f1f5f9",
                                            display: "flex",
                                            flexDirection: "column",
                                            gap: "4px",
                                        }}
                                        onMouseEnter={(e) => e.currentTarget.style.background = "#f8fafc"}
                                        onMouseLeave={(e) => e.currentTarget.style.background = "transparent"}
                                    >
                                        <span style={{ fontWeight: "500", color: "#475569" }}>
                                            {user.firstName} {user.lastName}
                                        </span>
                                        <span style={{ fontSize: "13px", color: "#94a3b8" }}>
                                            {user.email} · {user.roleName}
                                        </span>
                                    </button>
                                ))}
                            </div>
                        )}
                        {showSearchDropdown && searchQuery.length >= 2 && searchResults.length === 0 && !searchLoading && (
                            <div
                                style={{
                                    position: "absolute",
                                    top: "100%",
                                    left: 0,
                                    right: 0,
                                    background: "#fff",
                                    border: "1px solid #e2e8f0",
                                    borderRadius: "8px",
                                    padding: "16px",
                                    textAlign: "center",
                                    color: "#94a3b8",
                                    marginTop: "4px",
                                }}
                            >
                                No users found
                            </div>
                        )}
                    </div>

                    <div style={{ overflowX: "auto", position: "relative" }}>
                        {loading && (
                            <div
                                style={{
                                    position: "absolute",
                                    inset: 0,
                                    background: "rgba(255,255,255,0.7)",
                                    display: "flex",
                                    alignItems: "center",
                                    justifyContent: "center",
                                    zIndex: 10,
                                }}
                            >
                                <p className="muted">Loading...</p>
                            </div>
                        )}
                        <table
                            style={{
                                width: "100%",
                                borderCollapse: "collapse",
                                minWidth: "1000px",
                            }}
                        >
                            <thead>
                                <tr
                                    style={{
                                        borderBottom: "2px solid #e2e8f0",
                                        textAlign: "left",
                                    }}
                                >
                                    <th style={{ padding: "12px", width: "48px" }}>
                                        <input
                                            type="checkbox"
                                            style={{
                                                width: "18px",
                                                height: "18px",
                                                accentColor: "#7b58ca",
                                                cursor: "pointer",
                                            }}
                                            checked={
                                                users.length > 0 &&
                                                selectedUserIds.size === users.length
                                            }
                                            onChange={handleSelectAll}
                                            disabled={users.length === 0}
                                        />
                                    </th>
                                    <th
                                        style={{ padding: "12px", color: "#64748b", width: "60px" }}
                                    >
                                        ID
                                    </th>
                                    <th style={{ padding: "12px", color: "#64748b" }}>Name</th>
                                    <th style={{ padding: "12px", color: "#64748b" }}>Email</th>
                                    {LEAVE_TYPES.map((lt) => (
                                        <th
                                            key={lt.id}
                                            style={{
                                                padding: "12px",
                                                color: "#64748b",
                                                textAlign: "center",
                                                fontSize: "13px",
                                            }}
                                        >
                                            {lt.name}
                                        </th>
                                    ))}
                                </tr>
                            </thead>
                            <tbody>
                                {users.length === 0 && !loading && (
                                    <tr>
                                        <td
                                            colSpan={4 + LEAVE_TYPES.length}
                                            style={{
                                                textAlign: "center",
                                                padding: "48px",
                                                color: "#94a3b8",
                                            }}
                                        >
                                            No users found.
                                        </td>
                                    </tr>
                                )}
                                {users.map((user) => (
                                    <tr
                                        key={user.userId}
                                        style={{
                                            borderBottom: "1px solid #f1f5f9",
                                            background: highlightedUserId === user.userId
                                                ? "#eff6ff"
                                                : selectedUserIds.has(user.userId)
                                                    ? "#f8fafc"
                                                    : "transparent",
                                            transition: "background 0.2s",
                                        }}
                                    >
                                        <td style={{ padding: "12px" }}>
                                            <input
                                                type="checkbox"
                                                style={{
                                                    width: "18px",
                                                    height: "18px",
                                                    accentColor: "#7b58ca",
                                                    cursor: "pointer",
                                                }}
                                                checked={selectedUserIds.has(user.userId)}
                                                onChange={() => handleSelectUser(user.userId)}
                                            />
                                        </td>
                                        <td
                                            style={{
                                                padding: "12px",
                                                color: "#94a3b8",
                                                fontSize: "13px",
                                            }}
                                        >
                                            {user.userId}
                                        </td>
                                        <td
                                            style={{
                                                padding: "12px",
                                                fontWeight: "500",
                                                color: "#475569",
                                            }}
                                        >
                                            {user.firstName} {user.lastName}
                                        </td>
                                        <td style={{ padding: "12px", color: "#64748b" }}>
                                            {user.email}
                                        </td>
                                        {LEAVE_TYPES.map((lt) => (
                                            <td
                                                key={lt.id}
                                                style={{
                                                    padding: "12px",
                                                    textAlign: "center",
                                                    fontSize: "13px",
                                                    color: "#475569",
                                                }}
                                            >
                                                {getBalanceDisplay(user, lt.id)}
                                            </td>
                                        ))}
                                    </tr>
                                ))}
                            </tbody>
                        </table>
                    </div>

                    {/* Pagination Controls */}
                    {paginatedData && paginatedData.totalPages > 1 && (
                        <div
                            style={{
                                display: "flex",
                                justifyContent: "space-between",
                                alignItems: "center",
                                marginTop: "24px",
                                paddingTop: "16px",
                                borderTop: "1px solid #e2e8f0",
                                flexWrap: "wrap",
                                gap: "16px",
                            }}
                        >
                            <p style={{ margin: 0, color: "#64748b", fontSize: "14px" }}>
                                Showing {(currentPage - 1) * pageSize + 1} -{" "}
                                {Math.min(currentPage * pageSize, paginatedData.totalCount)} of{" "}
                                {paginatedData.totalCount.toLocaleString()}
                            </p>
                            <div style={{ display: "flex", gap: "8px", alignItems: "center" }}>
                                <button
                                    onClick={() => handlePageChange(currentPage - 1)}
                                    disabled={currentPage === 1 || loading}
                                    style={{
                                        padding: "8px 16px",
                                        border: "1px solid #e2e8f0",
                                        borderRadius: "8px",
                                        background: currentPage > 1 ? "#fff" : "#f8fafc",
                                        color: currentPage > 1 ? "#475569" : "#94a3b8",
                                        cursor: currentPage > 1 ? "pointer" : "not-allowed",
                                        fontSize: "14px",
                                        fontWeight: "500",
                                    }}
                                >
                                    ← Previous
                                </button>
                                <span
                                    style={{
                                        display: "flex",
                                        alignItems: "center",
                                        gap: "8px",
                                        color: "#64748b",
                                        fontSize: "14px",
                                    }}
                                >
                                    Page
                                    <input
                                        type="number"
                                        min={1}
                                        max={paginatedData.totalPages}
                                        value={currentPage}
                                        onChange={(e) => {
                                            const val = parseInt(e.target.value);
                                            if (!isNaN(val) && val >= 1 && val <= paginatedData.totalPages) {
                                                handlePageChange(val);
                                            }
                                        }}
                                        onKeyDown={(e) => {
                                            if (e.key === "Enter") {
                                                const val = parseInt((e.target as HTMLInputElement).value);
                                                if (!isNaN(val) && val >= 1 && val <= paginatedData.totalPages) {
                                                    handlePageChange(val);
                                                }
                                            }
                                        }}
                                        style={{
                                            width: "60px",
                                            padding: "6px 8px",
                                            border: "1px solid #e2e8f0",
                                            borderRadius: "6px",
                                            textAlign: "center",
                                            fontSize: "14px",
                                        }}
                                        disabled={loading}
                                    />
                                    of {paginatedData.totalPages.toLocaleString()}
                                </span>
                                <button
                                    onClick={() => handlePageChange(currentPage + 1)}
                                    disabled={currentPage >= paginatedData.totalPages || loading}
                                    style={{
                                        padding: "8px 16px",
                                        border: "1px solid #e2e8f0",
                                        borderRadius: "8px",
                                        background:
                                            currentPage < paginatedData.totalPages
                                                ? "#fff"
                                                : "#f8fafc",
                                        color:
                                            currentPage < paginatedData.totalPages
                                                ? "#475569"
                                                : "#94a3b8",
                                        cursor:
                                            currentPage < paginatedData.totalPages
                                                ? "pointer"
                                                : "not-allowed",
                                        fontSize: "14px",
                                        fontWeight: "500",
                                    }}
                                >
                                    Next →
                                </button>
                            </div>
                        </div>
                    )}
                </section>
            </main>

            {/* Update Modal */}
            {showModal && (
                <div
                    style={{
                        position: "fixed",
                        inset: 0,
                        background: "rgba(0, 0, 0, 0.5)",
                        display: "flex",
                        alignItems: "center",
                        justifyContent: "center",
                        zIndex: 100,
                    }}
                    onClick={() => !updating && setShowModal(false)}
                >
                    <div
                        className="card"
                        style={{
                            width: "100%",
                            maxWidth: "500px",
                            margin: "16px",
                            maxHeight: "90vh",
                            overflow: "auto",
                        }}
                        onClick={(e) => e.stopPropagation()}
                    >
                        <h2 style={{ marginBottom: "8px" }}>Update Leave Balances</h2>
                        <p className="muted" style={{ marginBottom: "24px" }}>
                            Set new total days for {selectedUserIds.size} selected user
                            {selectedUserIds.size !== 1 ? "s" : ""}. Only enabled leave types
                            will be updated.
                        </p>

                        {selectedUserIds.size > 1000 && (
                            <div
                                style={{
                                    padding: "12px",
                                    background: "#FEF3C7",
                                    color: "#92400E",
                                    borderRadius: "8px",
                                    marginBottom: "16px",
                                    fontSize: "14px",
                                    border: "1px solid #FCD34D",
                                }}
                            >
                                ⚠️ Maximum 1000 users per request. Please select fewer users.
                            </div>
                        )}

                        <div
                            style={{ display: "flex", flexDirection: "column", gap: "16px" }}
                        >
                            {LEAVE_TYPES.map((lt) => (
                                <div
                                    key={lt.id}
                                    style={{
                                        display: "flex",
                                        alignItems: "center",
                                        gap: "12px",
                                        padding: "12px",
                                        background: enabledLeaveTypes.has(lt.id)
                                            ? "#f8fafc"
                                            : "transparent",
                                        borderRadius: "8px",
                                        border: "1px solid #e2e8f0",
                                    }}
                                >
                                    <input
                                        type="checkbox"
                                        style={{
                                            width: "18px",
                                            height: "18px",
                                            accentColor: "#7b58ca",
                                            cursor: "pointer",
                                        }}
                                        checked={enabledLeaveTypes.has(lt.id)}
                                        onChange={() => handleToggleLeaveType(lt.id)}
                                    />
                                    <label
                                        style={{
                                            flex: 1,
                                            fontWeight: "500",
                                            color: enabledLeaveTypes.has(lt.id)
                                                ? "#475569"
                                                : "#94a3b8",
                                        }}
                                    >
                                        {lt.name}
                                    </label>
                                    <input
                                        type="number"
                                        min="0"
                                        className="input"
                                        style={{
                                            width: "80px",
                                            textAlign: "center",
                                            opacity: enabledLeaveTypes.has(lt.id) ? 1 : 0.5,
                                        }}
                                        disabled={!enabledLeaveTypes.has(lt.id)}
                                        value={updateValues[lt.id] ?? ""}
                                        onChange={(e) => handleUpdateValue(lt.id, e.target.value)}
                                        placeholder="Days"
                                    />
                                </div>
                            ))}
                        </div>

                        <div
                            style={{
                                display: "flex",
                                gap: "12px",
                                justifyContent: "flex-end",
                                marginTop: "24px",
                            }}
                        >
                            <button
                                onClick={() => setShowModal(false)}
                                disabled={updating}
                                style={{
                                    padding: "10px 20px",
                                    border: "1px solid #e2e8f0",
                                    borderRadius: "8px",
                                    background: "#fff",
                                    color: "#64748b",
                                    cursor: "pointer",
                                    fontSize: "14px",
                                    fontWeight: "500",
                                }}
                            >
                                Cancel
                            </button>
                            <button
                                onClick={handleSubmitUpdate}
                                disabled={
                                    updating ||
                                    enabledLeaveTypes.size === 0 ||
                                    selectedUserIds.size > 1000
                                }
                                className="primary-btn"
                                style={{
                                    opacity:
                                        updating ||
                                            enabledLeaveTypes.size === 0 ||
                                            selectedUserIds.size > 1000
                                            ? 0.6
                                            : 1,
                                }}
                            >
                                {updating
                                    ? "Updating..."
                                    : `Apply to ${selectedUserIds.size} user${selectedUserIds.size !== 1 ? "s" : ""
                                    }`}
                            </button>
                        </div>
                    </div>
                </div>
            )}
        </>
    );
}
