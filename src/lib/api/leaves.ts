const API_BASE_URL =
  process.env.NEXT_PUBLIC_BACKEND_URL || "http://localhost:8000";

export interface LeaveType {
  value: number;
  name: string;
}

export interface LeaveStatus {
  value: number;
  name: string;
}

export interface SubmitLeaveRequestDto {
  type: number; // LeaveType enum value
  startDate: string; // ISO date string
  endDate: string; // ISO date string
  notes?: string;
  attachment?: File;
}

export interface LeaveRequestResponse {
  id: string;
  type: number;
  startDate: string;
  endDate: string;
  notes?: string;
  status: string;
}

export interface MyLeaveRequest {
  id: number;
  leaveType: string; // LeaveType enum as string: "Annual", "Sick", "Emergency", "Unpaid", "Maternity", "Paternity"
  startDate: string;
  endDate: string;
  numberOfDays: number;
  status: string; // LeaveStatus enum as string: "PendingManager", "PendingHR", "Approved", "Rejected", "Cancelled"
  notes?: string | null;
  attachmentUrl?: string | null;
  managerId?: number;
  createdAt: string;
}

export interface LeaveBalanceSummaryDto {
  type: string;
  totalDays: number;
  usedDays: number;
  remainingDays: number;
}

export interface PaginatedResponse<T> {
  items: T[];
  totalCount: number;
  pageNumber: number;
  pageSize: number;
  totalPages: number;
}

import { getAccessToken } from "./auth";

export async function fetchLeaveTypes(): Promise<LeaveType[]> {
  try {
    const accessToken = await getAccessToken();
    if (!accessToken) {
      throw new Error("Not authenticated");
    }

    const response = await fetch(`${API_BASE_URL}/leaves/leave-types`, {
      method: "GET",
      headers: {
        "Content-Type": "application/json",
        Authorization: `Bearer ${accessToken}`,
      },
    });

    const data = await response.json();

    if (!response.ok) {
      throw new Error(data.message || "Failed to fetch leave types");
    }

    return data;
  } catch (error) {
    throw error;
  }
}

export async function fetchLeaveStatuses(): Promise<LeaveStatus[]> {
  try {
    const accessToken = await getAccessToken();
    if (!accessToken) {
      throw new Error("Not authenticated");
    }

    const response = await fetch(`${API_BASE_URL}/leaves/leave-statuses`, {
      method: "GET",
      headers: {
        "Content-Type": "application/json",
        Authorization: `Bearer ${accessToken}`,
      },
    });

    const data = await response.json();

    if (!response.ok) {
      throw new Error(data.message || "Failed to fetch leave statuses");
    }

    return data;
  } catch (error) {
    throw error;
  }
}

export async function submitLeaveRequest(
  dto: SubmitLeaveRequestDto
): Promise<LeaveRequestResponse> {
  try {
    const accessToken = await getAccessToken();
    if (!accessToken) {
      throw new Error("Not authenticated");
    }

    // Create FormData for multipart/form-data request
    const formData = new FormData();
    formData.append("Type", dto.type.toString());
    formData.append("StartDate", dto.startDate);
    formData.append("EndDate", dto.endDate);

    if (dto.notes) {
      formData.append("Notes", dto.notes);
    }

    if (dto.attachment) {
      formData.append("Attachment", dto.attachment);
    }

    const response = await fetch(`${API_BASE_URL}/leaves`, {
      method: "POST",
      headers: {
        Authorization: `Bearer ${accessToken}`,
        // Don't set Content-Type header - browser will set it with boundary for FormData
      },
      body: formData,
    });

    const data = await response.json();

    if (!response.ok) {
      throw new Error(data.message || "Failed to submit leave request");
    }

    return data;
  } catch (error) {
    throw error;
  }
}

export async function fetchMyLeaves(
  pageNumber: number = 1,
  pageSize: number = 10
): Promise<PaginatedResponse<MyLeaveRequest>> {
  try {
    const accessToken = await getAccessToken();
    if (!accessToken) {
      throw new Error("Not authenticated");
    }

    const response = await fetch(
      `${API_BASE_URL}/leaves/my-leaves?pageNumber=${pageNumber}&pageSize=${pageSize}`,
      {
        method: "GET",
        headers: {
          "Content-Type": "application/json",
          Authorization: `Bearer ${accessToken}`,
        },
      }
    );

    const data = await response.json();

    if (!response.ok) {
      throw new Error(data.message || "Failed to fetch leave requests");
    }

    // Handle paginated response - extract items if it's an object, otherwise wrap array
    if (Array.isArray(data)) {
      return {
        items: data,
        totalCount: data.length,
        pageNumber: 1,
        pageSize: data.length,
        totalPages: 1,
      };
    } else if (data && typeof data === "object" && "items" in data) {
      return data as PaginatedResponse<MyLeaveRequest>;
    }
    return {
      items: [],
      totalCount: 0,
      pageNumber: 1,
      pageSize: pageSize,
      totalPages: 0,
    };
  } catch (error) {
    throw error;
  }
}

export async function fetchRemainingLeaves(): Promise<
  LeaveBalanceSummaryDto[]
> {
  try {
    const accessToken = await getAccessToken();
    if (!accessToken) {
      throw new Error("Not authenticated");
    }

    const response = await fetch(`${API_BASE_URL}/leaves/remaining-leaves`, {
      method: "GET",
      headers: {
        "Content-Type": "application/json",
        Authorization: `Bearer ${accessToken}`,
      },
    });

    const data = await response.json();

    if (!response.ok) {
      throw new Error(data.message || "Failed to fetch remaining leaves");
    }

    return data;
  } catch (error) {
    throw error;
  }
}

export interface PendingLeaveRequest {
  id: number;
  employeeEmail?: string;
  leaveType: string;
  startDate: string;
  endDate: string;
  numberOfDays: number;
  status: string;
  notes?: string | null;
  attachmentUrl?: string | null;
  createdAt: string;
}

export interface ManagerActionDto {
  isApproved: boolean;
  comment?: string;
}

export interface ManagerActionResponse {
  id: number;
  message?: string;
}

export async function fetchPendingApprovals(
  status: string = "PendingManager",
  pageNumber: number = 1,
  pageSize: number = 10
): Promise<PaginatedResponse<PendingLeaveRequest>> {
  try {
    const accessToken = await getAccessToken();
    if (!accessToken) {
      throw new Error("Not authenticated");
    }

    const response = await fetch(
      `${API_BASE_URL}/leaves/pending-approval?status=${encodeURIComponent(
        status
      )}&pageNumber=${pageNumber}&pageSize=${pageSize}`,
      {
        method: "GET",
        headers: {
          "Content-Type": "application/json",
          Authorization: `Bearer ${accessToken}`,
        },
      }
    );

    const data = await response.json();

    if (!response.ok) {
      if (response.status === 401) {
        throw new Error("Unauthorized. Please log in again.");
      }
      throw new Error(data.message || "Failed to fetch pending approvals");
    }

    // Handle paginated response - extract items if it's an object, otherwise wrap array
    if (Array.isArray(data)) {
      return {
        items: data,
        totalCount: data.length,
        pageNumber: 1,
        pageSize: data.length,
        totalPages: 1,
      };
    } else if (data && typeof data === "object" && "items" in data) {
      return data as PaginatedResponse<PendingLeaveRequest>;
    }
    return {
      items: [],
      totalCount: 0,
      pageNumber: 1,
      pageSize: pageSize,
      totalPages: 0,
    };
  } catch (error) {
    throw error;
  }
}

export async function submitManagerAction(
  leaveId: number,
  actionDto: ManagerActionDto
): Promise<ManagerActionResponse> {
  try {
    const accessToken = await getAccessToken();
    if (!accessToken) {
      throw new Error("Not authenticated");
    }

    const response = await fetch(
      `${API_BASE_URL}/leaves/${leaveId}/manager-action`,
      {
        method: "POST",
        headers: {
          "Content-Type": "application/json",
          Authorization: `Bearer ${accessToken}`,
        },
        body: JSON.stringify(actionDto),
      }
    );

    const data = await response.json();

    if (!response.ok) {
      if (response.status === 401) {
        throw new Error("Unauthorized. Please log in again.");
      }
      if (response.status === 404) {
        throw new Error("Leave request not found.");
      }
      if (response.status === 400) {
        throw new Error(
          data.message || "Invalid request. Please check your input."
        );
      }
      throw new Error(data.message || "Failed to submit manager action");
    }

    return data;
  } catch (error) {
    throw error;
  }
}

export async function submitHRAction(
  leaveId: number,
  actionDto: ManagerActionDto
): Promise<ManagerActionResponse> {
  try {
    const accessToken = await getAccessToken();
    if (!accessToken) {
      throw new Error("Not authenticated");
    }

    const response = await fetch(
      `${API_BASE_URL}/leaves/${leaveId}/hr-action`,
      {
        method: "POST",
        headers: {
          "Content-Type": "application/json",
          Authorization: `Bearer ${accessToken}`,
        },
        body: JSON.stringify(actionDto),
      }
    );

    const data = await response.json();

    if (!response.ok) {
      if (response.status === 401) {
        throw new Error("Unauthorized. Please log in again.");
      }
      if (response.status === 404) {
        throw new Error("Leave request not found.");
      }
      if (response.status === 400) {
        throw new Error(
          data.message || "Invalid request. Please check your input."
        );
      }
      throw new Error(data.message || "Failed to submit HR action");
    }

    return data;
  } catch (error) {
    throw error;
  }
}

export interface LeaveSettingsDto {
  leaveTypeId: number;
  name: string;
  defaultBalance: number;
  autoApproveEnabled: boolean;
  autoApproveThresholdDays: number;
}

export async function fetchLeaveSettings(): Promise<LeaveSettingsDto[]> {
  try {
    const accessToken = await getAccessToken();
    if (!accessToken) {
      throw new Error("Not authenticated");
    }

    const response = await fetch(`${API_BASE_URL}/leaves/settings`, {
      method: "GET",
      headers: {
        "Content-Type": "application/json",
        Authorization: `Bearer ${accessToken}`,
      },
    });

    const data = await response.json();

    if (!response.ok) {
      if (response.status === 401) {
        throw new Error("Unauthorized. Please log in again.");
      }
      if (response.status === 403) {
        throw new Error("Access denied. Admin permissions required.");
      }
      throw new Error(data.message || "Failed to fetch leave settings");
    }

    return data;
  } catch (error) {
    throw error;
  }
}

export async function updateLeaveSettings(
  settings: LeaveSettingsDto[]
): Promise<void> {
  try {
    const accessToken = await getAccessToken();
    if (!accessToken) {
      throw new Error("Not authenticated");
    }

    const response = await fetch(`${API_BASE_URL}/leaves/settings`, {
      method: "PUT",
      headers: {
        "Content-Type": "application/json",
        Authorization: `Bearer ${accessToken}`,
      },
      body: JSON.stringify(settings),
    });

    if (!response.ok) {
      const data = await response.json().catch(() => ({}));
      if (response.status === 401) {
        throw new Error("Unauthorized. Please log in again.");
      }
      if (response.status === 403) {
        throw new Error("Access denied. Admin permissions required.");
      }
      throw new Error(data.message || "Failed to update leave settings");
    }
  } catch (error) {
    throw error;
  }
}

export interface CancelLeaveRequestResponse {
  message: string;
}

export interface LeaveAuditLogDto {
  id: number;
  action: string;
  actionBy: string;
  newStatus: string;
  comment?: string | null;
  actionDate: string;
}

export async function fetchLeaveRequestHistory(
  leaveId: number
): Promise<LeaveAuditLogDto[]> {
  try {
    const accessToken = await getAccessToken();
    if (!accessToken) {
      throw new Error("Not authenticated");
    }

    const response = await fetch(`${API_BASE_URL}/leaves/${leaveId}/history`, {
      method: "GET",
      headers: {
        "Content-Type": "application/json",
        Authorization: `Bearer ${accessToken}`,
      },
    });

    const data = await response.json();

    if (!response.ok) {
      if (response.status === 401) {
        throw new Error("Unauthorized. Please log in again.");
      }
      if (response.status === 404) {
        throw new Error("Leave request not found.");
      }
      throw new Error(data.message || "Failed to fetch leave request history");
    }

    return data;
  } catch (error) {
    throw error;
  }
}

export async function cancelLeaveRequest(
  leaveId: number
): Promise<CancelLeaveRequestResponse> {
  try {
    const accessToken = await getAccessToken();
    if (!accessToken) {
      throw new Error("Not authenticated");
    }

    const response = await fetch(`${API_BASE_URL}/leaves/${leaveId}/cancel`, {
      method: "POST",
      headers: {
        "Content-Type": "application/json",
        Authorization: `Bearer ${accessToken}`,
      },
    });

    const data = await response.json();

    if (!response.ok) {
      if (response.status === 401) {
        throw new Error("Unauthorized. Please log in again.");
      }
      if (response.status === 404) {
        throw new Error("Leave request not found.");
      }
      if (response.status === 400) {
        throw new Error(
          data.message || "Invalid request. Cannot cancel this leave request."
        );
      }
      if (response.status === 403) {
        throw new Error(
          data.message || "Access denied. You cannot cancel this leave request."
        );
      }
      throw new Error(data.message || "Failed to cancel leave request");
    }

    return data;
  } catch (error) {
    throw error;
  }
}
