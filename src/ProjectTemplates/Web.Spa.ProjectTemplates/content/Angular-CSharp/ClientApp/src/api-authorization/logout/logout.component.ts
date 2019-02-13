import { Component, OnInit } from '@angular/core';
import { AuthenticationResultStatus, AuthorizeService } from '../authorize.service';
import { BehaviorSubject } from 'rxjs';
import { ActivatedRoute, Router } from '@angular/router';
import { take } from 'rxjs/operators';
import { LogoutActions, ApplicationPaths, ReturnUrlType } from '../api-authorization.constants';

@Component({
  selector: 'app-logout',
  templateUrl: './logout.component.html',
  styleUrls: ['./logout.component.css']
})
export class LogoutComponent implements OnInit {
  private message = new BehaviorSubject<string>(null);

  constructor(
    private authorizeService: AuthorizeService,
    private activatedRoute: ActivatedRoute,
    private router: Router) { }

  async ngOnInit() {
    const action = this.activatedRoute.snapshot.url[1]
    switch (action.path) {
      case LogoutActions.LogoutCallback:
        await this.processLogoutCallback();
        break;
      case LogoutActions.Logout:
        await this.logout(this.getReturnUrl());
        break;
      case LogoutActions.LoggedOut:
        this.message.next("You successfully logged out!");
        break;
    }
  }

  private async logout(returnUrl: string): Promise<void> {
    const state: INavigationState = { returnUrl };
      var isauthenticated = await this.authorizeService.isAuthenticated().pipe(
        take(1)
      ).toPromise();      
    if (isauthenticated) {
      const result = await this.authorizeService.signOut(state);
      switch (result.status) {
        case AuthenticationResultStatus.Redirect:
          window.location.replace(result.redirectUrl);
          break;
        case AuthenticationResultStatus.Success:
          await this.navigateToReturnUrl(returnUrl);
          break;
        case AuthenticationResultStatus.Fail:
          this.message.next(result.message);
          break;
      }
    } else {
      this.message.next("You successfully logged out!");
    }
  }

  private async processLogoutCallback(): Promise<void> {
    const url = window.location.href;
    const result = await this.authorizeService.completeSignOut(url);
    switch (result.status) {
      case AuthenticationResultStatus.Redirect:
        // There should not be any redirects as the only time completeAuthentication finishes
        // is when we are doing a redirect sign in flow.
        throw new Error('Should not redirect.');
      case AuthenticationResultStatus.Success:
        await this.navigateToReturnUrl(this.getReturnUrl(result.state));
        break;
      case AuthenticationResultStatus.Fail:
        this.message.next(result.message);
        break;
    }
  }

  private async navigateToReturnUrl(returnUrl: string) {
    await this.router.navigateByUrl(returnUrl, {
      replaceUrl: true
    });
  }

  private getReturnUrl(state?: INavigationState): string {
    return (state && state.returnUrl) ||
      (this.activatedRoute.snapshot.queryParams as INavigationState).returnUrl ||
      ApplicationPaths.LoggedOut;
  }
}

interface INavigationState {
  [ReturnUrlType]: string;
}
