import { Injectable } from '@angular/core';
import { BehaviorSubject, Subject } from 'rxjs';
import { webSocket, WebSocketSubject } from 'rxjs/webSocket';
import { environment } from '../../environments/environment.development';
import { WebSocketMessage } from '../models/web-socket-message';

@Injectable({
  providedIn: 'root',
})
export class WebsocketService {

  rxjsSocket: WebSocketSubject<WebSocketMessage> | null = null;

  // Eventos
  connected = new Subject<void>();
  messageReceived = new Subject<WebSocketMessage>();
  disconnected = new Subject<void>();

  private onConnected() {
    //console.log('WebSocket conectado');
    this.connected.next();
  }

  private onMessageReceived(message: WebSocketMessage) {
    //console.log('Mensaje recibido en el servicio:', message);
    this.messageReceived.next(message);
  }

  private onError(error: WebSocketMessage) {
    console.error('Error:', error);
  }

  private onDisconnected() {
    console.log('WebSocket desconectado');
    this.disconnected.next();
  }

  isConnectedRxjs(): boolean {
    return this.rxjsSocket != null && !this.rxjsSocket.closed;
  }

  connectRxjs(token: string, isAuthenticated: boolean): Promise<void> {
    return new Promise((resolve, reject) => {

      if (!this.isConnectedRxjs() && isAuthenticated) {
        //console.log("Conectando con el webSocket...");

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
        console.error('El WebSocket ya está conectado.');
        resolve();
      }
    });
  }

  // Método para enviar mensajes
  sendRxjs(message: WebSocketMessage) {
    if (this.isConnectedRxjs()) {
      this.rxjsSocket.next(message);
      console.log('Mensaje enviado:', message);
    } else {
      console.error("No hay una conexión activa para enviar el mensaje.");
    }
  }

  disconnectRxjs(): void {
    if (this.rxjsSocket) {
      this.rxjsSocket.complete();
      this.rxjsSocket = null;
    }
  }
}
