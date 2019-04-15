import { Injectable } from '@angular/core';
import { User, UserManager, WebStorageStateStore } from 'oidc-client';
import { BehaviorSubject, concat, from, Observable } from 'rxjs';
import { filter, map, mergeMap, take, tap } from 'rxjs/operators';
import { ApplicationPaths, ApplicationName } from './api-authorization.constants';

export type IAuthenticationResult =
  SuccessAuthenticationResult |
  FailureAuthenticationResult |
  RedirectAuthenticationResult;

export interface SuccessAuthenticationResult {
  status: AuthenticationResultStatus.Success;
  state: any;
}

export interface FailureAuthenticationResult {
  status: AuthenticationResultStatus.Fail;
  message: string;
}

export interface RedirectAuthenticationResult {
  status: AuthenticationResultStatus.Redirect;
  redirectUrl: string;
}

export enum AuthenticationResultStatus {
  Success,
  Redirect,
  Fail
}

export interface IUser {
  name: string;
}

// Private interfaces
enum LoginMode {
  Silent,
  PopUp,
  Redirect
}

interface IAuthenticationState {
  mode: LoginMode;
  userState?: any;
}

@Injectable({
  providedIn: 'root'
})
export class AuthorizeService {
  // By default pop ups are disabled because they don't work properly on Edge.
  // If you want to enable pop up authentication simply set this flag to false.

  private popUpDisabled = true;
  private userManager: UserManager;
  private userSubject: BehaviorSubject<IUser | null> = new BehaviorSubject(null);

  public isAuthenticated(): Observable<boolean> {
    return this.getUser().pipe(map(u => !!u));
  }

  public getUser(): Observable<IUser | null> {
    return concat(
      this.userSubject.pipe(take(1), filter(u => !!u)),
      this.getUserFromStorage().pipe(filter(u => !!u), tap(u => this.userSubject.next(u))),
      this.userSubject.asObservable());
  }

  public getAccessToken(): Observable<string> {
    return from(this.ensureUserManagerInitialized())
      .pipe(mergeMap(() => from(this.userManager.getUser())),
        map(user => user && user.access_token));
  }

  // We try to authenticate the user in three different ways:
  // 1) We try to see if we can authenticate the user silently. This happens
  //    when the user is already logged in on the IdP and is done using a hidden iframe
  //    on the client.
  // 2) We try to authenticate the user using a PopUp Window. This might fail if there is a
  //    Pop-Up blocker or the user has disabled PopUps.
  // 3) If the two methods above fail, we redirect the browser to the IdP to perform a traditional
  //    redirect flow.
  public async signIn(state: any): Promise<IAuthenticationResult> {
    await this.ensureUserManagerInitialized();
    let user: User = null;
    try {
      user = await this.userManager.signinSilent(this.createArguments(LoginMode.Silent));
      this.userSubject.next(user.profile);
      return this.success(state);
    } catch (silentError) {
      // User might not be authenticated, fallback to popup authentication
      console.log('Silent authentication error: ', silentError);

      try {
        if (this.popUpDisabled) {
          throw new Error('Popup disabled. Change \'authorize.service.ts:AuthorizeService.popupDisabled\' to false to enable it.');
        }
        user = await this.userManager.signinPopup(this.createArguments(LoginMode.PopUp));
        this.userSubject.next(user.profile);
        return this.success(state);
      } catch (popupError) {
        if (popupError.message === 'Popup window closed') {
          // The user explicitly cancelled the login action by closing an opened popup.
          return this.error('The user closed the window.');
        } else if (!this.popUpDisabled) {
          console.log('Popup authentication error: ', popupError);
        }

        // PopUps might be blocked by the user, fallback to redirect
        try {
          const signInRequest = await this.userManager.createSigninRequest(
            this.createArguments(LoginMode.Redirect, state));
          return this.redirect(signInRequest.url);
        } catch (redirectError) {
          console.log('Redirect authentication error: ', redirectError);
          return this.error(redirectError);
        }
      }
    }
  }

  // We are receiving a callback from the IdP. This code can be running in 3 situations:
  // 1) As a hidden iframe started by a silent login on signIn (above). The code in the main
  //    browser window will close the iframe after returning from signInSilent.
  // 2) As a PopUp window started by a pop-up login on signIn (above). The code in the main
  //    browser window will close the pop-up window after returning from signInPopUp
  // 3) On the main browser window when the IdP redirects back to the app. We will process
  //    the response and redirect to the return url or display an error message.
  public async completeSignIn(url: string): Promise<IAuthenticationResult> {
    await this.ensureUserManagerInitialized();
    let response;
    try {
      response = await this.getSignInResponse(url);
      if (!!response.error) {
        return this.error(`${response.error}: ${response.error_description}`);
      }
    } catch (processSignInResponseError) {
      if (processSignInResponseError.error === 'login_required') {
        // This error is thrown by the underlying oidc client when it tries to log in
        // the user silently as in case 1 defined above and the IdP requires the user
        // to enter credentials. We let the user manager handle the response to notify
        // the main window.
        response = processSignInResponseError;
      } else {
        console.log('There was an error processing the sign-in response: ', processSignInResponseError);
        return this.error('There was an error processing the sign-in response.');
      }
    }

    const authenticationState = response.state as IAuthenticationState;
    const mode = authenticationState.mode;

    switch (mode) {
      case LoginMode.Silent:
        try {
          await this.userManager.signinSilentCallback(url);
          return this.success(undefined);
        } catch (silentCallbackError) {
          console.log('Silent callback authentication error: ', silentCallbackError);
          return this.error('Silent callback authentication error');
        }
      case LoginMode.PopUp:
        try {
          await this.userManager.signinPopupCallback(url);
          return this.success(undefined);
        } catch (popupCallbackError) {
          console.log('Popup callback authentication error: ', popupCallbackError);
          return this.error('Popup callback authentication error.');
        }
      case LoginMode.Redirect:
        try {
          const user = await this.userManager.signinRedirectCallback(url);
          this.userSubject.next(user.profile);
          return this.success(response.state.userState);
        } catch (redirectCallbackError) {
          console.log('Redirect callback authentication error: ', redirectCallbackError);
          return this.error('Redirect callback authentication error.');
        }
      default:
        throw new Error(`Invalid login mode '${mode}'.`);
    }
  }

  // We try to sign out the user in two different ways:
  // 1) We try to do a sign-out using a PopUp Window. This might fail if there is a
  //    Pop-Up blocker or the user has disabled PopUps.
  // 2) If the method above fails, we redirect the browser to the IdP to perform a traditional
  //    post logout redirect flow.
  public async signOut(state: any): Promise<IAuthenticationResult> {
    await this.ensureUserManagerInitialized();
    try {
      await this.userManager.signoutPopup(this.createArguments(LoginMode.PopUp));
      this.userSubject.next(null);
      return this.success(state);
    } catch (popupSignOutError) {
      console.log('Popup signout error: ', popupSignOutError);
      try {
        const signInRequest = await this.userManager.createSignoutRequest(
          this.createArguments(LoginMode.Redirect, state));
        return this.redirect(signInRequest.url);
      } catch (redirectSignOutError) {
        console.log('Redirect signout error: ', popupSignOutError);
        return this.error(redirectSignOutError);
      }
    }
  }

  // We are receiving a callback from the IdP. This code can be running in 2 situations:
  // 1) As a PopUp window started by a pop-up login on signOut (above). The code in the main
  //    browser window will close the pop-up window after returning from signOutPopUp
  // 2) On the main browser window when the IdP redirects back to the app. We will process
  //    the response and redirect to the logged-out url or display an error message.
  public async completeSignOut(url: string): Promise<IAuthenticationResult> {
    await this.ensureUserManagerInitialized();
    let response;
    try {
      response = await await this.getSignOutResponse(url);
    } catch (processSignOutResponseError) {
      console.log('There was an error processing the sign-out response: ', processSignOutResponseError);
      response = processSignOutResponseError;
    }

    if (!!response.error) {
      return this.error(`${response.error}: ${response.error_description}`);
    }

    const authenticationState = response.state as IAuthenticationState;
    const mode = (authenticationState && authenticationState.mode) ||
      !!window.opener ? LoginMode.PopUp : LoginMode.Redirect;

    switch (mode) {
      case LoginMode.PopUp:
        try {
          await this.userManager.signoutPopupCallback(url);
          return this.success(response.state && response.state.userState);
        } catch (popupCallbackError) {
          console.log('Popup signout callback error: ', popupCallbackError);
          return this.error('Popup signout callback error');
        }
      case LoginMode.Redirect:
        try {
          await this.userManager.signoutRedirectCallback(url);
          this.userSubject.next(null);
          return this.success(response.state.userState);
        } catch (redirectCallbackError) {
          console.log('Redirect signout callback error: ', redirectCallbackError);
          return this.error('Redirect signout callback error');
        }
      default:
        throw new Error(`Invalid LoginMode '${mode}'.`);
    }
  }

  private async getSignInResponse(url: string) {
    const keys = await this.userManager.settings.stateStore.getAllKeys();
    const states = keys.map(key => ({ key, state: this.userManager.settings.stateStore.get(key) }));
    for (const state of states) {
      state.state = await state.state;
    }
    try {
      const response = await this.userManager.processSigninResponse(url);
      return response;
    } finally {
      for (const state of states) {
        await this.userManager.settings.stateStore.set(state.key, state.state);
      }
    }
  }

  private async getSignOutResponse(url: string) {
    const keys = await this.userManager.settings.stateStore.getAllKeys();
    const states = keys.map(key => ({ key, state: this.userManager.settings.stateStore.get(key) }));
    for (const state of states) {
      state.state = await state.state;
    }
    try {
      const response = await this.userManager.processSignoutResponse(url);
      return response;
    } finally {
      for (const state of states) {
        await this.userManager.settings.stateStore.set(state.key, state.state);
      }
    }
  }

  private createArguments(mode: LoginMode, state?: any): any {
    if (mode !== LoginMode.Silent) {
      return { data: { mode, userState: state } };
    } else {
      return { data: { mode, userState: state }, redirect_uri: this.userManager.settings.redirect_uri };
    }
  }

  private error(message: string): IAuthenticationResult {
    return { status: AuthenticationResultStatus.Fail, message };
  }

  private success(state: any): IAuthenticationResult {
    return { status: AuthenticationResultStatus.Success, state };
  }

  private redirect(redirectUrl: string): IAuthenticationResult {
    return { status: AuthenticationResultStatus.Redirect, redirectUrl };
  }

  private async ensureUserManagerInitialized(): Promise<void> {
    if (this.userManager !== undefined) {
      return;
    }

    const response = await fetch(ApplicationPaths.ApiAuthorizationClientConfigurationUrl);
    if (!response.ok) {
      throw new Error(`Could not load settings for '${ApplicationName}'`);
    }

    const settings: any = await response.json();
    settings.automaticSilentRenew = true;
    settings.includeIdTokenInSilentRenew = true;
    settings.userStore = new WebStorageStateStore({
      prefix: ApplicationName
    });
    this.userManager = new UserManager(settings);
  }

  private getUserFromStorage(): Observable<IUser> {
    return from(this.ensureUserManagerInitialized())
      .pipe(
        mergeMap(() => this.userManager.getUser()),
        map(u => u && u.profile));
  }
}
