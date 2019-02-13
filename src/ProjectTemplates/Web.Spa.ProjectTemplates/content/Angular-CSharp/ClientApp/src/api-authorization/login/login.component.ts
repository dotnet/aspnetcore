import { Component, OnInit } from '@angular/core';
import { AuthorizeService, AuthenticationResultStatus } from '../authorize.service';
import { ActivatedRoute, Router } from '@angular/router';
import { BehaviorSubject } from 'rxjs';
import { LoginActions, QueryParameterNames, ApplicationPaths, ReturnUrlType } from '../api-authorization.constants';

@Component({
  selector: 'app-login',
  templateUrl: './login.component.html',
  styleUrls: ['./login.component.css']
})
export class LoginComponent implements OnInit {
  private message = new BehaviorSubject<string>(null);

  constructor(
    private authorizeService: AuthorizeService,
    private activatedRoute: ActivatedRoute,
    private router: Router) { }

  async ngOnInit() {
    const action = this.activatedRoute.snapshot.url[1]
    switch (action.path) {
      case LoginActions.Login:
        await this.login(this.getReturnUrl());
        break;
      case LoginActions.LoginCallback:
        await this.processLoginCallback();
        break;
      case LoginActions.LoginFailed:
        const message = this.activatedRoute.snapshot.queryParamMap.get(QueryParameterNames.Message);
        this.message.next(message);
        break;
      case LoginActions.Profile:
        this.redirectToProfile();
        break;
      case LoginActions.Register:
        this.redirectToRegister();
        break;
    }
  }


  private async login(returnUrl: string): Promise<void> {
    const state: INavigationState = { returnUrl };
    const result = await this.authorizeService.signIn(state);
    this.message.next(undefined);
    switch (result.status) {
      case AuthenticationResultStatus.Redirect:
        window.location.replace(result.redirectUrl);
        break;
      case AuthenticationResultStatus.Success:
        await this.navigateToReturnUrl(returnUrl);
        break;
      case AuthenticationResultStatus.Fail:
        await this.router.navigate(ApplicationPaths.LoginFailedPathComponents, {
          queryParams: { [QueryParameterNames.Message]: result.message }
        });
        break;
    }
  }

  private async processLoginCallback(): Promise<void> {
    const url = window.location.href;
    const result = await this.authorizeService.completeSignIn(url);
    switch (result.status) {
      case AuthenticationResultStatus.Redirect:
        // There should not be any redirects as completeSignIn never redirects.
        throw new Error('Should not redirect.');
      case AuthenticationResultStatus.Success:
        await this.navigateToReturnUrl(this.getReturnUrl(result.state));
        break;
      case AuthenticationResultStatus.Fail:
        this.message.next(result.message);
        break;
    }
  }

  private redirectToRegister(): any {
    this.redirectToApiAuthorizationPath(
      `${ApplicationPaths.IdentityRegisterPath}?returnUrl=${encodeURI('/' + ApplicationPaths.Login)}`);
  }

  private redirectToProfile(): void {
    this.redirectToApiAuthorizationPath(ApplicationPaths.IdentityManagePath);
  }

  private async navigateToReturnUrl(returnUrl: string) {
    await this.router.navigateByUrl(returnUrl, {
      replaceUrl: true
    });
  }

  private getReturnUrl(state?: INavigationState): string {
    return (state && state.returnUrl) ||
      (this.activatedRoute.snapshot.queryParams as INavigationState).returnUrl ||
      ApplicationPaths.DefaultLoginRedirectPath;
  }

  private redirectToApiAuthorizationPath(apiAuthorizationPath: string) {
    const redirectUrl = `${this.getBaseUrl()}${apiAuthorizationPath}`;
    window.location.replace(redirectUrl);
  }

  private getBaseUrl(): string {
    const url = window.location;
    return `${url.protocol}//${url.host}`;
  }
}

interface INavigationState {
  [ReturnUrlType]: string;
}
