import { Component, OnDestroy, OnInit } from '@angular/core';
import { Router } from '@angular/router';
import { Subscription } from 'rxjs';
import { AuthService } from '../../services/auth.service';
import { WebsocketService } from '../../services/websocket.service';
import { User } from '../../models/user';
import { GameRoom, GameRoomType } from '../../models/game-room';
import { UserService } from '../../services/user.service';

@Component({
  selector: 'app-game',
  imports: [],
  templateUrl: './game.component.html',
  styleUrl: './game.component.css'
})
export class GameComponent implements OnInit, OnDestroy {

  user: User;
  opponent: User | null = null;
  gameRoom: GameRoom;

  // Suscripciones a los datos necesarios
  connected$: Subscription;
  disconnected$: Subscription;
  error$: Subscription;
  routeSubscription: Subscription;
  roomSubscription: Subscription;

  constructor(
    private router: Router,
    private authService: AuthService,
    private webSocketService: WebsocketService,
    private userService: UserService
  ) { }

  async ngOnInit(): Promise<void> {
    if (!this.authService.isAuthenticated()) {
      this.router.navigate(['/']);
    }

    this.user = this.authService.getUser();

    // Solicitar los datos de la sala
    this.roomSubscription = this.webSocketService.gameRoom$.subscribe(async (room) => {
      if (room) {
        this.gameRoom = room;

        if (room.GuestUserId != null) {
          console.log("Oponente:", room.GuestUserId);
        }

        console.log("Sala actualizada:", this.gameRoom);
      } else {
        console.error("No hay una sala guardada.");
        this.leaveMatchmaking();
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

    console.log(this.user);

    if (this.opponent) {
      console.log("Oponente:", this.opponent);
    }
  }

  // Regresar al menú
  leaveMatchmaking(): void { 
    this.router.navigate(['/menu']);
  }

  // Elimina la sala si el usuario la abandona
  clearGameRoom(): void {
    if (this.user) {
      this.webSocketService.clearGameRoom(this.user.userId);
    }
  }

  ngOnDestroy(): void {
    this.clearGameRoom();
    
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
