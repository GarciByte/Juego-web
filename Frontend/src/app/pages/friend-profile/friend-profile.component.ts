import { Component, OnDestroy, OnInit } from '@angular/core';
import { ActivatedRoute, ParamMap, Router, RouterModule } from '@angular/router';
import { environment } from '../../../environments/environment';
import { User } from '../../models/user';
import { AuthService } from '../../services/auth.service';
import { Subscription } from 'rxjs';
import { UserService } from '../../services/user.service';
import { WebsocketService } from '../../services/websocket.service';
import Swal from 'sweetalert2';
import { GameHistory } from '../../models/game-history';
import { CommonModule } from '@angular/common';
import { FriendRequest } from '../../models/friend-request';
import { FriendRequestService } from '../../services/friend-request.service';


@Component({
  selector: 'app-friend-profile',
  imports: [RouterModule, CommonModule],
  templateUrl: './friend-profile.component.html',
  styleUrl: './friend-profile.component.css'
})
export class FriendProfileComponent implements OnInit, OnDestroy {

  public readonly IMG_URL = environment.apiImg;
  user: User = null;
  currentUser: User = null;
  avatarUrl: string = "";

  friends: User[] = [];
  friendRequests: FriendRequest[] = [];
  friendRequestsSent: FriendRequest[] = [];

  gameHistories: GameHistory[] = [];
  paginatedHistories: GameHistory[] = [];

  // Paginación
  currentPage: number = 1;
  pageSize: number = 5;
  totalPages: number = 1;

  error$: Subscription;
  routeQueryMap$: Subscription;

  constructor(
    private router: Router,
    private authService: AuthService,
    private route: ActivatedRoute,
    private userService: UserService,
    private friendRequestService: FriendRequestService,
    private webSocketService: WebsocketService
  ) { }

  ngOnInit(): void {
    if (!this.authService.isAuthenticated()) {
      this.router.navigate(['/']);
    }

    this.currentUser = this.authService.getUser();

    // Error del WebSocket
    this.error$ = this.webSocketService.error.subscribe(() => {
      this.authService.logout();
      this.router.navigate(['/']);
    });

    this.routeQueryMap$ = this.route.queryParamMap.subscribe(queryMap => this.init(queryMap));
  }

  // Obtener datos del perfil
  async init(queryMap: ParamMap) {
    const userId = parseInt(queryMap.get("id"));

    if (this.currentUser.userId === userId) {
      this.router.navigate(['/menu']);
    }

    await this.getUser(userId);
    await this.getHistories(this.user.userId);

    // Cargar la lista de amigos y solicitudes de amistad
    await this.loadFriends();
    await this.loadFriendRequests();
    await this.loadFriendRequestsSent();
  }

  // Obtener datos del usuario
  async getUser(userId: number): Promise<void> {
    try {
      const result = await this.userService.getUserById(userId);

      if (result.success) {
        this.user = result.data;
        this.avatarUrl = this.user.avatar.url;
      }

    } catch (error) {
      //console.error('Error al buscar el usuario:', error);
      this.throwError("Se ha producido un error al obtener los datos del usuario");
    }
  }

  // Carga la lista de amigos
  async loadFriends(): Promise<void> {
    try {
      const result = await this.friendRequestService.getFriends(this.currentUser.userId);
      this.friends = result.data;

    } catch (error) {
      //console.error('Error al cargar amigos:', error);
      this.throwError("Se ha producido un error al cargar la lista de amigos");
    }
  }

  // Carga las solicitudes de amistad pendientes
  async loadFriendRequests(): Promise<void> {
    try {
      const result = await this.friendRequestService.getPendingRequests(this.currentUser.userId);
      this.friendRequests = result.data;

    } catch (error) {
      //console.error('Error al cargar solicitudes de amistad:', error);
      this.throwError("Se ha producido un error al cargar las solicitudes de amistad");
    }
  }

  // Carga las solicitudes de amistad enviadas
  async loadFriendRequestsSent(): Promise<void> {
    try {
      const result = await this.friendRequestService.getPendingSentRequests(this.currentUser.userId);
      this.friendRequestsSent = result.data;

    } catch (error) {
      //console.error('Error al cargar solicitudes de amistad enviadas:', error);
      this.throwError("Se ha producido un error al cargar las solicitudes de amistad enviadas");
    }
  }

  // Envía una solicitud de amistad
  async sendFriendRequest(): Promise<void> {
    try {
      const result = await this.friendRequestService.sendRequest(this.currentUser.userId, this.user.userId);

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

  // Elimina un amigo
  async deletefriend(): Promise<void> {
    Swal.fire({
      title: `¿Estás seguro de que quieres eliminar a ${this.user.nickname} de tus amigos?`,
      icon: "warning",
      showCancelButton: true,
      confirmButtonText: "Sí",
      cancelButtonText: "No"
    }).then(async (result) => {
      if (result.isConfirmed) {
        try {
          await this.friendRequestService.removeFriend(this.currentUser.userId, this.user.userId);
          await this.loadFriends();

        } catch (error) {
          //console.error('Error al borrar el amigo:', error);
          this.throwError("Se ha producido un error al borrar el amigo");
        }
        
      } else {
        Swal.close();
      }
    });
  }

  // Verifica si el usuario es amigo del usuario actual
  isFriend(): boolean {
    return this.friends.some(friend => friend.userId === this.user.userId);
  }

  // Verifica si existe alguna solicitud pendiente para este usuario
  hasPendingRequest(): boolean {
    const pendingIncoming = this.friendRequests.some(req => req.senderId === this.user.userId);
    const pendingSent = this.friendRequestsSent.some(req => req.receiverId === this.user.userId);
    return pendingIncoming || pendingSent;
  }

  // Muestra el botón para añadir/eliminar amigo
  showFriendButton(): boolean {
    return this.isFriend() || (!this.isFriend() && !this.hasPendingRequest());
  }

  // Nombre del botón
  getFriendButtonLabel(): string {
    return this.isFriend() ? "Eliminar" : "Enviar solicitud";
  }

  // Acción del botón
  onFriendButtonClick(): void {
    if (this.isFriend()) {
      this.deletefriend();
    } else {
      this.sendFriendRequest();
    }
  }

  // Obtener el historial de partidas y actualizar la paginación
  async getHistories(userId: number): Promise<void> {
    try {
      const result = await this.userService.GetGameHistories(userId);

      if (result.success) {
        this.gameHistories = result.data;

        // Orden cronológico inverso
        this.gameHistories.sort((a, b) => b.id - a.id);

        this.currentPage = 1;
        this.updatePaginatedHistories();

      } else {
        this.throwError("Se ha producido un error al obtener el historial de partidas");
      }

    } catch (error) {
      //console.error("Error al obtener el historial de partidas:", error);
      this.throwError("Se ha producido un error al obtener el historial de partidas");
    }
  }

  // Actualiza la paginación
  updatePaginatedHistories(): void {
    this.totalPages = Math.max(1, Math.ceil(this.gameHistories.length / this.pageSize));
    let startIndex = (this.currentPage - 1) * this.pageSize;
    let endIndex = startIndex + this.pageSize;
    this.paginatedHistories = this.gameHistories.slice(startIndex, endIndex);
  }

  // Cambia el tamaño de página
  onPageSizeChange(event: Event): void {
    const selectElement = event.target as HTMLSelectElement;
    let newSize = parseInt(selectElement.value, 10);
    this.pageSize = newSize;
    this.currentPage = 1;
    this.updatePaginatedHistories();
  }

  // Navegar a la primera página
  goToFirstPage(): void {
    this.currentPage = 1;
    this.updatePaginatedHistories();
  }

  // Navegar a la página anterior
  goToPreviousPage(): void {
    if (this.currentPage > 1) {
      this.currentPage = this.currentPage - 1;
      this.updatePaginatedHistories();
    }
  }

  // Navegar a la siguiente página
  goToNextPage(): void {
    if (this.currentPage < this.totalPages) {
      this.currentPage = this.currentPage + 1;
      this.updatePaginatedHistories();
    }
  }

  // Navegar a la última página
  goToLastPage(): void {
    this.currentPage = this.totalPages;
    this.updatePaginatedHistories();
  }

  ngOnDestroy(): void {
    if (this.error$) {
      this.error$.unsubscribe();
    }

    if (this.routeQueryMap$) {
      this.error$.unsubscribe();
    }
  }

  throwError(error: string) {
    Swal.fire({
      title: error,
      icon: "error",
      confirmButtonText: "Aceptar"
    });
  }

}
