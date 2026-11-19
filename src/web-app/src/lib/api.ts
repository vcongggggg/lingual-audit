/**
 * API client helper with auth header injection and Mezon SSO integration
 */

const getApiBaseUrl = (): string => {
  const envUrl = process.env.NEXT_PUBLIC_API_URL || "http://localhost:5112";
  return envUrl.endsWith("/api/v1")
    ? envUrl
    : `${envUrl.replace(/\/+$/, "")}/api/v1`;
};

export const API_URL = getApiBaseUrl();

export interface ApiResponse<T> {
  success: boolean;
  message: string;
  data: T;
  errors: string[];
}

export interface UserDto {
  id: string;
  mezonUserId: string;
  displayName: string;
  avatarUrl?: string | null;
  preferredLocale?: string;
  timezone?: string;
  status?: string;
  role?: string;
  lastSeenAt?: string | null;
  profile?: {
    level?: number;
    streakDays?: number;
    experiencePoints?: number;
    gems?: number;
  } | null;
}

export interface AuthResultDto {
  accessToken: string;
  tokenType: string;
  expiresIn: number;
  user: UserDto;
  isNewUser: boolean;
  onboardingCompleted: boolean;
}

export interface MezonAuthorizeUrlDto {
  authorizeUrl: string;
  state: string;
}

export interface LoginWithMezonRequest {
  code?: string;
  state?: string;
  token?: string;
  mezonUserId?: string;
  displayName?: string;
  avatarUrl?: string;
  redirectUri?: string;
}

export interface FetchApiOptions extends RequestInit {
  skipAuth?: boolean;
  explicitToken?: string;
}

function isApiResponse<T>(value: unknown): value is ApiResponse<T> {
  if (!value || typeof value !== "object") {
    return false;
  }
  return "success" in value && "data" in value;
}

async function parseApiJson<T>(response: Response): Promise<T> {
  if (response.status === 204) {
    return undefined as T;
  }

  const payload = await response.json();
  if (isApiResponse<T>(payload)) {
    if (!payload.success) {
      const errMsg =
        payload.errors && payload.errors.length > 0
          ? payload.errors.join(", ")
          : payload.message || "Yêu cầu xử lý thất bại.";
      throw new Error(errMsg);
    }
    return payload.data;
  }

  return payload as T;
}

export async function fetchApiResponse(
  endpoint: string,
  options: FetchApiOptions = {}
): Promise<Response> {
  const token =
    options.explicitToken ||
    (typeof window !== "undefined"
      ? localStorage.getItem("accessToken") || ""
      : "");

  const headers = new Headers(options.headers);
  const isFormData =
    typeof FormData !== "undefined" && options.body instanceof FormData;

  if (!isFormData && !headers.has("Content-Type")) {
    headers.set("Content-Type", "application/json");
  }

  if (token && !options.skipAuth && !headers.has("Authorization")) {
    headers.set("Authorization", `Bearer ${token}`);
  }

  const url = endpoint.startsWith("http") ? endpoint : `${API_URL}${endpoint}`;
  const response = await fetch(url, {
    ...options,
    headers,
  });

  if (!response.ok) {
    const errorPayload = await response.json().catch(() => ({}));
    let errorMessage = `API Error: ${response.status}`;

    if (errorPayload) {
      if (Array.isArray(errorPayload.errors) && errorPayload.errors.length > 0) {
        errorMessage = errorPayload.errors.join(", ");
      } else if (errorPayload.message) {
        errorMessage = errorPayload.message;
      } else if (errorPayload.title) {
        errorMessage = errorPayload.title;
      }
    }

    const error = new Error(errorMessage) as Error & {
      status?: number;
      data?: unknown;
    };
    error.status = response.status;
    error.data = errorPayload;
    throw error;
  }

  return response;
}

export async function fetchApi<T = unknown>(
  endpoint: string,
  options: FetchApiOptions = {}
): Promise<T> {
  const response = await fetchApiResponse(endpoint, options);
  return parseApiJson<T>(response);
}

// ---------------------------------------------------------------------------
// Mezon OAuth 2.0 & Authentication API Services
// ---------------------------------------------------------------------------

/**
 * Lấy URL ủy quyền đăng nhập Mezon OAuth 2.0 kèm CSRF state
 */
export async function getMezonAuthorizeUrl(
  redirectUri?: string
): Promise<MezonAuthorizeUrlDto> {
  const query = redirectUri
    ? `?redirectUri=${encodeURIComponent(redirectUri)}`
    : "";
  return fetchApi<MezonAuthorizeUrlDto>(`/auth/mezon/authorize${query}`, {
    skipAuth: true,
  });
}

/**
 * Gửi authorization code và state để đổi lấy JWT bearer token
 */
export async function loginWithMezon(
  request: LoginWithMezonRequest
): Promise<AuthResultDto> {
  return fetchApi<AuthResultDto>("/auth/mezon", {
    method: "POST",
    skipAuth: true,
    body: JSON.stringify(request),
  });
}

/**
 * Đăng xuất và thu hồi JWT token trên hệ thống backend
 */
export async function logoutMezon(explicitToken?: string): Promise<void> {
  await fetchApi<null>("/auth/logout", {
    method: "POST",
    explicitToken,
    body: JSON.stringify({}),
  });
}

/**
 * Kiểm tra tính hợp lệ của token và lấy thông tin phiên làm việc
 */
export async function getCurrentUser(explicitToken?: string): Promise<unknown> {
  return fetchApi<unknown>("/auth/me", {
    method: "GET",
    explicitToken,
  });
}

// ---------------------------------------------------------------------------
// Gamification & User Profile Summary API
// ---------------------------------------------------------------------------

export interface UserStreakSummaryDto {
  currentDays: number;
  bestDays: number;
  freezeBalance: number;
  isActiveToday: boolean;
  todayXp: number;
  xpNeededForStreak: number;
}

export interface BadgeSummaryDto {
  id: string;
  name: string;
  category: string;
  rarity: "common" | "rare" | "epic" | "legendary" | string;
  icon: string;
  description: string;
  unlocked: boolean;
  unlockedDate?: string | null;
  progress: number;
  maxProgress: number;
  xpReward: number;
  lore?: string | null;
}

export interface UserPerformanceStatsDto {
  wordsLearned: number;
  quizzesCompleted: number;
  accuracyRate: number;
}

export interface UserProfileSummaryDto {
  userId: string;
  displayName: string;
  avatarUrl: string | null;
  role: string;
  level: number;
  totalXp: number;
  nextLevelXp: number;
  streak: UserStreakSummaryDto;
  badges: BadgeSummaryDto[];
  stats: UserPerformanceStatsDto;
}

export const MOCK_USER_PROFILE_SUMMARY: UserProfileSummaryDto = {
  userId: "demo-user-001",
  displayName: "Mezon Learner",
  avatarUrl: null,
  role: "learner",
  level: 3,
  totalXp: 450,
  nextLevelXp: 600,
  streak: {
    currentDays: 5,
    bestDays: 12,
    freezeBalance: 2,
    isActiveToday: true,
    todayXp: 50,
    xpNeededForStreak: 30,
  },
  badges: [
    {
      id: "streak_3",
      name: "Chuỗi 3 ngày",
      category: "streak",
      rarity: "common",
      icon: "flame",
      description: "Duy trì chuỗi học tập 3 ngày liên tục",
      unlocked: true,
      unlockedDate: "2026-11-18",
      progress: 3,
      maxProgress: 3,
      xpReward: 50,
      lore: "Khởi đầu ấn tượng!",
    },
    {
      id: "streak_7",
      name: "Kiên trì 7 ngày",
      category: "streak",
      rarity: "rare",
      icon: "flame",
      description: "Học liên tục trong 7 ngày không gián đoạn",
      unlocked: false,
      unlockedDate: null,
      progress: 5,
      maxProgress: 7,
      xpReward: 100,
      lore: "Thói quen được hình thành qua từng ngày.",
    },
    {
      id: "quiz_master_10",
      name: "Chiến binh Quiz",
      category: "quiz",
      rarity: "epic",
      icon: "award",
      description: "Hoàn thành 10 bài luyện tập Quiz",
      unlocked: true,
      unlockedDate: "2026-11-15",
      progress: 12,
      maxProgress: 10,
      xpReward: 150,
      lore: "Tri thức là sức mạnh!",
    },
    {
      id: "vocab_50",
      name: "Kho từ vựng",
      category: "vocabulary",
      rarity: "common",
      icon: "book-open",
      description: "Học thuộc 50 từ vựng tiếng Anh",
      unlocked: false,
      unlockedDate: null,
      progress: 45,
      maxProgress: 50,
      xpReward: 100,
      lore: "Mỗi từ là một viên gạch xây nền móng.",
    },
    {
      id: "perfect_score",
      name: "Bách phát bách trúng",
      category: "accuracy",
      rarity: "legendary",
      icon: "sparkles",
      description: "Đạt độ chính xác trên 85% trong các bài Quiz",
      unlocked: true,
      unlockedDate: "2026-11-17",
      progress: 88,
      maxProgress: 85,
      xpReward: 250,
      lore: "Sự chuẩn xác của một bậc thầy ngôn ngữ.",
    },
  ],
  stats: {
    wordsLearned: 45,
    quizzesCompleted: 12,
    accuracyRate: 88,
  },
};

/**
 * Lấy thông tin tổng quan hồ sơ, chuỗi học tập, huy hiệu và thống kê gamification
 */
export async function fetchUserProfileSummary(
  userId?: string,
  options?: FetchApiOptions
): Promise<UserProfileSummaryDto> {
  const query = userId ? `?userId=${encodeURIComponent(userId)}` : "";
  try {
    const data = await fetchApi<UserProfileSummaryDto>(
      `/gamification/profile/summary${query}`,
      options
    );
    if (data && typeof data === "object" && typeof data.level === "number") {
      return data;
    }
    return MOCK_USER_PROFILE_SUMMARY;
  } catch (error) {
    console.warn("fetchUserProfileSummary request failed, using mock profile fallback:", error);
    return MOCK_USER_PROFILE_SUMMARY;
  }
}