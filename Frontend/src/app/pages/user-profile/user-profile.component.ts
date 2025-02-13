import { Component, OnInit } from '@angular/core';
import { Router, RouterModule } from '@angular/router';
import { environment } from '../../../environments/environment.development';
import { User } from '../../models/user';
import { AuthService } from '../../services/auth.service';
import { Subscription } from 'rxjs';
import { WebsocketService } from '../../services/websocket.service';

@Component({
  selector: 'app-user-profile',
  standalone: true,
  imports: [RouterModule],
  templateUrl: './user-profile.component.html',
  styleUrl: './user-profile.component.css'
})
export class UserProfileComponent implements OnInit {

  public readonly IMG_URL = environment.apiImg;
  user: User = null;
  avatarUrl: string = null;

  // Suscripciones generales
  error$: Subscription;

  constructor(
    private router: Router,
    private authService: AuthService,
    private webSocketService: WebsocketService
  ) { }

  ngOnInit(): void {
    if (!this.authService.isAuthenticated()) {
      this.router.navigate(['/']);
    }

    // Error del WebSocket
    this.error$ = this.webSocketService.error.subscribe(() => {
      this.authService.logout();
      this.router.navigate(['/']);
    });

    this.user = this.authService.getUser();
    this.avatarUrl = this.IMG_URL + this.user.avatar.url;

    console.log(this.user);
    console.log(this.avatarUrl);
  }

}
