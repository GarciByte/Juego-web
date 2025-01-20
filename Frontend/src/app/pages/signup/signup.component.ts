import { NgIf } from '@angular/common';
import { Component, OnInit } from '@angular/core';
import { ReactiveFormsModule, FormsModule, FormBuilder, FormGroup, Validators } from '@angular/forms';
import { Router, RouterModule } from '@angular/router';
import { AuthService } from '../../services/auth.service';
import Swal from 'sweetalert2';

@Component({
  selector: 'app-signup',
  standalone: true,
  imports: [RouterModule, ReactiveFormsModule, FormsModule, NgIf],
  templateUrl: './signup.component.html',
  styleUrl: './signup.component.css'
})
export class SignupComponent implements OnInit {

  myForm: FormGroup;
  selectedFile: File;
  fileName: string = "Ningún archivo seleccionado";

  constructor(
    private formBuilder: FormBuilder,
    private authService: AuthService,
    private router: Router
  ) {
    this.myForm = this.formBuilder.group({
      avatar: [''],
      nickname: ['', [Validators.required, Validators.pattern(/^[^@]*$/)]], // No se permite el carácter @
      email: ['', [Validators.required, Validators.email]],
      password: ['', [Validators.required, Validators.minLength(6)]],
      confirmPassword: ['', Validators.required]
    },
      { validators: this.passwordMatchValidator });
  }

  ngOnInit(): void {
    if (this.authService.isAuthenticated()) {
      this.router.navigate(['/menu']);
    }
  }

  onFileSelected(event: any): void {
    const file = event.target.files[0];
    if (file) {
      this.selectedFile = file;
      this.fileName = this.selectedFile.name;
      this.myForm.patchValue({ avatar: this.selectedFile });
    }
  }

  passwordMatchValidator(form: FormGroup) {
    const password = form.get('password')?.value;
    const confirmPasswordControl = form.get('confirmPassword');
    const confirmPassword = confirmPasswordControl?.value;

    if (password !== confirmPassword && confirmPasswordControl) {
      confirmPasswordControl.setErrors({ mismatch: true });
    } else if (confirmPasswordControl) {
      confirmPasswordControl.setErrors(null);
    }
  }

  async submit() {
    if (this.myForm.valid) {
      const formData = new FormData();

      formData.append('nickname', this.myForm.get('nickname').value);
      formData.append('email', this.myForm.get('email').value);
      formData.append('password', this.myForm.get('password').value);
      formData.append('avatar', this.selectedFile);

      const signupResult = await this.authService.signup(formData); // Registro

      if (signupResult.success) {
        console.log('Registro exitoso', signupResult);

        const authData = { nickname: "", email: this.myForm.get('email').value, password: this.myForm.get('password').value };
        const loginResult = await this.authService.login(authData, false); // Login

        if (loginResult.success) {
          console.log('Inicio de sesión exitoso', loginResult);

          const user = this.authService.getUser();
          const nickname = user ? user.nickname : null;

          Swal.fire({
            title: "Te has registrado con éxito",
            text: `¡Hola, ${nickname}!`,
            icon: 'success',
            showConfirmButton: false,
            timer: 3000,
            timerProgressBar: true,
            didClose: () => this.router.navigate(['/menu'])
          });

        } else {
          this.throwError("Error en el inicio de sesión");
        }

      } else {
        console.error(signupResult);
        this.throwError("Error en el registro");
      }

    } else {
      this.throwError("Formulario no válido");
    }

  }

  throwError(error: string) {
    Swal.fire({
      title: "Se ha producido un error",
      text: error,
      icon: "error",
      confirmButtonText: "Vale"
    });
  }

}