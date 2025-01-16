import { Component, OnInit } from '@angular/core';
import { Router } from '@angular/router';
import { AuthService } from '../../services/auth.service';

@Component({
  selector: 'app-menu',
  standalone: true,
  imports: [],
  templateUrl: './menu.component.html',
  styleUrl: './menu.component.css'
})
export class MenuComponent implements OnInit {

  private readonly USER_KEY = 'user';
  private readonly TOKEN_KEY = 'jwtToken';

  constructor(
    private router: Router,
    private authService: AuthService,
  ) { }

  ngOnInit(): void {
    if (!this.authService.isAuthenticated()) {
      this.router.navigate(['/']);
    }

    const user = this.authService.getUser();
    console.log("Usuario:", user);
  }

  logout(): void {
    this.authService.logout();
    alert("ok");
    this.router.navigate(['/']);
  }

}
