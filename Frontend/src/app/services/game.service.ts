import { Injectable } from '@angular/core';
import { MsgType, WebSocketMessage } from '../models/web-socket-message';
import { Subject } from 'rxjs';

@Injectable({
  providedIn: 'root'
})
export class GameService {

  // Notificar el comienzo de la partida
  public gameStartSubject = new Subject<any>();

  // Notificar actualizaciones de la partida
  public gameUpdateSubject = new Subject<any>();

  // Notificar finalización de la partida
  public gameOverSubject = new Subject<any>();

  // Notificar mensajes del chat
  public chatSubject = new Subject<string>();

  // Notificar cancelamiento de la revancha
  public cancelRequestRematchSubject = new Subject<void>();


  public onMessageReceived(message: WebSocketMessage) {

    // Según el tipo de mensaje
    switch (message.Type) {
      case MsgType.GameStart:
        this.gameStartSubject.next(message.Content);
        break;
      case MsgType.GameUpdate:
        this.gameUpdateSubject.next(message.Content);
        break;
      case MsgType.GameOver:
        this.gameOverSubject.next(message.Content);
        break;
      case MsgType.Chat:
        this.chatSubject.next(message.Content.Content);
        break;
      case MsgType.CancelRematchRequest:
        this.cancelRequestRematchSubject.next();
        break

      default:
        console.warn("Mensaje no reconocido:", message.Type);
        break;
    }
  }

}
