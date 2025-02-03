import { Component, OnInit } from '@angular/core';
import { Router, RouterModule } from '@angular/router';
import { WebsocketService } from '../../services/websocket.service';
import { AuthService } from '../../services/auth.service';

@Component({
  selector: 'app-matchmaking',
  standalone: true,
  imports: [RouterModule],
  templateUrl: './matchmaking.component.html',
  styleUrl: './matchmaking.component.css'
})
export class MatchmakingComponent implements OnInit {

  constructor(
    private router: Router,
    private authService: AuthService,
    private webSocketService: WebsocketService
  ) { }

  ngOnInit(): void {
    if (!this.authService.isAuthenticated()) {
      this.router.navigate(['/login']);
    }

    const isConnected = this.webSocketService.isConnectedRxjs();
    console.log(isConnected);
  }

}
