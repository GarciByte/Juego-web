import { Component, OnDestroy, OnInit } from '@angular/core';
import { Router, RouterModule } from '@angular/router';
import { AuthService } from '../../services/auth.service';
import { Subscription } from 'rxjs';
import { WebsocketService } from '../../services/websocket.service';
import { User } from '../../models/user';
import { UserService } from '../../services/user.service';
import Swal from 'sweetalert2';
import { CommonModule } from '@angular/common';
import { MsgType, WebSocketMessage } from '../../models/web-socket-message';

@Component({
  selector: 'app-admin-profile',
  imports: [RouterModule, CommonModule],
  templateUrl: './admin-profile.component.html',
  styleUrl: './admin-profile.component.css'
})
export class AdminProfileComponent implements OnInit, OnDestroy {

  user: User = null;
  allUsers: User[] = [];
  error$: Subscription;

  constructor(
    private router: Router,
    private authService: AuthService,
    private userService: UserService,
    private webSocketService: WebsocketService
  ) { }

  async ngOnInit(): Promise<void> {
    if (!this.authService.isAuthenticated()) {
      this.router.navigate(['/']);
    }

    this.user = this.authService.getUser();

    if (this.user.role != "Admin") {
      this.router.navigate(['/']);
    }

    // Obtener todos los usuarios
    await this.getAllUsers();

    // Error del WebSocket
    this.error$ = this.webSocketService.error.subscribe(() => {
      this.authService.logout();
      this.router.navigate(['/']);
    });
  }

  // Obtener todos los usuarios
  async getAllUsers(): Promise<void> {
    try {
      const result = await this.userService.getAllUsers();

      if (result.success) {
        this.allUsers = result.data;

      } else {
        this.throwError("Se ha producido un error al obtener todos los usuarios");
      }

    } catch (error) {
      //console.error("Error al obtener todos los usuarios:", error);
      this.throwError("Se ha producido un error al obtener todos los usuarios");
    }
  }

  // Editar el rol de un usuario
  async modifyUserRole(user: User) {
    try {
      let role = user.role;

      if (role === "Admin") {
        role = "User";
      } else {
        role = "Admin";
      }

      const result = await this.userService.modifyRole(user.userId, role);

      if (result.success) {

      } else {
        console.error("Error al modificar el rol:", result.error);
        this.throwError("Se ha producido un error al modificar el rol del usuario");
      }

    } catch (error) {
      console.error("Error al modificar el rol:", error);
      this.throwError("Se ha producido un error al modificar el rol del usuario");
    }

    this.getAllUsers();
  }

  // Editar la prohibición de un usuario
  async modifyUserBan(user: User) {
    Swal.fire({
      title: `¿Estás seguro de que quieres modificar la prohibición de ${user.nickname}?`,
      icon: "warning",
      showCancelButton: true,
      confirmButtonText: "Sí",
      cancelButtonText: "No"
    }).then(async (result) => {
      if (result.isConfirmed) {
        try {
          let isBanned = user.isBanned;

          if (isBanned) {
            isBanned = false;
          } else {
            isBanned = true;
          }

          const result = await this.userService.modifyBan(user.userId, isBanned);

          if (result.success) {

            Swal.fire({
              title: `Se ha modificado la prohibición de ${user.nickname}`,
              icon: 'success',
              showConfirmButton: false,
              timer: 2000,
              timerProgressBar: true
            });

            if (isBanned) {

              const message: WebSocketMessage = {
                Type: MsgType.UserBanned,
                Id: this.user.userId,
                Content: user.userId,
              };

              this.webSocketService.sendRxjs(message);
            }

          } else {
            console.error("Error al modificar la prohibición:", result.error);
            this.throwError("Se ha producido un error al modificar la prohibición del usuario");
          }

        } catch (error) {
          console.error("Error al modificar la prohibición:", error);
          this.throwError("Se ha producido un error al modificar la prohibición del usuario");
        }

        this.getAllUsers();

      } else {
        Swal.close();
      }
    });
  }

  ngOnDestroy(): void {
    if (this.error$) {
      this.error$.unsubscribe();
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
