import { Component, OnDestroy, OnInit } from '@angular/core';
import { Router } from '@angular/router';
import { Subscription } from 'rxjs';
import { AuthService } from '../../services/auth.service';
import { WebsocketService } from '../../services/websocket.service';
import { User } from '../../models/user';
import { GameRoom } from '../../models/game-room';
import { UserService } from '../../services/user.service';
import Swal from 'sweetalert2';
import { GameService } from '../../services/game.service';
import { ChatMessage } from '../../models/chat-message';
import { WebSocketMessage, MsgType } from '../../models/web-socket-message';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { MemoryGameMove } from '../../models/memory-game-move';
import { environment } from '../../../environments/environment.development';

@Component({
  selector: 'app-game',
  imports: [FormsModule, CommonModule],
  templateUrl: './game.component.html',
  styleUrl: './game.component.css'
})
export class GameComponent implements OnInit, OnDestroy {

  public readonly IMG_URL = environment.apiImg;
  user: User;
  opponent: User | null = null;
  gameRoom: GameRoom;

  // Suscripciones a los datos necesarios
  disconnected$: Subscription;
  error$: Subscription;
  roomSubscription: Subscription;
  gameStartSubscription: Subscription;
  gameUpdateSubscription: Subscription;
  gameOverSubscription: Subscription;
  chatSubscription: Subscription;
  cancelRequestRematch: Subscription;

  // Variables del juego
  board: any[] = [];
  currentTurnUserId: number;
  chatMessages: string[] = [];
  chatInput: string = "";
  gameOverData: any;
  showError: Boolean = true;
  gameFinished: Boolean = false;

  // Temporizador del turno
  turnTimer: number = 60;
  turnTimerInterval: any;

  constructor(
    private router: Router,
    private authService: AuthService,
    private webSocketService: WebsocketService,
    private userService: UserService,
    private gameService: GameService
  ) { }

  async ngOnInit(): Promise<void> {
    if (!this.authService.isAuthenticated()) {
      this.router.navigate(['/']);
    }

    this.startTurnTimer();

    // Inicio de la partida
    this.gameStartSubscription = this.gameService.gameStartSubject.subscribe((content: any) => {
      this.board = content.Board;
      this.currentTurnUserId = content.CurrentTurnUserId;
      this.gameFinished = false;
      this.resetTurnTimer();
    });

    this.user = this.authService.getUser();
    this.showError = true;

    // Solicitar los datos de la sala
    this.roomSubscription = this.webSocketService.gameRoom$.subscribe(async (room) => {
      if (room) {
        this.gameRoom = room;

        if (room.GuestUserId != null) {
          let opponentId: number;
          if (this.user.userId === room.HostUserId) {
            opponentId = room.GuestUserId
          } else {
            opponentId = room.HostUserId
          }

          const result = await this.userService.getUserById(opponentId);
          if (result.success) {
            this.opponent = result.data;
            this.opponent.avatar.url = this.opponent.avatar.url;
          } else {
            console.error("Error al obtener datos del oponente:", result.error);
          }
        }

      } else {
        if (this.showError) {
          Swal.fire({
            title: "Se ha perdido la conexión con la partida",
            icon: "error",
            confirmButtonText: "Aceptar"
          });
          this.leaveGame();
        }
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

    // Actualizaciones de la partida
    this.gameUpdateSubscription = this.gameService.gameUpdateSubject.subscribe((content: any) => {
      this.board = content.Board;
      this.currentTurnUserId = content.CurrentTurnUserId;
      this.resetTurnTimer();
    });

    // Finalización de la partida
    this.gameOverSubscription = this.gameService.gameOverSubject.subscribe((content: any) => {
      this.gameOverData = content;
      this.gameFinished = true;
      this.showGameOverModal();
    });

    // Mensajes del chat
    this.chatSubscription = this.gameService.chatSubject.subscribe((message: string) => {
      this.chatMessages.push(message);
    });

    // Cancelar la revancha
    this.cancelRequestRematch = this.gameService.cancelRequestRematchSubject.subscribe(() => {
      Swal.fire({
        title: this.opponent.nickname + " no ha aceptado la revancha",
        icon: "info",
        confirmButtonText: "Aceptar"
      });
      this.leaveGame();
    });
  }

  // Manejar la carta seleccionada
  onCardClick(card: any): void {
    if (this.currentTurnUserId === this.user.userId && !this.gameFinished) {

      // Enviar el movimiento al servidor
      const move: MemoryGameMove = {
        CardId: card.CardId,
        RoomId: this.gameRoom.RoomId
      };

      const message: WebSocketMessage = {
        Type: MsgType.GameUpdate,
        Id: this.user.userId,
        Content: move
      };

      this.webSocketService.sendRxjs(message);
    }
  }

  // Obtener la URL de la imagen de la carta
  getCardImage(value: number): string {
    let imageUrl = '';
    switch (value) {
      case 1:
        imageUrl = 'game/1.png';
        break;
      case 2:
        imageUrl = 'game/2.png';
        break;
      case 3:
        imageUrl = 'game/3.png';
        break;
      case 4:
        imageUrl = 'game/4.png';
        break;
      case 5:
        imageUrl = 'game/5.png';
        break;
      case 6:
        imageUrl = 'game/6.png';
        break;
      case 7:
        imageUrl = 'game/7.png';
        break;
      case 8:
        imageUrl = 'game/8.png';
        break;
    }
    return imageUrl;
  }

  // Envía mensajes del chat
  sendChat(): void {
    if (!(this.chatInput.trim() === "")) {

      let opponentId: number = 0;
      if (this.opponent) {
        opponentId = this.opponent.userId;
      }

      const chatMessage: ChatMessage = {
        UserId: this.user.userId,
        Nickname: this.user.nickname,
        FriendId: opponentId,
        Content: this.chatInput
      };

      const message: WebSocketMessage = {
        Type: MsgType.Chat,
        Id: this.user.userId,
        Content: chatMessage
      };

      this.webSocketService.sendRxjs(message);
      this.chatInput = "";
    }
  }

  // Regresar al menú
  leaveGame(): void {
    this.showError = false;
    this.router.navigate(['/menu']);
  }

  // Muestra el modal de final de partida
  showGameOverModal(): void {
    const history = this.gameOverData.Result;

    let htmlContent: string = "";
    htmlContent += "<p>Puntuación: " + history.Score + " puntos</p>";
    htmlContent += "<p>Resultado: " + history.Result + "</p>";
    htmlContent += "<p>Duración: " + history.Duration + "</p>";

    Swal.fire({
      title: "Fin de la partida",
      html: htmlContent,
      icon: "info",
      showCancelButton: true,
      confirmButtonText: "Solicitar una Revancha",
      cancelButtonText: "Volver al Menú"
    }).then((result) => {
      if (result.isConfirmed) {
        this.requestRematch();
      } else {
        this.leaveGame();
      }
    });
  }

  // Enviar solicitud de revancha al servidor
  requestRematch(): void {
    this.stopTurnTimer();

    const message: WebSocketMessage = {
      Type: MsgType.RematchRequest,
      Id: this.user.userId,
      Content: this.gameRoom
    };

    this.webSocketService.sendRxjs(message);
  }

  // Elimina la sala si el usuario la abandona
  clearGameRoom(): void {
    if (this.user) {
      this.webSocketService.clearGameRoom(this.user.userId);
    }
  }

  // Iniciar el timer
  startTurnTimer(): void {
    this.turnTimer = 60;
    this.turnTimerInterval = setInterval(() => {
      if (this.turnTimer > 0) {
        this.turnTimer--;
      } else {
        clearInterval(this.turnTimerInterval);
      }
    }, 1000);
  }

  // Para el timer
  stopTurnTimer(): void {
    if (this.turnTimerInterval) {
      clearInterval(this.turnTimerInterval);
    }
  }

  // Reinicia el timer
  resetTurnTimer(): void {
    if (this.turnTimerInterval) {
      clearInterval(this.turnTimerInterval);
    }
    this.startTurnTimer();
  }

  // Formatear el tiempo del temporizador
  formatTime(seconds: number): string {
    const minutes = Math.floor(seconds / 60);
    const secs = seconds % 60;

    let minutesStr = minutes.toString();
    let secsStr = secs.toString();

    if (minutes < 10) {
      minutesStr = '0' + minutesStr;
    }

    if (secs < 10) {
      secsStr = '0' + secsStr;
    }

    return minutesStr + ':' + secsStr;
  }

  ngOnDestroy(): void {
    this.clearGameRoom();

    if (this.turnTimerInterval) {
      clearInterval(this.turnTimerInterval);
    }

    if (this.disconnected$) {
      this.disconnected$.unsubscribe();
    }

    if (this.error$) {
      this.error$.unsubscribe();
    }

    if (this.roomSubscription) {
      this.roomSubscription.unsubscribe();
    }

    if (this.gameStartSubscription) {
      this.gameStartSubscription.unsubscribe();
    }

    if (this.gameUpdateSubscription) {
      this.gameUpdateSubscription.unsubscribe();
    }

    if (this.gameOverSubscription) {
      this.gameOverSubscription.unsubscribe();
    }

    if (this.chatSubscription) {
      this.chatSubscription.unsubscribe();
    }

    if (this.cancelRequestRematch) {
      this.cancelRequestRematch.unsubscribe();
    }
  }
}