import { CommonModule } from '@angular/common';
import { Component, OnInit } from '@angular/core';
import { Router } from '@angular/router';
import { ButtonModule } from 'primeng/button';
import { AuthService } from '../../services/auth.service';
import { WebsocketService } from '../../services/websocket.service';


@Component({
  selector: 'app-home',
  standalone: true,
  imports: [CommonModule, ButtonModule],
  templateUrl: './home.component.html',
  styleUrl: './home.component.css'
})
export class HomeComponent implements OnInit {

  constructor(
    private router: Router,
    private authService: AuthService,
    private websocketService: WebsocketService
  ) { }

  ngOnInit(): void {
    if (this.authService.isAuthenticated() && this.websocketService.isConnectedRxjs()) {
      this.router.navigate(['/menu']);
    }
  }
  
}
