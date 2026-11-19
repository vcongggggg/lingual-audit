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

export interface GameDataResponse<T = unknown> {
  success: boolean;
  data: {
    attemptId: string;
    gameType: string;
    items?: T[];
    questions?: T[];
    words?: T[];
    id?: string;
    prompt?: string;
    category?: string;
    meaningVi?: string;
    wordLength?: number;
    maxAttempts?: number;
  };
}

export interface GameSubmitRequest {
  attemptId: string;
  gameType: string;
  timeRemainingSeconds: number;
  answers: { questionId: string; answer: string }[];
}

export interface GameSubmitResult {
  success: boolean;
  score: number;
  xpEarned: number;
  accuracy: number;
  comboMultiplier: number;
  timeBonus: number;
  streakBonus: number;
  durationSeconds: number;
  correctCount: number;
  totalQuestions: number;
  highScore: number;
  isNewHighScore: boolean;
  stampsAwarded?: string[];
}

export interface GameLeaderboardEntry {
  rank: number;
  userId: string;
  displayName: string;
  avatarUrl?: string | null;
  gameType: string;
  highScore: number;
  totalWins: number;
  totalXpEarned: number;
  lastPlayedAt: string;
}

export interface MyGameStatsDto {
  success: boolean;
  userId: string;
  totalGamesPlayed: number;
  totalWins: number;
  totalXpEarned: number;
  byGame: Record<string, {
    highScore: number;
    totalGames: number;
    totalWins: number;
    totalXp: number;
    avgAccuracy: number;
    lastPlayedAt: string;
  }>;
}

export interface GameDataPayload {
  attemptId?: string;
  gameType?: string;
  items?: { id: string; targetText: string; translation: string; phonetic?: string; cefrLevel?: string }[];
  questions?: { id: string; prompt: string; tokens?: string[]; choices?: string[]; level?: string; audioText?: string }[];
  words?: { id: string; targetText: string; phonetic?: string; translation: string; cefrLevel?: string }[];
  id?: string;
  prompt?: string;
  category?: string;
  meaningVi?: string;
  wordLength?: number;
  maxAttempts?: number;
  [key: string]: unknown;
}

export async function getGameData(type: string, difficulty?: string): Promise<{ success: boolean; data: GameDataPayload }> {
  const query = difficulty ? `?type=${encodeURIComponent(type)}&difficulty=${encodeURIComponent(difficulty)}` : `?type=${encodeURIComponent(type)}`;
  const raw = await fetchApi<Record<string, unknown>>(`/games/data${query}`);
  if (raw && raw.data && typeof raw.data === "object") {
    return { success: true, data: raw.data as GameDataPayload };
  }
  return { success: true, data: (raw || {}) as GameDataPayload };
}

export async function submitGameResult(payload: GameSubmitRequest, explicitToken?: string): Promise<GameSubmitResult> {
  return fetchApi<GameSubmitResult>("/games/submit", {
    method: "POST",
    explicitToken,
    body: JSON.stringify(payload),
  });
}

export async function getGameLeaderboard(type?: string, limit: number = 20): Promise<{ success: boolean; entries: GameLeaderboardEntry[] }> {
  const query = type ? `?type=${encodeURIComponent(type)}&limit=${limit}` : `?limit=${limit}`;
  return fetchApi<{ success: boolean; entries: GameLeaderboardEntry[] }>(`/games/leaderboard${query}`);
}

export async function getMyGameStats(explicitToken?: string): Promise<MyGameStatsDto> {
  return fetchApi<MyGameStatsDto>("/games/stats/me", {
    method: "GET",
    explicitToken,
  });
}

export interface WordleGuessResponse {
  success: boolean;
  evaluations: ("correct" | "present" | "absent")[];
  remainingAttempts: number;
  isWon: boolean;
  isGameOver: boolean;
  message?: string;
}

export async function submitWordleGuess(
  attemptId: string,
  guess: string,
  explicitToken?: string
): Promise<WordleGuessResponse> {
  return fetchApi<WordleGuessResponse>("/games/wordle/guess", {
    method: "POST",
    explicitToken,
    body: JSON.stringify({ attemptId, guess }),
  });
}

export const api = {
  getGamePools: async (params?: { cefrLevel?: string; topic?: string; limit?: number; random?: boolean }) => {
    return { items: [], total: 0 };
  },
  getDictationSentences: async (params?: { cefrLevel?: string; limit?: number; random?: boolean }) => {
    return { items: [], total: 0 };
  },
  getDailyVocabQuestions: async (params?: { cefrLevel?: string; limit?: number; random?: boolean }) => {
    return { items: [], total: 0 };
  },
};


