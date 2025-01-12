import { Routes } from '@angular/router';
import { HomeComponent } from './pages/home/home.component';
import { LoginComponent } from './pages/login/login.component';
import { SignupComponent } from './pages/signup/signup.component';

export const routes: Routes = [
    { path: '', component: HomeComponent }, // Ruta Principal
    { path: 'login', component: LoginComponent }, // Ruta login
    { path: 'signup', component: SignupComponent } // Ruta registro
];
