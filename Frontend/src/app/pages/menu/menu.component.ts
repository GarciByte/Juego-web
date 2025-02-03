import { Component, OnDestroy, OnInit } from '@angular/core';
import { Router, RouterModule } from '@angular/router';
import { AuthService } from '../../services/auth.service';
import Swal from 'sweetalert2';
import { User, UserStatus } from '../../models/user';
import { environment } from '../../../environments/environment.development';
import { WebsocketService } from '../../services/websocket.service';
import { Subscription } from 'rxjs';
import { FriendRequestService } from '../../services/friend-request.service';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { FriendRequest } from '../../models/friend-request';
import { GameStats } from '../../models/game-stats';
import { UserService } from '../../services/user.service';
import { WebSocketMessage } from '../../models/web-socket-message';

@Component({
  selector: 'app-menu',
  standalone: true,
  imports: [FormsModule, CommonModule, RouterModule],
  templateUrl: './menu.component.html',
  styleUrl: './menu.component.css'
})
export class MenuComponent implements OnInit, OnDestroy {

  public readonly IMG_URL = environment.apiImg;

  user: User;
  avatarUrl: string;
  friends: User[] = [];

  friendRequests: FriendRequest[] = [];
  filteredFriends: User[] = [];
  filteredUsers: User[] = [];

  searchText: string = '';  // Para buscar amigos
  searchQuery: string = ''; // Para buscar usuarios

  totalPlayers: number = 0;
  activeGames: number = 0;
  playersInGames: number = 0;

  message: WebSocketMessage;

  isConnected: boolean = false;
  connected$: Subscription;
  messageReceived$: Subscription;
  disconnected$: Subscription;

  constructor(
    private router: Router,
    private authService: AuthService,
    private webSocketService: WebsocketService,
    private friendRequestService: FriendRequestService,
    private userService: UserService
  ) { }

  ngOnInit(): void {
    if (!this.authService.isAuthenticated()) {
      this.router.navigate(['/login']);
    }

    this.user = this.authService.getUser();
    this.avatarUrl = this.IMG_URL + this.user.avatar.url;

    this.connected$ = this.webSocketService.connected.subscribe(() => {
      this.isConnected = true;
      //console.log(this.isConnected);
    });

    this.messageReceived$ = this.webSocketService.messageReceived.subscribe(message => {
      //console.log('Mensaje recibido en el componente:', message);
      this.webSocketMessage(message);
    });

    this.disconnected$ = this.webSocketService.disconnected.subscribe(() => this.isConnected = false);

    this.loadFriends();
    this.loadFriendRequests();
  }

  ngOnDestroy(): void {
    this.connected$.unsubscribe();
    this.messageReceived$.unsubscribe();
    this.disconnected$.unsubscribe();
  }

  // Ejecuta una acción dependiendo del mensaje del WebSocket
  webSocketMessage(message: WebSocketMessage): void {
    console.log('Mensaje recibido del WebSocket:', message);

    switch (message.Type) {
      case 'Connection':
        console.log(message.Content);
        break;
      case 'FriendListUpdate':
        //console.log("FriendListUpdate")
        this.loadFriends();
        break;
      case 'FriendRequestUpdate':
        console.log("FriendRequestUpdate")
        this.loadFriendRequests();
        break;
      case 'StatsUpdate':
        //console.log("StatsUpdate")
        this.updateStats(message.Content);
        break;
      case 'FriendStatusUpdate':
        //console.log("FriendStatusUpdate")
        this.updateFriendStatus(message.Content);
        break;
      default:
        console.error('Tipo de mensaje no reconocido:', message.Type);
    }
  }

  // Carga la lista de amigos
  async loadFriends(): Promise<void> {
    try {
      const result = await this.friendRequestService.getFriends(this.user.userId);
      this.friends = result.data;
      this.filterFriends();

      console.log("Lista de amigos:", this.friends);

    } catch (error) {
      console.error('Error al cargar amigos:', error);
    }
  }

  // Filtra la lista de amigos
  filterFriends(): void {
    const query = this.normalizeText(this.searchText);
    this.filteredFriends = this.friends.filter(friend =>
      this.normalizeText(friend.nickname).includes(query)
    );
  }

  // Quita tildes y pone el texto en minúsculas
  normalizeText(text: string): string {
    return text.normalize("NFD").replace(/[\u0300-\u036f]/g, "").toLowerCase();
  }

  // Carga las solicitudes de amistad pendientes
  async loadFriendRequests(): Promise<void> {
    try {
      const result = await this.friendRequestService.getPendingRequests(this.user.userId);
      this.friendRequests = result.data;
      console.log("Solicitudes de amistad:", this.friendRequests)

    } catch (error) {
      console.error('Error al cargar solicitudes de amistad:', error);
    }
  }

  // Filtra la lista de usuarios
  async filterUsers(): Promise<void> {
    if (!this.searchQuery.trim()) {
      this.filteredUsers = [];
    } else {

      try {
        const result = await this.userService.searchUsers(this.searchQuery);
        this.filteredUsers = result.data;
        console.log(result);

        if (this.filteredUsers.length === 0) {
          console.log("No hay resultados");
        }

      } catch (error) {
        console.error('Error al buscar usuarios:', error);
        this.filteredUsers = [];
      }

    }
  }

  // Envía una solicitud de amistad
  async sendFriendRequest(user: User): Promise<void> {
    try {
      await this.friendRequestService.sendRequest(this.user.userId, user.userId);
      alert("Solicitud enviada");

    } catch (error) {
      console.error('Error al enviar solicitud:', error);
    }
  }

  // Acepta una solicitud de amistad
  async acceptFriendRequest(request: FriendRequest): Promise<void> {
    try {
      await this.friendRequestService.acceptRequest(request.id);
      await this.loadFriends();
      await this.loadFriendRequests();

    } catch (error) {
      console.error('Error al aceptar solicitud:', error);
    }
  }

  // Rechaza una solicitud de amistad
  async rejectFriendRequest(request: FriendRequest): Promise<void> {
    try {
      await this.friendRequestService.rejectRequest(request.id);
      await this.loadFriendRequests();

    } catch (error) {
      console.error('Error al rechazar solicitud:', error);
    }
  }

  // Actualiza estadísticas en tiempo real
  updateStats(stats: GameStats): void {
    console.log('Actualizando estadísticas con:', stats);

    this.totalPlayers = stats.TotalPlayers;
    this.activeGames = stats.ActiveGames;
    this.playersInGames = stats.PlayersInGames;
  }

  // Actualiza estado del amigo
  updateFriendStatus(updatedFriend: any): void {
    console.log("Nuevo estado del amigo: ", updatedFriend)

    if (!updatedFriend) {
      this.filteredFriends.forEach(friend => {
        friend.status = UserStatus.Offline;
        console.log("Status_1", friend.status);
      });

    } else {

      if (updatedFriend.length > 0) {
        const userId = updatedFriend[0].UserId;
        const status = updatedFriend[0].Status as UserStatus;
        const friend = this.filteredFriends.find(friend => friend.userId === userId);
        console.log("Status_2", status);

        if (friend) {
          friend.status = status;
          console.log("Status_3", friend.status);
        }
      }
    }
  }

  // Borrar un amigo
  async deletefriend(friend: any): Promise<void> {
    if (confirm(`¿Estás seguro de eliminar a ${friend.nickname} de tus amigos?`)) {
      try {
        await this.friendRequestService.removeFriend(this.user.userId, friend.userId);
        await this.loadFriends();

      } catch (error) {
        console.error('Error al borrar el amigo:', error);
      }
    }
  }

  // Redirigir al perfil de otro usuario
  navigateToProfile(userId: number) {
    console.log("Navegando al perfil del usuario:", userId);

    this.router.navigate(['/friend-profile'], {
      queryParams: {
        id: userId
      },
    });

  }

  // Cerrar sesión
  logout(): void {
    Swal.fire({
      title: "Has cerrado sesión con éxito",
      text: `¡Hasta pronto ${this.user.nickname}!`,
      icon: 'success',
      showConfirmButton: false,
      timer: 3000,
      timerProgressBar: true,
      didClose: () => {
        this.authService.logout();
        this.router.navigate(['/login']);
      }
    });
  }

}