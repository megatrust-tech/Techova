"use client";

import { useEffect, useState } from "react";
import { useRouter } from "next/navigation";
import Header from "@/components/common/Header";
import { getUserRole } from "@/lib/api/auth";
import {
    fetchUsersWithoutBalances,
    initializeBalances,
    UserWithoutBalanceDto,
    PaginatedUsersWithoutBalanceDto,
} from "@/lib/api/leaves";

const PAGE_SIZE = 20;

export default function AdminBalancesPage() {
    const [paginatedData, setPaginatedData] =
        useState<PaginatedUsersWithoutBalanceDto | null>(null);
    const [selectedUserIds, setSelectedUserIds] = useState<Set<string>>(
        new Set()
    );
    const [currentPage, setCurrentPage] = useState(1);
    const [loading, setLoading] = useState(true);
    const [initializing, setInitializing] = useState(false);
    const [error, setError] = useState<string | null>(null);
    const [successMsg, setSuccessMsg] = useState<string | null>(null);
    const router = useRouter();

    useEffect(() => {
        const role = getUserRole();
        if (role !== "Admin") {
            router.push("/");
            return;
        }
        loadUsers(1);
    }, [router]);

    const loadUsers = async (page: number) => {
        try {
            setLoading(true);
            setError(null);
            const data = await fetchUsersWithoutBalances(page, PAGE_SIZE);
            setPaginatedData(data);
            setCurrentPage(page);
            // Clear selections when changing pages
            setSelectedUserIds(new Set());
        } catch (err) {
            console.error("Fetch error:", err);
            setError(
                err instanceof Error
                    ? err.message
                    : "An error occurred fetching users"
            );
        } finally {
            setLoading(false);
        }
    };

    const users: UserWithoutBalanceDto[] = paginatedData?.items ?? [];

    const handleSelectAll = () => {
        if (selectedUserIds.size === users.length) {
            setSelectedUserIds(new Set());
        } else {
            setSelectedUserIds(new Set(users.map((u) => u.userId)));
        }
    };

    const handleSelectUser = (userId: string) => {
        const newSet = new Set(selectedUserIds);
        if (newSet.has(userId)) {
            newSet.delete(userId);
        } else {
            newSet.add(userId);
        }
        setSelectedUserIds(newSet);
    };

    const handleInitializeBalances = async () => {
        if (selectedUserIds.size === 0) return;

        try {
            setInitializing(true);
            setError(null);
            setSuccessMsg(null);

            const result = await initializeBalances({
                userIds: Array.from(selectedUserIds),
            });

            setSuccessMsg(
                result.message ||
                `Successfully initialized balances for ${result.initializedCount} user(s).`
            );
            setTimeout(() => setSuccessMsg(null), 5000);

            // Reload current page
            await loadUsers(currentPage);
        } catch (err) {
            setError(
                err instanceof Error ? err.message : "Failed to initialize balances"
            );
        } finally {
            setInitializing(false);
        }
    };

    const handlePageChange = (newPage: number) => {
        if (newPage < 1 || (paginatedData && newPage > paginatedData.totalPages)) {
            return;
        }
        loadUsers(newPage);
    };

    const formatDate = (dateString: string) => {
        return new Date(dateString).toLocaleDateString("en-US", {
            year: "numeric",
            month: "short",
            day: "numeric",
        });
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
                        <h1>Manage User Balances</h1>
                        <p className="muted">
                            Initialize leave balances for users who don&apos;t have them set
                            up yet.
                        </p>
                    </div>
                    <div>
                        <button
                            onClick={handleInitializeBalances}
                            disabled={initializing || selectedUserIds.size === 0}
                            className="primary-btn"
                            style={{
                                minWidth: "180px",
                                opacity: initializing || selectedUserIds.size === 0 ? 0.6 : 1,
                            }}
                        >
                            {initializing
                                ? "Initializing..."
                                : `Initialize (${selectedUserIds.size})`}
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
                        }}
                    >
                        <h2 style={{ fontSize: "20px", fontWeight: "bold", margin: 0 }}>
                            Users Without Leave Balances
                        </h2>
                        <span
                            className="pill"
                            style={{ background: "#f1f5f9", color: "#64748b" }}
                        >
                            {paginatedData?.totalCount ?? 0} Total User
                            {paginatedData?.totalCount !== 1 ? "s" : ""}
                        </span>
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
                                minWidth: "600px",
                            }}
                        >
                            <thead>
                                <tr
                                    style={{
                                        borderBottom: "2px solid #e2e8f0",
                                        textAlign: "left",
                                    }}
                                >
                                    <th style={{ padding: "16px", width: "48px" }}>
                                        <input
                                            type="checkbox"
                                            style={{
                                                width: "20px",
                                                height: "20px",
                                                accentColor: "#7b58ca",
                                                cursor: "pointer",
                                            }}
                                            checked={
                                                users.length > 0 && selectedUserIds.size === users.length
                                            }
                                            onChange={handleSelectAll}
                                            disabled={users.length === 0}
                                        />
                                    </th>
                                    <th style={{ padding: "16px", color: "#64748b" }}>
                                        Full Name
                                    </th>
                                    <th style={{ padding: "16px", color: "#64748b" }}>Email</th>
                                    <th style={{ padding: "16px", color: "#64748b" }}>
                                        Created At
                                    </th>
                                </tr>
                            </thead>
                            <tbody>
                                {users.length === 0 && !loading && (
                                    <tr>
                                        <td
                                            colSpan={4}
                                            style={{
                                                textAlign: "center",
                                                padding: "48px",
                                                color: "#94a3b8",
                                            }}
                                        >
                                            🎉 All users have their leave balances initialized!
                                        </td>
                                    </tr>
                                )}
                                {users.map((user) => (
                                    <tr
                                        key={user.userId}
                                        style={{
                                            borderBottom: "1px solid #f1f5f9",
                                            background: selectedUserIds.has(user.userId)
                                                ? "#f8fafc"
                                                : "transparent",
                                        }}
                                    >
                                        <td style={{ padding: "16px" }}>
                                            <input
                                                type="checkbox"
                                                style={{
                                                    width: "20px",
                                                    height: "20px",
                                                    accentColor: "#7b58ca",
                                                    cursor: "pointer",
                                                }}
                                                checked={selectedUserIds.has(user.userId)}
                                                onChange={() => handleSelectUser(user.userId)}
                                            />
                                        </td>
                                        <td
                                            style={{
                                                padding: "16px",
                                                fontWeight: "500",
                                                color: "#475569",
                                            }}
                                        >
                                            {user.fullName}
                                        </td>
                                        <td style={{ padding: "16px", color: "#64748b" }}>
                                            {user.email}
                                        </td>
                                        <td style={{ padding: "16px", color: "#94a3b8" }}>
                                            {formatDate(user.createdAt)}
                                        </td>
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
                            }}
                        >
                            <p style={{ margin: 0, color: "#64748b", fontSize: "14px" }}>
                                Showing {(currentPage - 1) * PAGE_SIZE + 1} -{" "}
                                {Math.min(currentPage * PAGE_SIZE, paginatedData.totalCount)} of{" "}
                                {paginatedData.totalCount}
                            </p>
                            <div style={{ display: "flex", gap: "8px" }}>
                                <button
                                    onClick={() => handlePageChange(currentPage - 1)}
                                    disabled={!paginatedData.hasPreviousPage || loading}
                                    style={{
                                        padding: "8px 16px",
                                        border: "1px solid #e2e8f0",
                                        borderRadius: "8px",
                                        background: paginatedData.hasPreviousPage ? "#fff" : "#f8fafc",
                                        color: paginatedData.hasPreviousPage ? "#475569" : "#94a3b8",
                                        cursor: paginatedData.hasPreviousPage ? "pointer" : "not-allowed",
                                        fontSize: "14px",
                                        fontWeight: "500",
                                    }}
                                >
                                    ← Previous
                                </button>
                                <span
                                    style={{
                                        padding: "8px 16px",
                                        color: "#64748b",
                                        fontSize: "14px",
                                        display: "flex",
                                        alignItems: "center",
                                    }}
                                >
                                    Page {currentPage} of {paginatedData.totalPages}
                                </span>
                                <button
                                    onClick={() => handlePageChange(currentPage + 1)}
                                    disabled={!paginatedData.hasNextPage || loading}
                                    style={{
                                        padding: "8px 16px",
                                        border: "1px solid #e2e8f0",
                                        borderRadius: "8px",
                                        background: paginatedData.hasNextPage ? "#fff" : "#f8fafc",
                                        color: paginatedData.hasNextPage ? "#475569" : "#94a3b8",
                                        cursor: paginatedData.hasNextPage ? "pointer" : "not-allowed",
                                        fontSize: "14px",
                                        fontWeight: "500",
                                    }}
                                >
                                    Next →
                                </button>
                            </div>
                        </div>
                    )}

                    <div
                        style={{
                            marginTop: "24px",
                            padding: "16px",
                            background: "#eff6ff",
                            borderRadius: "12px",
                            color: "#1e40af",
                            fontSize: "14px",
                        }}
                    >
                        <p style={{ margin: 0 }}>
                            <strong>Note:</strong> Initializing balances will create default
                            leave allocations for the selected users based on the current
                            policy settings. Selection is per-page only.
                        </p>
                    </div>
                </section>
            </main>
        </>
    );
}
