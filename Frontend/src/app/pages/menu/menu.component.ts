import { Component, OnDestroy, OnInit } from '@angular/core';
import { Router, RouterModule } from '@angular/router';
import { AuthService } from '../../services/auth.service';
import Swal from 'sweetalert2';
import { User } from '../../models/user';
import { environment } from '../../../environments/environment';
import { WebsocketService } from '../../services/websocket.service';
import { Subscription } from 'rxjs';
import { FriendRequestService } from '../../services/friend-request.service';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { FriendRequest } from '../../models/friend-request';
import { UserService } from '../../services/user.service';
import { GameInvitation } from '../../models/game-invitation';

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
  friendRequestsSent: FriendRequest[] = [];
  filteredFriends: User[] = [];
  filteredUsers: User[] = [];

  searchText: string = '';  // Para buscar amigos
  searchQuery: string = ''; // Para buscar usuarios

  totalPlayers: number = 0;
  activeGames: number = 0;
  playersInGames: number = 0;

  // Suscripciones a los datos necesarios
  connected$: Subscription;
  disconnected$: Subscription;
  error$: Subscription;
  friendListSubscription: Subscription;
  gameInvitationSubscription: Subscription;
  totalPlayersSubscription: Subscription;
  activeGamesSubscription: Subscription;
  playersInGamesSubscription: Subscription;
  friendRequestSubscription: Subscription;
  friendsSubscription: Subscription;
  errorGameInvitationSubject: Subscription;


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
    await this.loadFriendRequestsSent();

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
      console.warn('Desconectado del Servidor');
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

    // Recibir notificación de de la lista de amigos
    this.friendRequestSubscription = this.webSocketService.friendsSubject.subscribe(async () => {
      await this.loadFriends();
      this.webSocketService.initMenuData(this.user.userId);
    });

    // Recibir notificación de solicitud de amistad
    this.friendsSubscription = this.webSocketService.friendRequestSubject.subscribe(async () => {
      await this.loadFriendRequests();
      await this.loadFriendRequestsSent();
    });

    // Invitaciones a partidas
    this.gameInvitationSubscription = this.webSocketService.gameInvitation$.subscribe(async (invitation: GameInvitation) => {
      await this.handleInvitation(invitation);
    });

    // Cancela la invitación a una partida
    this.errorGameInvitationSubject = this.webSocketService.errorGameInvitationSubject.subscribe(() => {
      Swal.fire({
        title: "Invitación cancelada",
        icon: "info",
        confirmButtonText: "Aceptar"
      });
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

    if (this.error$) {
      this.error$.unsubscribe();
    }

    if (this.friendListSubscription) {
      this.friendListSubscription.unsubscribe();
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

    if (this.friendRequestSubscription) {
      this.friendRequestSubscription.unsubscribe();
    }

    if (this.friendsSubscription) {
      this.friendsSubscription.unsubscribe();
    }

    if (this.errorGameInvitationSubject) {
      this.errorGameInvitationSubject.unsubscribe();
    }
  }

  // Invitaciones a partidas
  async handleInvitation(invitation: GameInvitation): Promise<void> {

    if (invitation) {
      const result = await this.userService.getUserById(invitation.FromUserId);

      if (result.success) {
        const friend = result.data

        Swal.fire({
          title: `Has sido invitado por ${friend.nickname} para jugar una partida. ¿Aceptas la invitación?`,
          icon: "info",
          showCancelButton: true,
          confirmButtonText: "Sí",
          cancelButtonText: "No"
        }).then((result) => {
          if (result.isConfirmed) {
            this.acceptInvitation(invitation);
          } else {
            this.cancelInvitation(invitation);
          }
        });

      } else {
        //console.error('Error en la invitación:', result.error);
        this.throwError("Se ha producido un error con la invitación");
      }
    }
  }

  // Acepta la invitación
  async acceptInvitation(invitation: GameInvitation) {
    await this.webSocketService.acceptInvitation(this.user, invitation);
    this.router.navigate(['/matchmaking'], { queryParams: { userId: invitation.FromUserId } });
  }

  // Rechaza la invitación
  async cancelInvitation(invitation: GameInvitation) {
    await this.webSocketService.cancelInvitation(invitation);
  }

  // Carga la lista de amigos
  async loadFriends(): Promise<void> {
    try {
      const result = await this.friendRequestService.getFriends(this.user.userId);
      this.webSocketService.setFriendList(result.data);
      this.friends = result.data;
      this.filterFriends();

    } catch (error) {
      //console.error('Error al cargar amigos:', error);
      this.throwError("Se ha producido un error al cargar la lista de amigos");
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

    } catch (error) {
      //console.error('Error al cargar solicitudes de amistad:', error);
      this.throwError("Se ha producido un error al cargar las solicitudes de amistad");
    }
  }

  // Carga las solicitudes de amistad enviadas
  async loadFriendRequestsSent(): Promise<void> {
    try {
      const result= await this.friendRequestService.getPendingSentRequests(this.user.userId);
      this.friendRequestsSent = result.data;

    } catch (error) {
      //console.error('Error al cargar solicitudes de amistad enviadas:', error);
      this.throwError("Se ha producido un error al cargar las solicitudes de amistad enviadas");
    }
  }

  // Filtra la lista de usuarios
  async filterUsers(): Promise<void> {
    if (!this.searchQuery.trim()) {
      this.filteredUsers = [];
    } else {

      try {
        const result = await this.userService.searchUsers(this.searchQuery);
        let users = result.data;
        users = users.filter(user => user.userId !== this.user.userId);
        this.filteredUsers = users;

        if (this.filteredUsers.length === 0) {
          Swal.fire({
            title: "No hay resultados",
            icon: "info",
            confirmButtonText: "Aceptar"
          });
        }

      } catch (error) {
        //console.error('Error al buscar usuarios:', error);
        this.throwError("Se ha producido un error al buscar usuarios");
        this.filteredUsers = [];
      }
    }
  }

  // Envía una solicitud de amistad
  async sendFriendRequest(user: User): Promise<void> {
    try {
      const result = await this.friendRequestService.sendRequest(this.user.userId, user.userId);

      if (result.error === "OK") {
        Swal.fire({
          title: "Se ha enviado la solicitud de amistad",
          icon: 'success',
          showConfirmButton: false,
          timer: 2000,
          timerProgressBar: true
        });
        await this.loadFriendRequestsSent();

      } else {
        this.throwError("Se ha producido un error al enviar la solicitud");
      }

    } catch (error) {
      //console.error('Error al enviar solicitud:', error);
      this.throwError("Se ha producido un error al enviar la solicitud");
    }
  }

  // Acepta una solicitud de amistad
  async acceptFriendRequest(request: FriendRequest): Promise<void> {
    try {
      await this.friendRequestService.acceptRequest(request.id);
      await this.loadFriends();
      await this.loadFriendRequests();
      await this.loadFriendRequestsSent();
      this.webSocketService.initMenuData(this.user.userId);

    } catch (error) {
      //console.error('Error al aceptar solicitud:', error);
      this.throwError("Se ha producido un error al aceptar la solicitud");
    }
  }

  // Rechaza una solicitud de amistad
  async rejectFriendRequest(request: FriendRequest): Promise<void> {
    try {
      await this.friendRequestService.rejectRequest(request.id);
      await this.loadFriendRequests();
      await this.loadFriendRequestsSent();

    } catch (error) {
      //console.error('Error al rechazar solicitud:', error);
      this.throwError("Se ha producido un error al rechazar la solicitud");
    }
  }

  // Elimina un amigo
  async deletefriend(friend: any): Promise<void> {
    Swal.fire({
      title: `¿Estás seguro de que quieres eliminar a ${friend.nickname} de tus amigos?`,
      icon: "warning",
      showCancelButton: true,
      confirmButtonText: "Sí",
      cancelButtonText: "No"
    }).then(async (result) => {
      if (result.isConfirmed) {
        try {
          await this.friendRequestService.removeFriend(this.user.userId, friend.userId);
          await this.loadFriends();
          this.webSocketService.initMenuData(this.user.userId);

        } catch (error) {
          //console.error('Error al borrar el amigo:', error);
          this.throwError("Se ha producido un error al borrar el amigo");
        }
      } else {
        Swal.close();
      }
    });
  }

  // Verifica si el usuario buscado ya es amigo
  isFriend(user: User): boolean {
    return this.friends.some(friend => friend.userId === user.userId);
  }

  // Verifica si ya se le ha enviado una solicitud de amistad al usuario buscado
  hasPendingSentFriendRequest(user: User): boolean {
    return this.friendRequestsSent.some(request =>
      request.senderId === this.user.userId && request.receiverId === user.userId
    );
  }

  // Verifica si ya se ha recibido una solicitud de amistad del usuario buscado
  hasPendingFriendRequest(user: User): boolean {
    return this.friendRequests.some(request =>
      request.senderId === user.userId && request.receiverId === this.user.userId
    );
  }

  // Redirigir al perfil de otro usuario
  navigateToProfile(userId: number) {
    this.router.navigate(['/friend-profile'], {
      queryParams: {
        id: userId
      },
    });
  }

  // Redirigir al perfil Admin
  goToAdminProfile(): void {
    this.router.navigate(['/admin-profile']);
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

  throwError(error: string) {
    Swal.fire({
      title: error,
      icon: "error",
      confirmButtonText: "Aceptar"
    });
  }

}