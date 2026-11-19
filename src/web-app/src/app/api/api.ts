import { NextRequest, NextResponse } from "next/server";

// ============================================================================
// 1. CONFIGURATION & TYPES
// ============================================================================

export function getBackendBaseUrl(): string {
  const envUrl =
    process.env.LINGUAL_API_URL ||
    process.env.NEXT_PUBLIC_API_URL ||
    "http://localhost:5112";
  return envUrl.replace(/\/+$/, "");
}

export interface ApiRequestConfig extends RequestInit {
  endpoint?: string;
  baseUrl?: string;
  params?: Record<string, string | number | boolean | undefined | null>;
  token?: string;
  skipAuth?: boolean;
  incomingRequest?: NextRequest | Request;
  timeoutMs?: number;
}

export interface ApiResponse<T = unknown> {
  success?: boolean;
  message?: string;
  data?: T;
  errors?: string[];
  [key: string]: unknown;
}

export class ApiError extends Error {
  status: number;
  data: unknown;

  constructor(message: string, status: number = 500, data?: unknown) {
    super(message);
    this.name = "ApiError";
    this.status = status;
    this.data = data;
  }
}

export type RequestInterceptor = (
  config: ApiRequestConfig
) => ApiRequestConfig | Promise<ApiRequestConfig>;

export type ResponseSuccessInterceptor = (
  response: Response,
  config: ApiRequestConfig
) => Response | Promise<Response>;

export type ResponseErrorInterceptor = (
  error: unknown,
  config: ApiRequestConfig
) => unknown | Promise<unknown>;

export interface InterceptorManager<T, E = unknown> {
  use: (onFulfilled: T, onRejected?: E) => number;
  eject: (id: number) => void;
}

// ============================================================================
// 2. TOKEN RESOLVER HELPER
// ============================================================================

/**
 * Lấy token từ nhiều nguồn theo thứ tự ưu tiên:
 * 1. Explicit token truyền vào config
 * 2. NextRequest/Request (Header Authorization hoặc Cookie accessToken)
 * 3. Client-side browser (localStorage hoặc Document Cookie)
 */
export function resolveToken(config: ApiRequestConfig): string | null {
  if (config.skipAuth) return null;

  // 1. Explicit token
  if (config.token) return config.token;

  // 2. Incoming NextRequest / Request (Server-side Route Handler)
  if (config.incomingRequest) {
    const authHeader =
      config.incomingRequest.headers.get("authorization") ||
      config.incomingRequest.headers.get("Authorization");

    if (authHeader) {
      const match = authHeader.match(/^Bearer\s+(.+)$/i);
      return match ? match[1].trim() : authHeader.trim();
    }

    if ("cookies" in config.incomingRequest) {
      const cookieObj = (config.incomingRequest as NextRequest).cookies;
      const cookieToken =
        cookieObj.get("accessToken")?.value ||
        cookieObj.get("token")?.value;
      if (cookieToken) return cookieToken;
    }
  }

  // 3. Client-side browser environment
  if (typeof window !== "undefined") {
    try {
      const storedToken =
        localStorage.getItem("accessToken") ||
        localStorage.getItem("token") ||
        sessionStorage.getItem("accessToken");
      if (storedToken) return storedToken;

      const cookieMatch = document.cookie.match(
        /(?:^|;\s*)(?:accessToken|token)=([^;]+)/
      );
      if (cookieMatch) return decodeURIComponent(cookieMatch[1]);
    } catch {
      // Ignore security errors in restricted browser contexts
    }
  }

  return null;
}

// ============================================================================
// 3. CORE API CLIENT WITH INTERCEPTOR SYSTEM
// ============================================================================

export class ApiClient {
  private defaultBaseUrl: string;
  private requestInterceptors: Array<{
    id: number;
    handler: RequestInterceptor;
  }> = [];
  private responseInterceptors: Array<{
    id: number;
    onSuccess?: ResponseSuccessInterceptor;
    onError?: ResponseErrorInterceptor;
  }> = [];
  private nextId = 0;

  constructor(baseUrl?: string) {
    this.defaultBaseUrl = baseUrl !== undefined ? baseUrl : getBackendBaseUrl();
    this.registerDefaultInterceptors();
  }

  public get interceptors() {
    return {
      request: {
        use: (handler: RequestInterceptor): number => {
          const id = ++this.nextId;
          this.requestInterceptors.push({ id, handler });
          return id;
        },
        eject: (id: number): void => {
          this.requestInterceptors = this.requestInterceptors.filter(
            (i) => i.id !== id
          );
        },
      },
      response: {
        use: (
          onSuccess?: ResponseSuccessInterceptor,
          onError?: ResponseErrorInterceptor
        ): number => {
          const id = ++this.nextId;
          this.responseInterceptors.push({ id, onSuccess, onError });
          return id;
        },
        eject: (id: number): void => {
          this.responseInterceptors = this.responseInterceptors.filter(
            (i) => i.id !== id
          );
        },
      },
    };
  }

  /**
   * Đăng ký các interceptor mặc định:
   * - Request: Tự động inject Bearer token, Content-Type, Query params
   * - Response: Bắt lỗi 401 Unauthorized, parse message chuẩn, log warn
   */
  private registerDefaultInterceptors(): void {
    // Default Request Interceptor
    this.interceptors.request.use(async (config) => {
      const headers = new Headers(config.headers || {});

      // 1. Gắn Token nếu có
      const token = resolveToken(config);
      if (token && !headers.has("Authorization")) {
        headers.set("Authorization", `Bearer ${token}`);
      }

      // 2. Đặt Content-Type JSON nếu không phải FormData
      const isFormData =
        typeof FormData !== "undefined" && config.body instanceof FormData;
      if (!isFormData && !headers.has("Content-Type")) {
        headers.set("Content-Type", "application/json");
      }

      config.headers = headers;
      return config;
    });

    // Default Response Interceptor
    this.interceptors.response.use(
      async (response, config) => {
        // Xử lý 401 Unauthorized
        if (response.status === 401) {
          console.warn(
            `⚠️ [ApiClient 401 Unauthorized] Endpoint: ${config.endpoint || response.url}`
          );

          if (typeof window !== "undefined") {
            window.dispatchEvent(
              new CustomEvent("lingual:auth-unauthorized", {
                detail: { url: response.url, status: 401 },
              })
            );
          }
        }
        return response;
      },
      async (error, config) => {
        console.error(
          `❌ [ApiClient Error] Endpoint: ${config.endpoint || "unknown"}:`,
          error
        );
        throw error;
      }
    );
  }

  /**
   * Thực thi toàn bộ chuỗi request & response interceptors
   */
  public async requestRaw(
    endpoint: string,
    options: ApiRequestConfig = {}
  ): Promise<Response> {
    let config: ApiRequestConfig = {
      ...options,
      endpoint,
      baseUrl: options.baseUrl !== undefined ? options.baseUrl : this.defaultBaseUrl,
    };

    // 1. Chạy qua chuỗi Request Interceptors
    for (const { handler } of this.requestInterceptors) {
      config = await handler(config);
    }

    // 2. Xây dựng URL hoàn chỉnh
    let fullUrl: string;
    if (config.endpoint!.startsWith("http")) {
      fullUrl = config.endpoint!;
    } else if (config.baseUrl) {
      fullUrl = `${config.baseUrl.replace(/\/+$/, "")}/${config.endpoint!.replace(/^\/+/, "")}`;
    } else {
      fullUrl = config.endpoint!.startsWith("/") ? config.endpoint! : `/${config.endpoint}`;
    }

    if (config.params && Object.keys(config.params).length > 0) {
      const baseOrigin =
        typeof window !== "undefined"
          ? window.location.origin
          : "http://localhost:3000";
      const urlObj = new URL(fullUrl, baseOrigin);
      for (const [key, val] of Object.entries(config.params)) {
        if (val !== undefined && val !== null) {
          urlObj.searchParams.set(key, String(val));
        }
      }
      fullUrl =
        config.baseUrl || config.endpoint!.startsWith("http")
          ? urlObj.toString()
          : `${urlObj.pathname}${urlObj.search}`;
    }

    // 3. Quản lý Timeout (mặc định 15s nếu không cấu hình)
    const timeoutMs = config.timeoutMs ?? 15000;
    const controller = new AbortController();
    const timeoutId = setTimeout(() => controller.abort(), timeoutMs);

    const originalSignal = config.signal;
    if (originalSignal) {
      originalSignal.addEventListener("abort", () => controller.abort());
    }

    try {
      let response = await fetch(fullUrl, {
        ...config,
        signal: controller.signal,
      });

      clearTimeout(timeoutId);

      // 4. Chạy qua chuỗi Response Success Interceptors
      for (const { onSuccess } of this.responseInterceptors) {
        if (onSuccess) {
          response = await onSuccess(response, config);
        }
      }

      return response;
    } catch (err: unknown) {
      clearTimeout(timeoutId);

      // 5. Chạy qua chuỗi Response Error Interceptors
      let currentError: unknown = err;
      for (const { onError } of this.responseInterceptors) {
        if (onError) {
          try {
            currentError = await onError(currentError, config);
          } catch (interceptorErr) {
            currentError = interceptorErr;
          }
        }
      }

      throw currentError;
    }
  }

  /**
   * Gọi API và tự động parse JSON / ném lỗi dạng ApiError
   */
  public async request<T = unknown>(
    endpoint: string,
    options: ApiRequestConfig = {}
  ): Promise<T> {
    const response = await this.requestRaw(endpoint, options);

    if (response.status === 204) {
      return undefined as T;
    }

    let payload: unknown;
    try {
      payload = await response.json();
    } catch {
      payload = await response.text();
    }

    if (!response.ok) {
      let errorMessage = `API Error ${response.status}: ${response.statusText}`;

      if (payload && typeof payload === "object") {
        const obj = payload as Record<string, unknown>;
        if (Array.isArray(obj.errors) && obj.errors.length > 0) {
          errorMessage = obj.errors.join(", ");
        } else if (typeof obj.message === "string") {
          errorMessage = obj.message;
        } else if (typeof obj.error === "string") {
          errorMessage = obj.error;
        } else if (typeof obj.title === "string") {
          errorMessage = obj.title;
        }
      }

      throw new ApiError(errorMessage, response.status, payload);
    }

    return payload as T;
  }

  public get<T = unknown>(endpoint: string, options?: ApiRequestConfig): Promise<T> {
    return this.request<T>(endpoint, { ...options, method: "GET" });
  }

  public post<T = unknown>(
    endpoint: string,
    body?: unknown,
    options?: ApiRequestConfig
  ): Promise<T> {
    const isFormData = typeof FormData !== "undefined" && body instanceof FormData;
    return this.request<T>(endpoint, {
      ...options,
      method: "POST",
      body: isFormData ? (body as FormData) : JSON.stringify(body ?? {}),
    });
  }

  public put<T = unknown>(
    endpoint: string,
    body?: unknown,
    options?: ApiRequestConfig
  ): Promise<T> {
    const isFormData = typeof FormData !== "undefined" && body instanceof FormData;
    return this.request<T>(endpoint, {
      ...options,
      method: "PUT",
      body: isFormData ? (body as FormData) : JSON.stringify(body ?? {}),
    });
  }

  public delete<T = unknown>(endpoint: string, options?: ApiRequestConfig): Promise<T> {
    return this.request<T>(endpoint, { ...options, method: "DELETE" });
  }

  public patch<T = unknown>(
    endpoint: string,
    body?: unknown,
    options?: ApiRequestConfig
  ): Promise<T> {
    const isFormData = typeof FormData !== "undefined" && body instanceof FormData;
    return this.request<T>(endpoint, {
      ...options,
      method: "PATCH",
      body: isFormData ? (body as FormData) : JSON.stringify(body ?? {}),
    });
  }
}

// ============================================================================
// 4. SINGLETON CLIENTS
// ============================================================================

/**
 * Instance mặc định kết nối tới Backend ASP.NET Core
 */
export const backendApi = new ApiClient(getBackendBaseUrl());

/**
 * Instance tiện ích cho Client components (gọi route Next.js `/api/...`)
 */
export const clientApi = new ApiClient("");

// ============================================================================
// 5. HELPER CHUYÊN DỤNG CHO NEXT.JS SERVER ROUTE HANDLERS (route.ts)
// ============================================================================

export interface ProxyBackendOptions {
  method?: string;
  forwardBody?: boolean;
  customBody?: unknown;
  params?: Record<string, string | number | boolean | undefined | null>;
  timeoutMs?: number;
  skipAuth?: boolean;
}

/**
 * Proxy an toàn từ Next.js Route Handler sang Backend ASP.NET Core:
 * - Tự động trích xuất và chuyển tiếp `Authorization: Bearer <token>`
 * - Chuyển tiếp query parameters và body
 * - Xử lý bắt lỗi 503 khi Backend offline hoặc trả mã HTTP tương ứng
 */
export async function proxyBackend(
  incomingRequest: NextRequest,
  targetBackendPath: string,
  options: ProxyBackendOptions = {}
): Promise<NextResponse> {
  const method = options.method || incomingRequest.method;
  const forwardBody =
    options.forwardBody ?? (method !== "GET" && method !== "HEAD");

  let body: unknown = undefined;
  if (options.customBody !== undefined) {
    body = options.customBody;
  } else if (forwardBody) {
    try {
      body = await incomingRequest.json();
    } catch {
      // Body rỗng hoặc không phải JSON
      body = undefined;
    }
  }

  try {
    const response = await backendApi.requestRaw(targetBackendPath, {
      method,
      body: body !== undefined ? JSON.stringify(body) : undefined,
      params: options.params,
      incomingRequest,
      skipAuth: options.skipAuth,
      timeoutMs: options.timeoutMs ?? 10000,
      cache: "no-store",
    });

    let payload: unknown;
    try {
      payload = await response.json();
    } catch {
      payload = await response.text();
    }

    return NextResponse.json(payload, { status: response.status });
  } catch (error: unknown) {
    const status = error instanceof ApiError ? error.status : 503;
    const message =
      error instanceof Error ? error.message : "Backend service is unavailable";

    return NextResponse.json(
      {
        error: message,
        status,
        timestamp: new Date().toISOString(),
      },
      { status }
    );
  }
}
