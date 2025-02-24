import { Component, OnDestroy, OnInit } from '@angular/core';
import { ActivatedRoute, ParamMap, Router, RouterModule } from '@angular/router';
import { environment } from '../../../environments/environment.development';
import { User } from '../../models/user';
import { AuthService } from '../../services/auth.service';
import { Subscription } from 'rxjs';
import { UserService } from '../../services/user.service';
import { WebsocketService } from '../../services/websocket.service';
import Swal from 'sweetalert2';

@Component({
  selector: 'app-friend-profile',
  imports: [RouterModule],
  templateUrl: './friend-profile.component.html',
  styleUrl: './friend-profile.component.css'
})
export class FriendProfileComponent implements OnInit, OnDestroy {

  public readonly IMG_URL = environment.apiImg;

  user: User = null;
  avatarUrl: string = null;
  GameHistoriesList: History[];
  error$: Subscription;
  routeQueryMap$: Subscription;

  constructor(
    private router: Router,
    private authService: AuthService,
    private route: ActivatedRoute,
    private userService: UserService,
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

    this.routeQueryMap$ = this.route.queryParamMap.subscribe(queryMap => this.init(queryMap));
  }

  async init(queryMap: ParamMap) {
    const id = parseInt(queryMap.get("id"));
    await this.getUser(id);
    await this.getHistories(this.user.userId);
    //console.log(this.user);
    //console.log(this.GameHistoriesList);
  }

  async getUser(userId: number): Promise<void> {
    try {
      const result = await this.userService.getUserById(userId);

      if (result.success) {
        this.user = result.data;
        this.avatarUrl = this.user.avatar.url;
      }

    } catch (error) {
      //console.error('Error al buscar el usuario:', error);
      this.throwError("Se ha producido un error al obtener los datos del usuario");
    }
  }

  async getHistories(userId: number): Promise<void> {
    try {
      const result = await this.userService.GetGameHistories(userId);

      if (result.success) {
        this.GameHistoriesList = result.data;
      }

    } catch (error) {
      //console.error('Error al obtener el historial de partidas:', error);
      this.throwError("Se ha producido un error al obtener el historial de partidas");
    }
  }

  ngOnDestroy(): void {
    if (this.error$) {
      this.error$.unsubscribe();
    }

    if (this.routeQueryMap$) {
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
