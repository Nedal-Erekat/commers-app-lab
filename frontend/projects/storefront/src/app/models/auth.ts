export interface AuthResponse {
  token: string;
  email: string;
  expiresAt: string;
  roles: string[];
}

export interface AuthErrorResponse {
  errors: string[];
}
