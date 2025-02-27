import { Injectable } from '@angular/core';
import { User } from '../models/user';
import { Result } from '../models/result';
import { ApiService } from './api.service';
import { GameHistory } from '../models/game-history';

@Injectable({
  providedIn: 'root'
})
export class UserService {

  constructor(private api: ApiService) { }

  // Obtener información de un usuario por su ID
  async getUserById(userId: number): Promise<Result<User>> {
    return this.api.get<User>(`User/${userId}`);
  }

  // Buscar usuarios por nickname
  async searchUsers(nickname: string): Promise<Result<User[]>> {
    return this.api.get<User[]>(`User/search`, { nickname });
  }

  // Obtener todos los usuarios
  async getAllUsers(): Promise<Result<User[]>> {
    return this.api.get<User[]>(`User/allUsers`);
  }

  // Obtener historial de partidas de un usuario
  async GetGameHistories(userId: number): Promise<Result<GameHistory[]>> {
    return this.api.get<GameHistory[]>(`GameHistory/user/${userId}`);
  }

  // Actualizar datos del usuario
  async updateUserProfile(formData: FormData): Promise<Result<any>> {
    return this.api.put<any>('User/modifyUser', formData, 'application/json');
  }

  // Modificar la contraseña del usuario
  async modifyPassword(newPassword: string): Promise<Result<any>> {
    const body = { newPassword: newPassword };
    return this.api.put<any>('User/modifyPassword', body, 'application/json');
  }

  // Modificar rol del usuario
  async modifyRole(userId: number, newRole: string): Promise<Result<any>> {
    const body = { userId: userId, newRole: newRole }
    return this.api.put<any>('User/modifyUserRole', body, 'application/json');
  }

  // Modificar prohibición de un usuario
  async modifyBan(userId: number, isBanned: boolean): Promise<Result<any>> {
    const body = { userId: userId, isBanned: isBanned }
    return this.api.put<any>('User/modifyUserBan', body, 'application/json');
  }

}
