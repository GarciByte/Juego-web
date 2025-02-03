import { Component, OnInit } from '@angular/core';
import { ActivatedRoute, ParamMap, Router, RouterModule } from '@angular/router';
import { environment } from '../../../environments/environment.development';
import { User } from '../../models/user';
import { AuthService } from '../../services/auth.service';
import { Subscription } from 'rxjs';
import { UserService } from '../../services/user.service';

@Component({
  selector: 'app-friend-profile',
  imports: [RouterModule],
  templateUrl: './friend-profile.component.html',
  styleUrl: './friend-profile.component.css'
})
export class FriendProfileComponent implements OnInit {

  public readonly IMG_URL = environment.apiImg;

  routeQueryMap$: Subscription;

    user: User = null;
    avatarUrl: string = null;

  constructor(
      private router: Router,
      private authService: AuthService,
      private route: ActivatedRoute,
      private userService: UserService
    ) { }

  ngOnInit(): void {
    if (!this.authService.isAuthenticated()) {
      this.router.navigate(['/login']);
    }

    this.routeQueryMap$ = this.route.queryParamMap.subscribe(queryMap => this.init(queryMap));
  }

  async init(queryMap: ParamMap) {
    const id = parseInt(queryMap.get("id"));
    await this.getUser(id);
    console.log(this.user);
    console.log(this.avatarUrl);
  }

  async getUser(userId: number): Promise<void> {
    try {
      const result = await this.userService.getUserById(userId);

      if (result.success) {
        this.user = result.data;
        this.avatarUrl = this.user.avatar.url;
      }

    } catch (error) {
      console.error('Error al buscar el usuario:', error);
    }
  }
}
