import { Component, OnDestroy, OnInit } from '@angular/core';
import { Router, RouterModule } from '@angular/router';
import { AuthService } from '../../services/auth.service';
import Swal from 'sweetalert2';
import { User } from '../../models/user';
import { environment } from '../../../environments/environment.development';
import { WebsocketService } from '../../services/websocket.service';
import { Subscription } from 'rxjs';
import { FriendRequestService } from '../../services/friend-request.service';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { FriendRequest } from '../../models/friend-request';
import { UserService } from '../../services/user.service';
import { GameInvitation } from '../../models/game-invitation';
import { GameRoomAction, RoomAction } from '../../models/game-room-action';
import { WebSocketMessage, MsgType } from '../../models/web-socket-message';

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

  // Suscripciones a los datos necesarios
  friendListSubscription: Subscription;
  gameInvitationSubscription: Subscription;
  totalPlayersSubscription: Subscription;
  activeGamesSubscription: Subscription;
  playersInGamesSubscription: Subscription;

  // Suscripciones generales
  connected$: Subscription;
  disconnected$: Subscription;
  error$: Subscription;

  constructor(
    private router: Router,
    private authService: AuthService,
    private webSocketService: WebsocketService,
    private friendRequestService: FriendRequestService,
    private userService: UserService
  ) { }

  async ngOnInit(): Promise<void> {
    if (!this.authService.isAuthenticated()) {
      this.router.navigate(['/']);
    }

    this.user = this.authService.getUser();
    this.avatarUrl = this.IMG_URL + this.user.avatar.url;

    // Cargar la lista de amigos y solicitudes de amistad
    await this.loadFriends();
    await this.loadFriendRequests();

    // Solicitar los datos necesarios
    if (this.webSocketService.isConnectedRxjs()) {
      this.webSocketService.initMenuData(this.user.userId);
    } else {
      this.connected$ = this.webSocketService.connected.subscribe(() => {
        this.webSocketService.initMenuData(this.user.userId);
      });
    }

    // Desconexión del usuario
    this.disconnected$ = this.webSocketService.disconnected.subscribe(() => {
      console.warn('Desconectado del WebSocket');
    });

    // Error del WebSocket
    this.error$ = this.webSocketService.error.subscribe(() => {
      this.authService.logout();
      this.router.navigate(['/']);
    });

    // Lista de amigos con sus estados
    this.friendListSubscription = this.webSocketService.friendList$.subscribe((friends) => {
      this.friends = friends;
      this.filterFriends();
    });

    // Invitaciones a partidas
    this.gameInvitationSubscription = this.webSocketService.gameInvitationSubject.subscribe(async (invitation: GameInvitation) => {
      await this.handleInvitation(invitation);
    });

    // Estadísticas del juego
    this.totalPlayersSubscription = this.webSocketService.totalPlayersSubject.subscribe(total => {
      this.totalPlayers = total;
    });
    this.activeGamesSubscription = this.webSocketService.activeGamesSubject.subscribe(active => {
      this.activeGames = active;
    });
    this.playersInGamesSubscription = this.webSocketService.playersInGamesSubject.subscribe(players => {
      this.playersInGames = players;
    });
  }

  ngOnDestroy(): void {
    if (this.connected$) {
      this.connected$.unsubscribe();
    }

    if (this.disconnected$) {
      this.disconnected$.unsubscribe();
    }

    if (this.gameInvitationSubscription) {
      this.gameInvitationSubscription.unsubscribe();
    }

    if (this.totalPlayersSubscription) {
      this.totalPlayersSubscription.unsubscribe();
    }

    if (this.activeGamesSubscription) {
      this.activeGamesSubscription.unsubscribe();
    }

    if (this.playersInGamesSubscription) {
      this.playersInGamesSubscription.unsubscribe();
    }

    if (this.friendListSubscription) {
      this.friendListSubscription.unsubscribe();
    }
  }

  // Invitaciones a partidas
  async handleInvitation(invitation: GameInvitation): Promise<void> {
    console.log('Invitación recibida:', invitation);
    const result = await this.userService.getUserById(invitation.FromUserId);

    if (result.success) {
      const friend = result.data
      const acceptInvitation = confirm(`Has sido invitado por ${friend.nickname} para jugar una partida. ¿Aceptas la invitación?`);

      if (acceptInvitation) {

        const action: GameRoomAction = {
          Action: RoomAction.Friend,
          FriendId: invitation.FromUserId,
        };

        const message: WebSocketMessage = {
          Type: MsgType.GameRoom,
          Id: this.user.userId,
          Content: action,
        };

        await this.webSocketService.sendRxjs(message);
        this.router.navigate(['/matchmaking'], { queryParams: { userId: invitation.FromUserId } });

      } else {
        console.log("Invitación rechazada.");
      }
    }
  }

  // Carga la lista de amigos
  async loadFriends(): Promise<void> {
    try {
      const result = await this.friendRequestService.getFriends(this.user.userId);
      this.webSocketService.setFriendList(result.data);
      this.friends = result.data;
      this.filterFriends();
      //console.log("Lista de amigos:", this.friends);

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
      //console.log("Solicitudes de amistad:", this.friendRequests)

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

  // Elimina un amigo
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