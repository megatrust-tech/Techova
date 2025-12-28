const API_BASE_URL =
  process.env.NEXT_PUBLIC_BACKEND_URL || "http://localhost:8000";

export interface LoginCredentials {
  email: string;
  password: string;
}

export interface RoleDto {
  roleId: number;
  roleName: string;
}

export interface CreateUserData {
  firstName: string;
  lastName: string;
  email: string;
  password: string;
  roleId: number;
}

export interface CreateUserResponse {
  id?: string;
  email?: string;
  fullName?: string;
  message?: string;
}

export interface LoginResponse {
  accessToken: string;
  refreshToken: string;
  expiresAt: string;
}

export interface RefreshTokenResponse {
  accessToken: string;
  refreshToken: string;
  expiresAt: string;
}

// Storage keys
const ACCESS_TOKEN_KEY = "authAccessToken";
const REFRESH_TOKEN_KEY = "authRefreshToken";
const EXPIRES_AT_KEY = "authExpiresAt";
const AUTHENTICATED_FLAG = "isAuthenticated";

// Check if access token is expired or will expire soon (within 1 minute)
function isTokenExpired(): boolean {
  if (typeof window === "undefined") return true;

  const expiresAt = localStorage.getItem(EXPIRES_AT_KEY);
  if (!expiresAt) return true;

  const expiryTime = new Date(expiresAt).getTime();
  const now = Date.now();
  const bufferTime = 60 * 1000; // 1 minute buffer

  return now >= expiryTime - bufferTime;
}

// Store tokens in localStorage
function storeTokens(
  accessToken: string,
  refreshToken: string,
  expiresAt: string
) {
  if (typeof window === "undefined") return;
  localStorage.setItem(ACCESS_TOKEN_KEY, accessToken);
  localStorage.setItem(REFRESH_TOKEN_KEY, refreshToken);
  localStorage.setItem(EXPIRES_AT_KEY, expiresAt);
  localStorage.setItem(AUTHENTICATED_FLAG, "true");
}

// Clear all auth data
function clearAuthData() {
  if (typeof window === "undefined") return;
  localStorage.removeItem(ACCESS_TOKEN_KEY);
  localStorage.removeItem(REFRESH_TOKEN_KEY);
  localStorage.removeItem(EXPIRES_AT_KEY);
  localStorage.removeItem(AUTHENTICATED_FLAG);
}

export async function fetchRoles(): Promise<RoleDto[]> {
  try {
    const accessToken = await getAccessToken();

    if (!accessToken) {
      throw new Error("Not authenticated");
    }

    const response = await fetch(`${API_BASE_URL}/v1/roles`, {
      method: "GET",
      headers: {
        "Content-Type": "application/json",
        Authorization: `Bearer ${accessToken}`,
      },
    });

    const data = await response.json();

    if (!response.ok) {
      throw new Error(data.message || "Failed to fetch roles");
    }

    return data;
  } catch (error) {
    throw error;
  }
}

export async function createUser(
  userData: CreateUserData
): Promise<CreateUserResponse> {
  try {
    const accessToken = await getAccessToken();

    if (!accessToken) {
      throw new Error("Not authenticated");
    }

    const response = await fetch(`${API_BASE_URL}/v1/users`, {
      method: "POST",
      headers: {
        "Content-Type": "application/json",
        Authorization: `Bearer ${accessToken}`,
      },
      body: JSON.stringify(userData),
    });

    const data = await response.json();

    if (!response.ok) {
      throw new Error(data.message || "User creation failed");
    }

    return data;
  } catch (error) {
    throw error;
  }
}

export async function login(
  credentials: LoginCredentials
): Promise<LoginResponse> {
  try {
    const response = await fetch(`${API_BASE_URL}/v1/auth/login`, {
      method: "POST",
      headers: {
        "Content-Type": "application/json",
      },
      body: JSON.stringify(credentials),
    });

    const data = await response.json();

    if (!response.ok) {
      throw new Error(data.message || "Login failed");
    }

    // Store tokens
    if (data.accessToken && data.refreshToken && data.expiresAt) {
      storeTokens(data.accessToken, data.refreshToken, data.expiresAt);
    }

    return data;
  } catch (error) {
    throw error;
  }
}

export async function refreshAccessToken(): Promise<string> {
  const refreshToken = localStorage.getItem(REFRESH_TOKEN_KEY);

  if (!refreshToken) {
    throw new Error("No refresh token available");
  }

  try {
    const response = await fetch(`${API_BASE_URL}/v1/auth/refresh`, {
      method: "POST",
      headers: {
        "Content-Type": "application/json",
      },
      body: JSON.stringify({ RefreshToken: refreshToken }),
    });

    const data = await response.json();

    if (!response.ok) {
      clearAuthData();
      throw new Error(data.message || "Token refresh failed");
    }

    // Store new tokens
    if (data.accessToken && data.refreshToken && data.expiresAt) {
      storeTokens(data.accessToken, data.refreshToken, data.expiresAt);
    }

    return data.accessToken;
  } catch (error) {
    throw error;
  }
}

// Get access token, automatically refresh if expired
export async function getAccessToken(): Promise<string | null> {
  if (typeof window === "undefined") return null;

  const accessToken = localStorage.getItem(ACCESS_TOKEN_KEY);

  if (!accessToken) {
    return null;
  }

  // Check if token is expired and refresh if needed
  if (isTokenExpired()) {
    try {
      const newToken = await refreshAccessToken();
      return newToken;
    } catch (error) {
      console.error("Failed to refresh token:", error);
      return null;
    }
  }

  return accessToken;
}

// Synchronous version for use in headers (may return expired token)
// Use getAccessToken() for automatic refresh
export function getAuthToken(): string | null {
  if (typeof window === "undefined") return null;
  return localStorage.getItem(ACCESS_TOKEN_KEY);
}

export async function logout(): Promise<void> {
  try {
    const accessToken = await getAccessToken();

    // Call logout API endpoint if we have a token
    if (accessToken) {
      await fetch(`${API_BASE_URL}/v1/auth/logout`, {
        method: "POST",
        headers: {
          "Content-Type": "application/json",
          Authorization: `Bearer ${accessToken}`,
        },
      });
    }
  } catch (error) {
    // Even if API call fails, clear local data
    console.error("Logout API call failed:", error);
  } finally {
    // Always clear local auth data
    clearAuthData();
  }
}

export function isAuthenticated(): boolean {
  if (typeof window === "undefined") return false;
  const accessToken = localStorage.getItem(ACCESS_TOKEN_KEY);
  const refreshToken = localStorage.getItem(REFRESH_TOKEN_KEY);
  // User is authenticated if they have tokens (even if access token is expired, refresh token can renew it)
  return (
    !!(accessToken || refreshToken) &&
    localStorage.getItem(AUTHENTICATED_FLAG) === "true"
  );
}

export function setAuthenticated(value: boolean) {
  if (!value) {
    clearAuthData();
  }
}

// Decode JWT token to get user role
export function getUserRole(): string | null {
  if (typeof window === "undefined") return null;

  const accessToken = localStorage.getItem(ACCESS_TOKEN_KEY);
  if (!accessToken) return null;

  try {
    // JWT tokens have 3 parts: header.payload.signature
    const parts = accessToken.split(".");
    if (parts.length !== 3) return null;

    // Decode the payload (second part)
    const payload = parts[1];
    // Replace URL-safe base64 characters and add padding if needed
    const base64 = payload.replace(/-/g, "+").replace(/_/g, "/");
    const padded = base64 + "=".repeat((4 - (base64.length % 4)) % 4);
    const decoded = atob(padded);
    const claims = JSON.parse(decoded);

    // Extract role from the token
    // The role claim is at: http://schemas.microsoft.com/ws/2008/06/identity/claims/role
    const role =
      claims["http://schemas.microsoft.com/ws/2008/06/identity/claims/role"] ||
      claims.role ||
      null;

    return role;
  } catch (error) {
    console.error("Failed to decode token:", error);
    return null;
  }
}

// Check if user has one of the allowed roles
export function hasRole(allowedRoles: string[]): boolean {
  const userRole = getUserRole();
  if (!userRole) return false;
  return allowedRoles.includes(userRole);
}

// Helper function for making authenticated API requests
// Automatically includes access token and refreshes if expired
export async function authenticatedFetch(
  url: string,
  options: RequestInit = {}
): Promise<Response> {
  const accessToken = await getAccessToken();

  if (!accessToken) {
    throw new Error("Not authenticated");
  }

  const headers = new Headers(options.headers);
  headers.set("Authorization", `Bearer ${accessToken}`);

  const response = await fetch(url, {
    ...options,
    headers,
  });

  // If token expired during request, try refreshing and retry once
  if (response.status === 401) {
    try {
      const newToken = await refreshAccessToken();
      headers.set("Authorization", `Bearer ${newToken}`);

      return fetch(url, {
        ...options,
        headers,
      });
    } catch (error) {
      clearAuthData();
      throw error;
    }
  }

  return response;
}

// User Search Result interface
export interface UserSearchResult {
  id: number;
  firstName: string;
  lastName: string;
  email: string;
  roleName: string;
  departmentId: number;
}

// Search users by name or email (Admin only)
export async function searchUsers(query: string): Promise<UserSearchResult[]> {
  if (query.length < 2) {
    return [];
  }

  const accessToken = await getAccessToken();
  if (!accessToken) {
    throw new Error("Not authenticated");
  }

  const response = await fetch(
    `${API_BASE_URL}/v1/users/search?query=${encodeURIComponent(query)}`,
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
    if (response.status === 401 || response.status === 403) {
      throw new Error("Unauthorized");
    }
    throw new Error(data.message || "Failed to search users");
  }

  return data;
}

