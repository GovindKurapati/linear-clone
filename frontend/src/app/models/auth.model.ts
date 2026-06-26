// Mirrors the backend auth DTOs.

export interface RegisterRequest {
  email: string;
  password: string;
  displayName: string;
  workspaceName: string;
}

export interface LoginRequest {
  email: string;
  password: string;
}

export interface AuthResponse {
  token: string;
  userId: string;
  email: string;
  displayName: string;
  workspaceId: string;
  workspaceName: string;
}
