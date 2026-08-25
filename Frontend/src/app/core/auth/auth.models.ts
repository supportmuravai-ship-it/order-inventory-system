export interface LoginRequest {
  email: string;
  password: string;
}

export interface CurrentUser {
  id: string;
  name: string;
  email: string;
  roles: string[];
}

export interface Store {
  id: number;
  name: string;
  code: string;
}