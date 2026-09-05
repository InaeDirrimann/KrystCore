import type {
  ClientOptions,
  Content,
  ContentCreateRequest,
  ContentUpdateRequest,
  FilterParams,
  PaginationParams,
} from './types.ts';
import { PermissionBitmask } from './permissions.ts';

export class KrystApiError extends Error {
  public readonly status: number;
  public readonly body: unknown;

  public constructor(status: number, message: string, body?: unknown) {
    super(`KrystCore API Error [${status}]: ${message}`);
    this.name = 'KrystApiError';
    this.status = status;
    this.body = body;
  }
}

export class KrystClient {
  private readonly baseUrl: string;
  private readonly headers: Record<string, string>;
  private readonly cookie?: string;
  private readonly customFetch: typeof fetch;

  public constructor(options: ClientOptions) {
    this.baseUrl = options.baseUrl.replace(/\/+$/, '');
    this.headers = {
      Accept: 'application/json',
      ...(options.headers ?? {}),
    };
    this.cookie = options.cookie;
    this.customFetch = options.fetch ?? globalThis.fetch.bind(globalThis);
  }

  public withPermissions(bitmask: bigint | number | string | PermissionBitmask): KrystClient {
    const mask = bitmask instanceof PermissionBitmask ? bitmask : new PermissionBitmask(bitmask);
    return this.withHeader('X-User-Permissions', mask.toBigInt().toString());
  }

  public withUserId(userId: string): KrystClient {
    return this.withHeader('X-User-Id', userId);
  }

  public withHeader(name: string, value: string): KrystClient {
    return new KrystClient({
      baseUrl: this.baseUrl,
      headers: { ...this.headers, [name]: value },
      cookie: this.cookie,
      fetch: this.customFetch,
    });
  }

  private buildUrl(path: string, params?: PaginationParams & FilterParams): string {
    const url = new URL(`${this.baseUrl}${path}`);
    if (params) {
      for (const [key, value] of Object.entries(params)) {
        if (value !== undefined && value !== null) {
          url.searchParams.set(key, String(value));
        }
      }
    }
    return url.toString();
  }

  private async request<T>(path: string, init: RequestInit = {}): Promise<T> {
    const headers = new Headers(this.headers);
    if (this.cookie) {
      headers.set('Cookie', this.cookie);
    }
    if (init.headers) {
      new Headers(init.headers).forEach((v, k) => headers.set(k, v));
    }

    const response = await this.customFetch(path, {
      ...init,
      headers,
      credentials: this.cookie ? 'include' : init.credentials,
    });

    if (!response.ok) {
      let body: unknown;
      try {
        body = await response.json();
      } catch {
        body = await response.text();
      }
      const msg = typeof body === 'object' && body !== null && 'error' in body
        ? String((body as { error: unknown }).error)
        : response.statusText;
      throw new KrystApiError(response.status, msg, body);
    }

    if (response.status === 204) {
      return undefined as T;
    }

    const text = await response.text();
    if (!text || text.trim().length === 0) {
      return undefined as T;
    }

    try {
      return JSON.parse(text) as T;
    } catch {
      throw new KrystApiError(response.status, 'Invalid JSON response from server', text);
    }
  }

  public async getContentList<TData = Record<string, unknown>>(
    contentType: string,
    params?: PaginationParams & FilterParams
  ): Promise<Content<TData>[]> {
    const url = this.buildUrl(`/api/v1/content/${encodeURIComponent(contentType)}`, params);
    return this.request<Content<TData>[]>(url, { method: 'GET' });
  }

  public async getContentById<TData = Record<string, unknown>>(
    contentType: string,
    idOrSlug: string
  ): Promise<Content<TData>> {
    const url = this.buildUrl(`/api/v1/content/${encodeURIComponent(contentType)}/${encodeURIComponent(idOrSlug)}`);
    return this.request<Content<TData>>(url, { method: 'GET' });
  }

  public async createContent<TData = Record<string, unknown>>(
    contentType: string,
    request: ContentCreateRequest<TData>
  ): Promise<Content<TData>> {
    const url = this.buildUrl(`/api/v1/content/${encodeURIComponent(contentType)}`);
    return this.request<Content<TData>>(url, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify(request),
    });
  }

  public async updateContent<TData = Record<string, unknown>>(
    contentType: string,
    idOrSlug: string,
    request: ContentUpdateRequest<TData>
  ): Promise<Content<TData>> {
    const url = this.buildUrl(`/api/v1/content/${encodeURIComponent(contentType)}/${encodeURIComponent(idOrSlug)}`);
    return this.request<Content<TData>>(url, {
      method: 'PUT',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify(request),
    });
  }

  public async deleteContent(contentType: string, idOrSlug: string): Promise<boolean> {
    const url = this.buildUrl(`/api/v1/content/${encodeURIComponent(contentType)}/${encodeURIComponent(idOrSlug)}`);
    await this.request<void>(url, { method: 'DELETE' });
    return true;
  }
}
