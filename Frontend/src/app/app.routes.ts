import { Routes } from '@angular/router';
import { HomeComponent } from './pages/home/home.component';
import { LoginComponent } from './pages/login/login.component';
import { SignupComponent } from './pages/signup/signup.component';
import { MenuComponent } from './pages/menu/menu.component';
import { UserProfileComponent } from './pages/user-profile/user-profile.component';
import { MatchmakingComponent } from './pages/matchmaking/matchmaking.component';
import { FriendProfileComponent } from './pages/friend-profile/friend-profile.component';

export const routes: Routes = [
    { path: '', component: HomeComponent }, // Ruta principal
    { path: 'login', component: LoginComponent }, // Ruta login
    { path: 'signup', component: SignupComponent }, // Ruta registro
    { path: 'menu', component: MenuComponent }, // Ruta menú
    { path: 'user-profile', component: UserProfileComponent }, // Ruta perfil del usuario
    { path: 'friend-profile', component: FriendProfileComponent }, // Ruta perfil de otro usuario
    { path: 'matchmaking', component: MatchmakingComponent } // Ruta emparejamiento
];
