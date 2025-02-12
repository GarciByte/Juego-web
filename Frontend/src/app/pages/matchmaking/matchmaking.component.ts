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
  routeSubscription: Subscription;
  roomSubscription: Subscription;

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
      this.router.navigate(['/login']);
    }

    this.user = this.authService.getUser();
    this.user.avatar.url = this.IMG_URL + this.user.avatar.url;
    this.hostPlayer = this.user;

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
      }
    });

    // Solicitar los datos de la sala
    this.roomSubscription = this.webSocketService.gameRoom$.subscribe(async (room) => {
      if (room) {
        this.gameRoom = room;

        if (room.RoomType === GameRoomType.Bot) {
          this.canStartGame = true;
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
        }

        console.log("Sala actualizada:", this.gameRoom);
        console.log("hostPlayer: ", this.hostPlayer);
        console.log("guestPlayer: ", this.guestPlayer);
      }
    });

    // Desconexión del usuario
    this.disconnected$ = this.webSocketService.disconnected.subscribe(() => {
      console.error("Desconectado del WebSocket");
    });

    // Lista de amigos con sus estados
    this.friendListSubscription = this.webSocketService.friendList$.subscribe((friends) => {
      this.friends = friends;
    });

    console.log(this.gameStarted);
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
      alert("Ya tienes una sala creada: " + this.gameRoom.RoomType);
    }

  }

  // Buscar un oponente aleatorio
  playRandom(): void {
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
      alert("Ya tienes una sala creada: " + this.gameRoom.RoomType);
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
        alert("Se ha enviado la invitación.");

      } else {
        console.error("No se seleccionó un ID de amigo para invitar:");
      }
    } else {
      alert("Ya has invitado a un amigo");
    }
  }

  // Iniciar la partida
  startGame(): void {
    this.gameStarted = true;
    console.log("Partida iniciada");
  }

  // Regresar al menú
  leaveMatchmaking(): void { 
    this.router.navigate(['/menu']);
  }

  // Elimina la sala si el usuario la abandona
  clearGameRoom(): void {
    this.cancelRandomSearch()
    this.webSocketService.clearGameRoom(this.user.userId);
  }

  ngOnDestroy(): void {
    if (!this.gameStarted) {
      this.clearGameRoom();
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
  }
}
