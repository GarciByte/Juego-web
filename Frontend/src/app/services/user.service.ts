import { Injectable } from '@angular/core';
import { User } from '../models/user';
import { Result } from '../models/result';
import { ApiService } from './api.service';

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

}
