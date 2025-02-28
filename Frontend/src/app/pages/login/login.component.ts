import { Component, OnInit } from '@angular/core';
import { Router, RouterModule } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { AuthService } from '../../services/auth.service';
import { CheckboxModule } from 'primeng/checkbox';
import { CardModule } from 'primeng/card';
import { PasswordModule } from 'primeng/password';
import { InputTextModule } from 'primeng/inputtext';
import Swal from 'sweetalert2';
import { WebsocketService } from '../../services/websocket.service';

@Component({
  selector: 'app-login',
  standalone: true,
  imports: [FormsModule, RouterModule, CheckboxModule, InputTextModule, CardModule, PasswordModule],
  templateUrl: './login.component.html',
  styleUrls: ['./login.component.css'],
})
export class LoginComponent implements OnInit {

  nickname: string = '';
  email: string = '';
  password: string = '';
  rememberMe: boolean = false;
  jwt: string = '';

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

  async submit() {

    // Verifica si es un email o nickname
    if (this.email.includes('@')) {
      this.nickname = '';
    } else {
      this.nickname = this.email;
      this.email = '';
    }

    const authData = { nickname: this.nickname, email: this.email, password: this.password };
    const result = await this.authService.login(authData, this.rememberMe);

    if (result.success) {
      this.jwt = result.data.accessToken;

      if (this.rememberMe) {
        localStorage.setItem('jwtToken', this.jwt);
      }

      const user = this.authService.getUser();
      const nickname = user ? user.nickname : null;

      Swal.fire({
        title: "Inicio de sesión con éxito",
        text: `¡Hola, ${nickname}!`,
        icon: 'success',
        showConfirmButton: false,
        timer: 2000,
        timerProgressBar: true,
      });

      if (this.websocketService.isConnectedRxjs()) {
        this.router.navigate(['/menu'])
      } else {
        this.throwError("Se ha producido un error en la conexión con el servidor");
      }

    } else {

      // Prohibición del usuario
      if (result.statusCode === 403) {
        this.throwError("Tu cuenta ha sido suspendida");

      } else {
        //console.error(result.error);
        this.throwError("El nombre de usuario o la contraseña son incorrectos");
      }
    }
  }

  throwError(error: string) {
    Swal.fire({
      title: error,
      icon: "error",
      confirmButtonText: "Aceptar"
    });
  }
}