import { Component, OnDestroy, OnInit } from '@angular/core';
import { ActivatedRoute, Router, RouterModule } from '@angular/router';
import { WebsocketService } from '../../services/websocket.service';
import { AuthService } from '../../services/auth.service';
import { User } from '../../models/user';
import { environment } from '../../../environments/environment.development';
import { Subscription } from 'rxjs';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { GameRoom, GameRoomType } from '../../models/game-room';
import { GameRoomAction, RoomAction } from '../../models/game-room-action';
import { WebSocketMessage, MsgType } from '../../models/web-socket-message';
import { FriendRequestService } from '../../services/friend-request.service';
import { UserService } from '../../services/user.service';
import { GameInvitation } from '../../models/game-invitation';
import Swal from 'sweetalert2';

@Component({
  selector: 'app-matchmaking',
  standalone: true,
  imports: [FormsModule, CommonModule, RouterModule],
  templateUrl: './matchmaking.component.html',
  styleUrl: './matchmaking.component.css',
})
export class MatchmakingComponent implements OnInit, OnDestroy {
  public readonly IMG_URL = environment.apiImg;
  user: User;
  friends: User[] = [];

  invitation: GameInvitation | null = null; // Invitación de partida enviada

  hostPlayer: User; // El anfitrión
  guestPlayer: User | null = null; // El invitado

  isHost: boolean = true; // Si es anfitrión
  invitationReceived: boolean = false; // Si se accedió con una invitación
  invitationSent: boolean = false; // Si se envió una invitación
  isSearching: boolean = false; // Si se está buscando un oponente aleatorio

  // Sala de juego
  gameRoom: GameRoom | null = null;

  // ID del amigo invitado
  selectedFriendId: number | null = null;

  // Si se puede iniciar la partida
  canStartGame: boolean = false;

  // Si se inició la partida
  gameStarted: boolean = false;

  // Suscripciones a los datos necesarios
  friendListSubscription: Subscription;
  connected$: Subscription;
  disconnected$: Subscription;
  error$: Subscription;
  routeSubscription: Subscription;
  roomSubscription: Subscription;
  gameStartedSubscription: Subscription;
  cancelGameInvitationSubject: Subscription;
  errorGameInvitationSubject: Subscription;

  constructor(
    private router: Router,
    private route: ActivatedRoute,
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
    this.user.avatar.url = this.IMG_URL + this.user.avatar.url;
    this.hostPlayer = this.user;
    this.invitationSent = false;

    // Cargar la lista de amigos
    await this.loadFriends();

    // Solicitar los datos necesarios
    if (this.webSocketService.isConnectedRxjs()) {
      this.webSocketService.initMenuData(this.user.userId);
    } else {
      this.connected$ = this.webSocketService.connected.subscribe(() => {
        this.webSocketService.initMenuData(this.user.userId);
      });
    }

    // Comprobar parámetros de ruta para saber si se accedió mediante una invitación
    this.routeSubscription = this.route.queryParams.subscribe(async (params) => {
      if (params['userId']) {
        const userId = Number(params['userId']);
        await this.handleInvitation(userId);
      } else {
        this.webSocketService.clearGameRoom(this.user.userId);
      }
    });

    // Solicitar los datos de la sala
    this.roomSubscription = this.webSocketService.gameRoom$.subscribe(async (room) => {
      if (room) {
        this.gameRoom = room;

        if (room.RoomType === GameRoomType.Bot) {
          this.canStartGame = true;
          this.startGame()
        }

        if (room.RoomType === GameRoomType.Friend && room.GuestUserId !== null && room.GuestUserId !== undefined) {
          const result = await this.userService.getUserById(room.GuestUserId);
          if (result.success) {
            this.guestPlayer = result.data;
            this.canStartGame = true;
          } else {
            console.error("Se ha producido un error", result.error);
          }
        }

        if (room.RoomType === GameRoomType.Random && room.GuestUserId !== null && room.GuestUserId !== undefined) {
          if (this.user.userId === room.HostUserId) {
            this.invitationReceived = false;
            this.isHost = true;
            this.hostPlayer = this.user;

            const guestUserId = await this.userService.getUserById(room.GuestUserId);

            if (guestUserId.success) {
              this.guestPlayer = guestUserId.data;
              this.canStartGame = true;

            } else {
              console.error("Se ha producido un error", guestUserId.error);
            }

          } else {
            this.invitationReceived = false;
            this.isHost = false;
            this.guestPlayer = this.user;
            this.guestPlayer.avatar.url = this.user.avatar.url;

            const hostUserId = await this.userService.getUserById(room.HostUserId);

            if (hostUserId.success) {
              this.hostPlayer = hostUserId.data;
              this.canStartGame = true;

            } else {
              console.error("Se ha producido un error", hostUserId.error);
            }
          }
          this.startGame();
        }

        this.isSearching = false;
        console.log("Sala actualizada:", this.gameRoom);
        console.log("hostPlayer: ", this.hostPlayer);
        console.log("guestPlayer: ", this.guestPlayer);
      }
    });

    // Desconexión del usuario
    this.disconnected$ = this.webSocketService.disconnected.subscribe(() => {
      console.error("Desconectado del WebSocket");
    });

    // Error del WebSocket
    this.error$ = this.webSocketService.error.subscribe(() => {
      this.authService.logout();
      this.router.navigate(['/']);
    });

    // Lista de amigos con sus estados
    this.friendListSubscription = this.webSocketService.friendList$.subscribe((friends) => {
      this.friends = friends;
    });

    // Comprueba si el anfitrión ha empezado la partida
    this.gameStartedSubscription = this.webSocketService.gameStartedSubject.subscribe(() => {
      this.gameStarted = true;
      this.router.navigate(['/game']);
    });

    // Cancela la invitación a una partida
    this.cancelGameInvitationSubject = this.webSocketService.cancelGameInvitationSubject.subscribe(() => {

      Swal.fire({
        title: "La invitación ha sido rechazada",
        icon: "info",
        confirmButtonText: "Aceptar"
      });

      this.invitationSent = false;
      this.invitation = null;
    });

    // Notifica al usuario invitado en caso de abandono de la sala del anfitrión
    this.errorGameInvitationSubject = this.webSocketService.errorGameInvitation$.subscribe(async (message: WebSocketMessage) => {
      if (message) {
        await this.webSocketService.errorGameInvitation();
        this.router.navigate(['/menu']);
      }
    });
  }

  // Invitaciones a partidas
  async handleInvitation(userId: number): Promise<void> {
    const result = await this.userService.getUserById(userId);
    if (result.success) {
      this.hostPlayer = result.data
      this.guestPlayer = this.user;
      this.invitationReceived = true;
      this.isHost = false;

    } else {
      console.error("Se ha producido un error", result.error);
    }
  }

  // Carga la lista de amigos
  async loadFriends(): Promise<void> {
    try {
      const result = await this.friendRequestService.getFriends(this.user.userId);
      this.webSocketService.setFriendList(result.data);
      this.friends = result.data;

    } catch (error) {
      console.error('Error al cargar amigos:', error);
    }
  }

  // Jugar contra un bot
  playAgainstBot(): void {
    if (!(this.invitationSent)) {
      if (!(this.gameRoom)) {
        const action: GameRoomAction = {
          Action: RoomAction.Bot,
          FriendId: null,
        };

        const message: WebSocketMessage = {
          Type: MsgType.GameRoom,
          Id: this.user.userId,
          Content: action,
        };

        this.webSocketService.sendRxjs(message);
      } else {
        this.throwError("Ya tienes una sala creada: " + this.gameRoom.RoomType);
      }
    } else {
      this.throwError("Ya has enviado una invitación a un amigo");
    }
  }

  // Buscar un oponente aleatorio
  playRandom(): void {
    if (!(this.invitationSent)) {
      if (!(this.gameRoom)) {
        this.isSearching = true;

        const action: GameRoomAction = {
          Action: RoomAction.Random,
          FriendId: null,
        };

        const message: WebSocketMessage = {
          Type: MsgType.GameRoom,
          Id: this.user.userId,
          Content: action,
        };

        this.webSocketService.sendRxjs(message);
      } else {
        this.throwError("Ya tienes una sala creada: " + this.gameRoom.RoomType);
      }
    } else {
      this.throwError("Ya has enviado una invitación a un amigo");
    }
  }

  // Cancelar la búsqueda aleatoria
  cancelRandomSearch(): void {
    if (this.isSearching) {
      this.isSearching = false;

      const action: GameRoomAction = {
        Action: RoomAction.CancelRandom,
        FriendId: null,
      };

      const message: WebSocketMessage = {
        Type: MsgType.GameRoom,
        Id: this.user.userId,
        Content: action,
      };

      this.webSocketService.sendRxjs(message);
    }
  }

  // Cancelar la invitación a la partida
  async cancelInvitation(invitation: GameInvitation) {
    this.webSocketService.cancelInvitation(invitation);
  }

  // Invitar a un amigo
  inviteFriend(): void {
    if (!(this.invitationSent)) {
      if (!(this.selectedFriendId === null)) {

        const invitation: GameInvitation = {
          FromUserId: this.user.userId,
          ToUserId: Number(this.selectedFriendId),
        };

        const message: WebSocketMessage = {
          Type: MsgType.GameInvitation,
          Id: this.user.userId,
          Content: invitation,
        };

        this.webSocketService.sendRxjs(message);
        this.invitationSent = true;
        this.invitation = invitation;

        Swal.fire({
          title: "Se ha enviado la invitación",
          icon: 'success',
          showConfirmButton: false,
          timer: 2000,
          timerProgressBar: true
        });

      } else {
        this.throwError("No has seleccionado un amigo para invitar");
      }
    } else {
      this.throwError("Ya has enviado una invitación a un amigo");
    }
  }

  // Iniciar la partida
  startGame(): void {
    this.gameStarted = true;

    if (this.canStartGame && this.gameStarted && this.isHost) {

      let friendId = null
      if (this.gameRoom.GuestUserId != null) {
        friendId = this.gameRoom.GuestUserId
      }

      const action: GameRoomAction = {
        Action: RoomAction.StartGame,
        FriendId: friendId,
      };

      const message: WebSocketMessage = {
        Type: MsgType.GameRoom,
        Id: this.user.userId,
        Content: action,
      };
      this.webSocketService.sendRxjs(message);
    }
    this.router.navigate(['/game']);
  }

  // Regresar al menú
  leaveMatchmaking(): void {
    this.router.navigate(['/menu']);
  }

  // Elimina la sala si el usuario la abandona
  clearGameRoom(): void {
    if (this.user) {
      this.cancelRandomSearch()
      this.webSocketService.clearGameRoom(this.user.userId);
    }
  }

  ngOnDestroy(): void {
    if (this.invitation) {
      this.cancelInvitation(this.invitation);
    }

    if (!this.gameStarted) {
      this.clearGameRoom();
    }

    if (this.gameStartedSubscription) {
      this.gameStartedSubscription.unsubscribe();
    }

    if (this.friendListSubscription) {
      this.friendListSubscription.unsubscribe();
    }

    if (this.connected$) {
      this.connected$.unsubscribe();
    }

    if (this.disconnected$) {
      this.disconnected$.unsubscribe();
    }

    if (this.routeSubscription) {
      this.routeSubscription.unsubscribe();
    }

    if (this.roomSubscription) {
      this.roomSubscription.unsubscribe();
    }

    if (this.error$) {
      this.error$.unsubscribe();
    }

    if (this.cancelGameInvitationSubject) {
      this.cancelGameInvitationSubject.unsubscribe();
    }

    if (this.errorGameInvitationSubject) {
      this.errorGameInvitationSubject.unsubscribe();
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
