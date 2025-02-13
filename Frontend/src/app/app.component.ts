import { Component, OnDestroy, OnInit } from '@angular/core';
import { RouterModule, RouterOutlet } from '@angular/router';
import { ButtonModule } from 'primeng/button';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { WebsocketService } from './services/websocket.service';

@Component({
  selector: 'app-root',
  standalone: true,
  imports: [RouterOutlet, ButtonModule, RouterModule, FormsModule, CommonModule],
  templateUrl: './app.component.html',
  styleUrl: './app.component.css'
})
export class AppComponent implements OnDestroy {
  title = 'Juego de Memoria';

  constructor(private websocketService: WebsocketService) { }

  ngOnDestroy(): void {
    // Cierre de la conexión del Websocket
    if (this.websocketService.isConnectedRxjs()) {
      this.websocketService.disconnectRxjs();
    }
  }
}