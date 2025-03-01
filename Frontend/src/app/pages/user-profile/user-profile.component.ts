import { Component, OnDestroy, OnInit } from '@angular/core';
import { Router, RouterModule } from '@angular/router';
import { environment } from '../../../environments/environment';
import { User } from '../../models/user';
import { AuthService } from '../../services/auth.service';
import { Subscription } from 'rxjs';
import { WebsocketService } from '../../services/websocket.service';
import { UserService } from '../../services/user.service';
import Swal from 'sweetalert2';
import { GameHistory } from '../../models/game-history';
import { FormBuilder, FormGroup, FormsModule, ReactiveFormsModule, Validators } from '@angular/forms';
import { CommonModule } from '@angular/common';


@Component({
  selector: 'app-user-profile',
  standalone: true,
  imports: [RouterModule, ReactiveFormsModule, FormsModule, CommonModule],
  templateUrl: './user-profile.component.html',
  styleUrl: './user-profile.component.css'
})
export class UserProfileComponent implements OnInit, OnDestroy {

  public readonly IMG_URL = environment.apiImg;
  user: User = null;
  avatarUrl: string = "";

  userProfileForm: FormGroup; // Modificar datos del usuario
  passwordForm: FormGroup; // Modificar contraseña

  gameHistories: GameHistory[] = [];
  paginatedHistories: GameHistory[] = [];

  // Paginación
  currentPage: number = 1;
  pageSize: number = 5;
  totalPages: number = 1;

  selectedAvatarFile: File = null;
  fileName: string = "Ningún archivo seleccionado";
  error$: Subscription;

  constructor(
    private router: Router,
    private authService: AuthService,
    private webSocketService: WebsocketService,
    private userService: UserService,
    private formBuilder: FormBuilder
  ) {
    // Formulario para cambiar los datos del usuario
    this.userProfileForm = this.formBuilder.group({
      nickname: ['', [Validators.required, Validators.pattern(/^[^@]*$/)]], // No se permite el carácter @
      email: ['', [Validators.required, Validators.email]],
      removeAvatar: [false]
    });

    // Formulario para cambiar la contraseña del usuario
    this.passwordForm = this.formBuilder.group({
      newPassword: ['', [Validators.required, Validators.minLength(6)]],
      confirmPassword: ['', Validators.required]
    },
      { validators: this.passwordMatchValidator });
  }

  passwordMatchValidator(formGroup: FormGroup): { [key: string]: boolean } | null {
    const newPassword = formGroup.get('newPassword')?.value;
    const confirmPassword = formGroup.get('confirmPassword')?.value;

    if (newPassword !== confirmPassword) {
      return { mismatch: true };
    }

    return null;
  }

  async ngOnInit(): Promise<void> {
    if (!this.authService.isAuthenticated()) {
      this.router.navigate(['/']);
    }

    this.user = this.authService.getUser();
    this.avatarUrl = this.IMG_URL + this.user.avatar.url;

    // Cargar el historial de partidas
    await this.getHistories(this.user.userId);

    // Rellenar el formulario con los datos del usuario
    this.userProfileForm.patchValue({
      nickname: this.user.nickname,
      email: this.user.email,
      removeAvatar: false
    });

    // Error del WebSocket
    this.error$ = this.webSocketService.error.subscribe(() => {
      this.authService.logout();
      this.router.navigate(['/']);
    });
  }

  // Obtener el historial de partidas y actualizar la paginación
  async getHistories(userId: number): Promise<void> {
    try {
      const result = await this.userService.GetGameHistories(userId);

      if (result.success) {
        this.gameHistories = result.data;

        // Orden cronológico inverso
        this.gameHistories.sort((a, b) => b.id - a.id);

        this.currentPage = 1;
        this.updatePaginatedHistories();

      } else {
        this.throwError("Se ha producido un error al obtener el historial de partidas");
      }

    } catch (error) {
      //console.error("Error al obtener el historial de partidas:", error);
      this.throwError("Se ha producido un error al obtener el historial de partidas");
    }
  }

  // Actualiza la paginación
  updatePaginatedHistories(): void {
    this.totalPages = Math.max(1, Math.ceil(this.gameHistories.length / this.pageSize));
    let startIndex = (this.currentPage - 1) * this.pageSize;
    let endIndex = startIndex + this.pageSize;
    this.paginatedHistories = this.gameHistories.slice(startIndex, endIndex);
  }

  // Cambia el tamaño de página
  onPageSizeChange(event: Event): void {
    const selectElement = event.target as HTMLSelectElement;
    let newSize = parseInt(selectElement.value, 10);
    this.pageSize = newSize;
    this.currentPage = 1;
    this.updatePaginatedHistories();
  }

  // Navegar a la primera página
  goToFirstPage(): void {
    this.currentPage = 1;
    this.updatePaginatedHistories();
  }

  // Navegar a la página anterior
  goToPreviousPage(): void {
    if (this.currentPage > 1) {
      this.currentPage = this.currentPage - 1;
      this.updatePaginatedHistories();
    }
  }

  // Navegar a la siguiente página
  goToNextPage(): void {
    if (this.currentPage < this.totalPages) {
      this.currentPage = this.currentPage + 1;
      this.updatePaginatedHistories();
    }
  }

  // Navegar a la última página
  goToLastPage(): void {
    this.currentPage = this.totalPages;
    this.updatePaginatedHistories();
  }

  // Input del avatar
  onFileChange(event: any): void {
    const file = event.target.files[0];
    if (file) {
      this.selectedAvatarFile = file;
      this.fileName = this.selectedAvatarFile.name;
    }
  }

  // Enviar formulario
  async onSubmitProfile(): Promise<void> {
    if (this.userProfileForm.invalid) {

      Swal.fire({
        title: "Tienes que rellenar todos los campos correctamente",
        icon: "warning",
        confirmButtonText: "Aceptar"
      });

    } else {
      const formData = new FormData();

      formData.append('nickname', this.userProfileForm.get('nickname').value);
      formData.append('email', this.userProfileForm.get('email').value);

      let removeAvatar = this.userProfileForm.get('removeAvatar').value;
      formData.append('removeAvatar', removeAvatar.toString());

      if (this.selectedAvatarFile && removeAvatar === false) {
        formData.append('avatarFile', this.selectedAvatarFile);
      }

      formData.append('userId', this.user.userId.toString());

      try {
        const result = await this.userService.updateUserProfile(formData);

        if (result.success) {

          Swal.fire({
            title: "Perfil actualizado correctamente, tienes que volver a iniciar sesión",
            icon: 'success',
            showConfirmButton: false,
            timer: 3000,
            timerProgressBar: true,
            didClose: () => {
              this.authService.logout();
              this.router.navigate(['/login']);
            }
          });

        } else {
          //console.error("Error al actualizar los datos:", result.error);
          this.throwError("Se ha producido un error al actualizar los datos");
        }

      } catch (error) {
        //console.error("Error al actualizar los datos:", error);
        this.throwError("Se ha producido un error al actualizar los datos");
      }
    }
  }

  // Enviar formulario de la contraseña
  async onSubmitPassword(): Promise<void> {
    if (this.passwordForm.invalid) {

      Swal.fire({
        title: "Tienes que rellenar la contraseña correctamente",
        icon: "warning",
        confirmButtonText: "Aceptar"
      });

    } else {
      let newPassword = this.passwordForm.get('newPassword').value;

      try {
        const result = await this.userService.modifyPassword(newPassword);

        if (result.success) {

          Swal.fire({
            title: "Contraseña actualizada correctamente, tienes que volver a iniciar sesión",
            icon: 'success',
            showConfirmButton: false,
            timer: 3000,
            timerProgressBar: true,
            didClose: () => {
              this.authService.logout();
              this.router.navigate(['/login']);
            }
          });

        } else {
          //console.error("Error al actualizar la contraseña:", result.error);
          this.throwError("Se ha producido un error al actualizar la contraseña");
        }

      } catch (error) {
        //console.error("Error al actualizar la contraseña:", error);
        this.throwError("Se ha producido un error al actualizar la contraseña");
      }
    }
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
