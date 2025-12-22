"use client";

import { useEffect, useState } from "react";
import { useRouter } from "next/navigation";
import Header from "@/components/common/Header"; // Ensure path is correct
import { getUserRole } from "@/lib/api/auth";     // Ensure path is correct
import { 
  fetchLeaveSettings, 
  updateLeaveSettings, 
  LeaveSettingsDto 
} from "@/lib/api/leaves";                        // Ensure path is correct

export default function AdminHomePage() {
  const [settings, setSettings] = useState<LeaveSettingsDto[]>([]);
  const [loading, setLoading] = useState(true);
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [successMsg, setSuccessMsg] = useState<string | null>(null);
  const router = useRouter();

  useEffect(() => {
    // 1. Check Auth Role
    const role = getUserRole();
    if (role !== "Admin") {
       router.push("/");
       return;
    }

    // 2. Load Data
    loadSettings();
  }, [router]);

  const loadSettings = async () => {
    try {
      setLoading(true);
      setError(null);
      console.log("Fetching settings..."); // Debug log
      
      const data = await fetchLeaveSettings();
      console.log("Settings loaded:", data); // Debug log
      
      setSettings(data);
    } catch (err) {
      console.error("Fetch error:", err);
      setError(err instanceof Error ? err.message : "An error occurred fetching settings");
    } finally {
      setLoading(false);
    }
  };

  const handleSave = async () => {
    try {
      setSaving(true);
      setSuccessMsg(null);
      setError(null);

      await updateLeaveSettings(settings);

      setSuccessMsg("Policies updated successfully.");
      setTimeout(() => setSuccessMsg(null), 3000);
      
    } catch (err) {
      setError(err instanceof Error ? err.message : "Failed to save settings");
    } finally {
      setSaving(false);
    }
  };

  const handleChange = (
    index: number,
    field: keyof LeaveSettingsDto,
    value: any
  ) => {
    const newSettings = [...settings];
    newSettings[index] = {
      ...newSettings[index],
      [field]: value,
    };
    setSettings(newSettings);
  };

  if (loading) {
    return (
      <div style={{ display: 'flex', justifyContent: 'center', alignItems: 'center', height: '100vh' }}>
        <p className="muted">Loading admin configuration...</p>
      </div>
    );
  }

  return (
    <>
      <Header
        locale="en"
        onToggleLocale={() => {}}
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
        <header className="page-header" style={{ marginBottom: '32px' }}>
          <div>
            <p className="eyebrow">Administration</p>
            <h1>Admin Dashboard</h1>
            <p className="muted">
              Configure company-wide policies, default leave balances, and auto-approval rules.
            </p>
          </div>
          <div>
            <button
              onClick={handleSave}
              disabled={saving}
              className="primary-btn"
              style={{ minWidth: "140px" }}
            >
              {saving ? "Saving..." : "Save Changes"}
            </button>
          </div>
        </header>

        {/* Error / Success Messages */}
        {error && (
          <div style={{ padding: '16px', background: '#FEF2F2', color: '#B91C1C', borderRadius: '12px', marginBottom: '24px', border: '1px solid #FCA5A5' }}>
             ⚠️ {error}
          </div>
        )}

        {successMsg && (
          <div style={{ padding: '16px', background: '#ECFDF5', color: '#047857', borderRadius: '12px', marginBottom: '24px', border: '1px solid #6EE7B7' }}>
            ✅ {successMsg}
          </div>
        )}

        {/* Main Content Card */}
        <section className="card">
          <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: '24px' }}>
             <h2 style={{ fontSize: '20px', fontWeight: 'bold', margin: 0 }}>Leave Policy Configuration</h2>
             <span className="pill" style={{ background: '#f1f5f9', color: '#64748b' }}>
                {settings.length} Leave Types Active
             </span>
          </div>
          
          <div style={{ overflowX: 'auto' }}>
            <table style={{ width: '100%', borderCollapse: 'collapse', minWidth: '800px' }}>
              <thead>
                <tr style={{ borderBottom: '2px solid #e2e8f0', textAlign: 'left' }}>
                  <th style={{ padding: '16px', color: '#64748b' }}>Leave Type</th>
                  <th style={{ padding: '16px', color: '#64748b' }}>
                    Default Balance
                    <span style={{ display: 'block', fontSize: '12px', fontWeight: 'normal', marginTop: '4px' }}>Per year (new employees)</span>
                  </th>
                  <th style={{ padding: '16px', color: '#64748b' }}>
                    Auto-Approval
                    <span style={{ display: 'block', fontSize: '12px', fontWeight: 'normal', marginTop: '4px' }}>Enable system approval</span>
                  </th>
                  <th style={{ padding: '16px', color: '#64748b' }}>
                    Threshold
                    <span style={{ display: 'block', fontSize: '12px', fontWeight: 'normal', marginTop: '4px' }}>Max days for auto-approval</span>
                  </th>
                </tr>
              </thead>
              <tbody>
                {settings.length === 0 && (
                  <tr>
                    <td colSpan={4} style={{ textAlign: 'center', padding: '48px', color: '#94a3b8' }}>
                      No leave types found. Please check your database or backend connection.
                    </td>
                  </tr>
                )}
                {settings.map((setting, index) => (
                  <tr
                    key={setting.leaveTypeId}
                    style={{ borderBottom: '1px solid #f1f5f9' }}
                  >
                    <td style={{ padding: '16px', fontWeight: 'bold', color: '#475569' }}>
                        {setting.name}
                    </td>
                    <td style={{ padding: '16px' }}>
                      <div style={{ display: 'flex', alignItems: 'center', gap: '8px' }}>
                        <input
                          type="number"
                          min="0"
                          className="input"
                          style={{ width: '80px', textAlign: 'center' }}
                          value={setting.defaultBalance}
                          onChange={(e) =>
                            handleChange(
                              index,
                              "defaultBalance",
                              parseInt(e.target.value) || 0
                            )
                          }
                        />
                        <span style={{ fontSize: '14px', color: '#94a3b8' }}>days</span>
                      </div>
                    </td>
                    <td style={{ padding: '16px' }}>
                      <label style={{ display: 'flex', alignItems: 'center', gap: '12px', cursor: 'pointer' }}>
                        <input
                          type="checkbox"
                          style={{ width: '20px', height: '20px', accentColor: '#7b58ca' }}
                          checked={setting.autoApproveEnabled}
                          onChange={(e) =>
                            handleChange(
                              index,
                              "autoApproveEnabled",
                              e.target.checked
                            )
                          }
                        />
                        <span style={{ fontSize: '14px', fontWeight: '500', color: setting.autoApproveEnabled ? '#7b58ca' : '#94a3b8' }}>
                          {setting.autoApproveEnabled ? "Enabled" : "Disabled"}
                        </span>
                      </label>
                    </td>
                    <td style={{ padding: '16px' }}>
                      <div style={{ display: 'flex', alignItems: 'center', gap: '8px', opacity: setting.autoApproveEnabled ? 1 : 0.5 }}>
                        <span style={{ fontSize: '14px', color: '#94a3b8' }}>&le;</span>
                        <input
                          type="number"
                          min="0"
                          disabled={!setting.autoApproveEnabled}
                          className="input"
                          style={{ width: '80px', textAlign: 'center' }}
                          value={setting.autoApproveThresholdDays}
                          onChange={(e) =>
                            handleChange(
                              index,
                              "autoApproveThresholdDays",
                              parseInt(e.target.value) || 0
                            )
                          }
                        />
                        <span style={{ fontSize: '14px', color: '#94a3b8' }}>days</span>
                      </div>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
          
          <div style={{ marginTop: '24px', padding: '16px', background: '#eff6ff', borderRadius: '12px', color: '#1e40af', fontSize: '14px' }}>
            <p style={{ margin: 0 }}>
              <strong>Policy Impact Note:</strong> <br/>
              &bull; <strong>Default Balance:</strong> Changes affect employees created <em>after</em> this save. <br/>
              &bull; <strong>Auto-Approval:</strong> Rules take effect immediately. Requests within the threshold skip Manager approval.
            </p>
          </div>
        </section>
      </main>
    </>
  );
}