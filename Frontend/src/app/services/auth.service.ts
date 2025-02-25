import { Injectable } from "@angular/core";
import { AuthRequest } from "../models/auth-request";
import { AuthResponse } from "../models/auth-response";
import { Result } from "../models/result";
import { User } from "../models/user";
import { ApiService } from "./api.service";
import { WebsocketService } from "./websocket.service";


@Injectable({
  providedIn: 'root',
})
export class AuthService {

  private readonly USER_KEY = 'user';
  private readonly TOKEN_KEY = 'jwtToken';
  token: string = null;

  constructor(private api: ApiService, private webSocketService: WebsocketService) {
    this.init();
  }

  // Login si se recuerda la sesión
  private async init() {
    const token = localStorage.getItem(this.TOKEN_KEY) || sessionStorage.getItem(this.TOKEN_KEY);
    if (token) {
      this.token = token;
      this.api.jwt = token;

      await this.connectWebSocket();
    }
  }

  // Registro
  async signup(formData: any): Promise<Result<any>> {
    return this.api.post<any>('Auth/Signup', formData);
  }

  // Iniciar sesión
  async login(authData: AuthRequest, rememberMe: boolean): Promise<Result<AuthResponse>> {
    const result = await this.api.post<AuthResponse>('Auth/login', authData);

    if (result.success) {
      const { accessToken, user } = result.data;

      this.api.jwt = result.data.accessToken;
      this.token = result.data.accessToken;

      if (rememberMe) {
        localStorage.setItem(this.TOKEN_KEY, accessToken);
        localStorage.setItem(this.USER_KEY, JSON.stringify(user));
      } else {
        sessionStorage.setItem(this.TOKEN_KEY, accessToken);
        sessionStorage.setItem(this.USER_KEY, JSON.stringify(user));
      }

      await this.connectWebSocket();
    }

    return result;
  }

  // Conexión con el WebSocket
  async connectWebSocket() {
    return this.webSocketService.connectRxjs(this.token, this.isAuthenticated());
  }

  // Comprobar si el usuario está logeado
  isAuthenticated(): boolean {
    const token = localStorage.getItem(this.TOKEN_KEY) || sessionStorage.getItem(this.TOKEN_KEY);
    return !!token;
  }

  // Cerrar sesión
  logout(): void {
    this.webSocketService.disconnectRxjs();
    sessionStorage.removeItem(this.TOKEN_KEY);
    localStorage.removeItem(this.TOKEN_KEY);
    sessionStorage.removeItem(this.USER_KEY);
    localStorage.removeItem(this.USER_KEY);
  }

  // Obtener datos del usuario
  getUser(): User {
    const user = localStorage.getItem(this.USER_KEY) || sessionStorage.getItem(this.USER_KEY);
    return user ? JSON.parse(user) : null;
  }

  // Comprueba si es admin
  isAdmin(): boolean {
    const user = this.getUser();
    if (user.role == "Admin") {
      return true
    } else {
      return false
    }
  }

}
