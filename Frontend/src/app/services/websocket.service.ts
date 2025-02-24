import { Injectable } from '@angular/core';
import { BehaviorSubject, Observable, Subject } from 'rxjs';
import { webSocket, WebSocketSubject } from 'rxjs/webSocket';
import { environment } from '../../environments/environment.development';
import { MsgType, WebSocketMessage } from '../models/web-socket-message';
import { User } from '../models/user';
import { GameRoom } from '../models/game-room';
import { GameInvitation } from '../models/game-invitation';
import { GameRoomAction, RoomAction } from '../models/game-room-action';
import Swal from 'sweetalert2';
import { GameService } from './game.service';

@Injectable({
  providedIn: 'root',
})
export class WebsocketService {

  constructor(private gameService: GameService) { }

  private readonly USER_KEY = 'user';
  rxjsSocket: WebSocketSubject<WebSocketMessage> | null = null;

  // Eventos de conexión
  public connected = new Subject<void>();
  public disconnected = new Subject<void>();
  public error = new Subject<void>();

  // Lista de amigos con sus estados
  private friendListSubject = new BehaviorSubject<User[]>([]);
  public friendList$ = this.friendListSubject.asObservable();

  // Datos de la sala de juego donde está el usuario
  private gameRoomSubject = new BehaviorSubject<GameRoom | null>(null);
  public gameRoom$: Observable<GameRoom | null> = this.gameRoomSubject.asObservable();

  // Recibir invitaciones a partidas
  private gameInvitationSubject = new BehaviorSubject<GameInvitation | null>(null);
  public gameInvitation$ = this.gameInvitationSubject.asObservable();

  // Recibir notificación de cancelación de la invitación
  public cancelGameInvitationSubject = new Subject<void>();

  // Recibir notificación de error de la invitación
  public errorGameInvitationSubject = new Subject<void>();

  // Recibir notificación de partida empezada
  public gameStartedSubject = new Subject<void>();

  // Recibir la cantidad total de jugadores conectados
  public totalPlayersSubject = new Subject<number>();

  // Recibir la cantidad de partidas activas en curso
  public activeGamesSubject = new Subject<number>();

  // Recibir la cantidad de jugadores actualmente en partidas
  public playersInGamesSubject = new Subject<number>();

  // Recibir notificación de solicitud de amistad
  public friendRequestSubject = new Subject<void>();

  // Recibir notificación de de la lista de amigos
  public friendsSubject = new Subject<void>();


  private onConnected() {
    this.connected.next();
  }

  private onMessageReceived(message: WebSocketMessage) {
    //console.log('Mensaje recibido:', message);

    // Según el tipo de mensaje
    switch (message.Type) {

      case MsgType.Connection:
        console.log(message.Content);
        break;

      case MsgType.StatsUpdate: // Actualizar estadísticas
        this.totalPlayersSubject.next(message.Content.TotalPlayers);
        this.activeGamesSubject.next(message.Content.ActiveGames);
        this.playersInGamesSubject.next(message.Content.PlayersInGames);
        break;

      case MsgType.FriendStatusUpdate: // Estado de los amigos
        let friendStatus: any;
        const content = message.Content;

        if (content) {
          if (Array.isArray(content)) {
            friendStatus = content;
          } else {
            friendStatus = [content]
          }
        } else {
          friendStatus = content;
        }

        this.updateFriendStatus(friendStatus);
        break;

      case MsgType.GameInvitation: // Invitaciones a partidas
        this.gameInvitationSubject.next(message.Content);
        break;

      case MsgType.CancelGameInvitation: // Cancelar una invitación a una partida
        const user: User = JSON.parse(localStorage.getItem(this.USER_KEY) || sessionStorage.getItem(this.USER_KEY));

        if (user.userId == message.Content.FromUserId) {
          this.cancelGameInvitationSubject.next();
        } else {
          this.gameInvitationSubject.next(null);
          this.errorGameInvitationSubject.next();
        }
        break;

      case MsgType.GameRoom: // Crear una sala o actualizarla
        this.updateGameRoom(message.Content);
        break;

      case MsgType.StartGame: // Iniciar la partida al usuario invitado
        this.gameStartedSubject.next();
        break;

      case MsgType.FriendListUpdate: // Actualización de la lista de amigos
        this.friendsSubject.next();
        break;

      case MsgType.FriendRequestUpdate: // Actualización de las solicitudes de amistad
        this.friendRequestSubject.next();
        break;

      // Mensajes redirigidos al servicio del juego
      case MsgType.GameStart:
      case MsgType.GameUpdate:
      case MsgType.GameOver:
      case MsgType.Chat:
      case MsgType.CancelRematchRequest:
        this.gameService.onMessageReceived(message);
        break;

      default:
        //console.warn("Mensaje no reconocido:", message.Type);
        break;
    }
  }

  // Acepta una invitación
  async acceptInvitation(user: User, invitation: GameInvitation) {
    this.gameInvitationSubject.next(null);

    const action: GameRoomAction = {
      Action: RoomAction.Friend,
      FriendId: invitation.FromUserId,
    };

    const message: WebSocketMessage = {
      Type: MsgType.GameRoom,
      Id: user.userId,
      Content: action,
    };

    this.sendRxjs(message);
  }

  // Rechaza una invitación
  async cancelInvitation(invitation: GameInvitation) {
    this.gameInvitationSubject.next(null);

    const cancelInvitation: GameInvitation = {
      FromUserId: invitation.FromUserId,
      ToUserId: invitation.ToUserId,
    };

    const message: WebSocketMessage = {
      Type: MsgType.CancelGameInvitation,
      Id: invitation.ToUserId,
      Content: cancelInvitation,
    };

    this.sendRxjs(message);
  }

  // Actualizar la información de la sala
  updateGameRoom(room: GameRoom): void {
    this.gameRoomSubject.next(room);
  }

  // Limpiar la información de la sala
  clearGameRoom(userId: number): void {
    this.gameRoomSubject.next(null);

    const action: GameRoomAction = {
      Action: RoomAction.CancelRoom,
      FriendId: null,
    };

    const message: WebSocketMessage = {
      Type: MsgType.GameRoom,
      Id: userId,
      Content: action,
    };

    this.sendRxjs(message);
  }

  // Actualizar la lista completa de amigos
  setFriendList(friends: User[]): void {
    this.friendListSubject.next(friends);
  }

  // Actualizar el estado de los amigos
  updateFriendStatus(friendStatus: any[]): void {
    let friends: User[] = this.friendListSubject.getValue();

    for (let friend of friends) {
      for (let status of friendStatus) {
        if (friend.userId === status.UserId) {
          friend.status = status.Status;
        }
      }
    }

    this.friendListSubject.next(friends);
  }

  private onError(error: any) {
    //console.error("Error en WebSocket:", error);

    Swal.fire({
      title: "Se ha perdido la conexión con el servidor",
      icon: "error",
      confirmButtonText: "Aceptar",
    });

    this.error.next();
  }

  private onDisconnected() {
    this.disconnected.next();
  }

  isConnectedRxjs(): boolean {
    return this.rxjsSocket != null && !this.rxjsSocket.closed;
  }

  connectRxjs(token: string, isAuthenticated: boolean): Promise<void> {
    return new Promise((resolve, reject) => {

      if (!this.isConnectedRxjs() && isAuthenticated) {
        const url = `${environment.socketUrl}?token=${token}`;

        this.rxjsSocket = webSocket({
          url: url,

          // Evento de apertura de conexión
          openObserver: {
            next: () => {
              this.onConnected();
              resolve();
            }
          },

          serializer: (value: WebSocketMessage) => JSON.stringify(value),
          deserializer: (event: MessageEvent) => JSON.parse(event.data),
        });

        this.rxjsSocket.subscribe({

          // Evento de mensaje recibido
          next: (message) => this.onMessageReceived(message),

          // Evento de error generado
          error: (error) => {
            this.onError(error);
            reject(error);
          },

          // Evento de cierre de conexión
          complete: () => this.onDisconnected(),
        });

      } else {
        resolve();
      }
    });
  }

  // Método para enviar mensajes
  async sendRxjs(message: WebSocketMessage) {

    if (this.isConnectedRxjs() && this.rxjsSocket) {
      this.rxjsSocket.next(message);
      //console.log("Mensaje enviado:", message);

    } else {
      Swal.fire({
        title: "No se ha podido conectar con el servidor",
        icon: "error",
        confirmButtonText: "Aceptar"
      });
    }
  }

  // Solicitar al servidor los datos necesarios
  initMenuData(userId: number): void {

    // Estado de los amigos
    const friendStatusMessage: WebSocketMessage = {
      Type: MsgType.FriendStatusUpdate,
      Id: userId,
      Content: { action: 'getFriendStatus' }
    };
    this.sendRxjs(friendStatusMessage);

    // Estadísticas del juego
    const statsMessage: WebSocketMessage = {
      Type: MsgType.StatsUpdate,
      Id: userId,
      Content: { action: 'getStats' }
    };
    this.sendRxjs(statsMessage);
  }

  disconnectRxjs(): void {
    if (this.rxjsSocket) {
      this.rxjsSocket.complete();
      this.rxjsSocket = null;
    }
  }
}