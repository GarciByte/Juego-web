import { Injectable } from '@angular/core';
import { FriendRequest } from '../models/friend-request';
import { User } from '../models/user';
import { ApiService } from './api.service';
import { Result } from '../models/result';

@Injectable({
  providedIn: 'root'
})
export class FriendRequestService {

  constructor(private api: ApiService) { }

  // Enviar una solicitud de amistad
  async sendRequest(senderId: number, receiverId: number): Promise<Result<void>> {
    return this.api.post<void>(`FriendRequest/send?senderId=${senderId}&receiverId=${receiverId}`);
  }

  // Aceptar una solicitud de amistad
  async acceptRequest(requestId: number): Promise<Result<void>> {
    return this.api.post<void>(`FriendRequest/accept?requestId=${requestId}`);
  }

  // Rechazar una solicitud de amistad
  async rejectRequest(requestId: number): Promise<Result<void>> {
    return this.api.post<void>(`FriendRequest/reject?requestId=${requestId}`);
  }

  // Obtener todas las solicitudes de amistad pendientes de un usuario
  async getPendingRequests(userId: number): Promise<Result<FriendRequest[]>> {
    return this.api.get<FriendRequest[]>(`FriendRequest/pending?userId=${userId}`);
  }

  // Obtener todas las solicitudes de amistad enviadas por un usuario que no han sido aceptadas
  async getPendingSentRequests(userId: number): Promise<Result<FriendRequest[]>> {
    return this.api.get<FriendRequest[]>(`FriendRequest/pending-sent?userId=${userId}`);
  }

  // Obtener la lista de amigos
  async getFriends(userId: number): Promise<Result<User[]>> {
    return this.api.get<User[]>(`FriendRequest/friends?userId=${userId}`);
  }

  // Eliminar un amigo
  async removeFriend(userId: number, friendId: number): Promise<Result<void>> {
    return this.api.delete<void>(`FriendRequest/remove-friend/${userId}/${friendId}`);
  }
}
