import { Component, OnInit } from '@angular/core';
import { Router } from '@angular/router';
import { AuthService } from '../../services/auth.service';
import Swal from 'sweetalert2';
import { User } from '../../models/user';
import { environment } from '../../../environments/environment.development';

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
  public readonly IMG_URL = environment.apiImg;
  user: User = null;
  avatarUrl: string = null;

  constructor(
    private router: Router,
    private authService: AuthService,
  ) { }

  ngOnInit(): void {
    if (!this.authService.isAuthenticated()) {
      this.router.navigate(['/']);
    }

    this.user = this.authService.getUser();
    this.avatarUrl = this.IMG_URL + this.user.avatar.url;
    console.log("Usuario:", this.user);
  }

  logout(): void {
    Swal.fire({
      title: "Has cerrado sesión con éxito",
      text: `¡Hasta pronto ${this.user.nickname}!`,
      icon: 'success',
      showConfirmButton: false,
      timer: 3000,
      timerProgressBar: true,
      didClose: () => {
        this.authService.logout(),
        this.router.navigate(['/login']);
      }
    });
  }

}
