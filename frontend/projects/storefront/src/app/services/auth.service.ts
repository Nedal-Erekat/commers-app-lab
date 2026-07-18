import { isPlatformBrowser } from '@angular/common';
import { HttpClient } from '@angular/common/http';
import { Injectable, PLATFORM_ID, computed, inject, signal } from '@angular/core';
import { Observable, tap } from 'rxjs';
import { environment } from '../../environments/environment';
import { AuthResponse } from '../models/auth';

const STORAGE_KEY = 'commerce-app-lab.auth';

@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly http = inject(HttpClient);
  private readonly isBrowser = isPlatformBrowser(inject(PLATFORM_ID));
  private readonly baseUrl = `${environment.apiUrl}/api/auth`;

  private readonly session = signal<AuthResponse | null>(this.readStoredSession());

  readonly currentUser = computed(() => this.session());
  readonly isAuthenticated = computed(() => {
    const session = this.session();
    return session !== null && new Date(session.expiresAt) > new Date();
  });

  register(email: string, password: string): Observable<AuthResponse> {
    return this.http
      .post<AuthResponse>(`${this.baseUrl}/register`, { email, password })
      .pipe(tap((response) => this.storeSession(response)));
  }

  login(email: string, password: string): Observable<AuthResponse> {
    return this.http
      .post<AuthResponse>(`${this.baseUrl}/login`, { email, password })
      .pipe(tap((response) => this.storeSession(response)));
  }

  logout(): void {
    if (this.isBrowser) localStorage.removeItem(STORAGE_KEY);
    this.session.set(null);
  }

  getToken(): string | null {
    return this.session()?.token ?? null;
  }

  private storeSession(response: AuthResponse): void {
    if (this.isBrowser) localStorage.setItem(STORAGE_KEY, JSON.stringify(response));
    this.session.set(response);
  }

  private readStoredSession(): AuthResponse | null {
    if (!this.isBrowser) return null;

    const raw = localStorage.getItem(STORAGE_KEY);
    if (!raw) return null;

    try {
      return JSON.parse(raw) as AuthResponse;
    } catch {
      return null;
    }
  }
}
