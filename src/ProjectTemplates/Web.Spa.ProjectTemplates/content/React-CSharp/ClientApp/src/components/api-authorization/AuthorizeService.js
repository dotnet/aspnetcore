import { UserManager, WebStorageStateStore } from 'oidc-client';
import { ApplicationPaths, ApplicationName } from './ApiAuthorizationConstants';

export class AuthorizeService {
    _callbacks = [];
    _nextSubscriptionId = 0;
    _user = null;
    _isAuthenticated = false;

    async isAuthenticated() {
        const user = await this.getUser();
        return !!user;
    }

    async getUser() {
        if (this._user && this._user.profile) {
            return this._user.profile;
        }

        await this.ensureUserManagerInitialized();
        const user = await this.userManager.getUser();
        return user && user.profile;
    }

    async getAccessToken() {
        await this.ensureUserManagerInitialized();
        const user = await this.userManager.getUser();
        return user && user.access_token;
    }

    // We try to authenticate the user in three different ways:
    // 1) We try to see if we can authenticate the user silently. This happens
    //    when the user is already logged in on the IdP and is done using a hidden iframe
    //    on the client.
    // 2) We try to authenticate the user using a PopUp Window. This might fail if there is a
    //    Pop-Up blocker or the user has disabled PopUps.
    // 3) If the two methods above fail, we redirect the browser to the IdP to perform a traditional
    //    redirect flow.
    async signIn(state) {
        await this.ensureUserManagerInitialized();
        try {
            const silentUser = await this.userManager.signinSilent(this.createArguments(LoginMode.Silent));
            this.updateState(silentUser);
            return this.success(state);
        } catch (silentError) {
            // User might not be authenticated, fallback to popup authentication
            console.log("Silent authentication error: ", silentError);

            try {
                const popUpUser = await this.userManager.signinPopup(this.createArguments(LoginMode.PopUp));
                this.updateState(popUpUser);
                return this.success(state);
            } catch (popUpError) {
                if (popUpError.message === "Popup window closed") {
                    // The user explicitly cancelled the login action by closing an opened popup.
                    return this.error("The user closed the window.");
                }
                console.log("Popup authentication error: ", popUpError);

                // PopUps might be blocked by the user, fallback to redirect
                try {
                    const signInRequest = await this.userManager.createSigninRequest(
                        this.createArguments(LoginMode.Redirect, state));
                    return this.redirect(signInRequest.url);
                } catch (redirectError) {
                    console.log("Redirect authentication error: ", redirectError);
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
    async completeSignIn(url) {
        await this.ensureUserManagerInitialized();
        let response = undefined;
        try {
            response = await this.getSignInResponse(url);
            if (!!response.error) {
                return this.error(`${response.error}: ${response.error_description}`);
            }
        } catch (processSignInResponseError) {
            if (processSignInResponseError.error === "login_required") {
                // This error is thrown by the underlying oidc client when it tries to log in
                // the user silently as in case 1 defined above and the IdP requires the user
                // to enter credentials. We let the user manager handle the response to notify
                // the main window.
                response = processSignInResponseError;
            } else {
                console.log("There was an error processing the sign-in response: ", processSignInResponseError);
                return this.error("There was an error processing the sign-in response.");
            }
        }

        const authenticationState = response.state;
        const mode = authenticationState.mode;

        switch (mode) {
            case LoginMode.Silent:
                try {
                    await this.userManager.signinSilentCallback(url);
                    return this.success(undefined);
                } catch (silentCallbackError) {
                    console.log("Silent callback authentication error: ", silentCallbackError);
                    return this.error("Silent callback authentication error");
                }
            case LoginMode.PopUp:
                try {
                    await this.userManager.signinPopupCallback(url);
                    return this.success(undefined);
                } catch (popupCallbackError) {
                    console.log("Popup callback authentication error: ", popupCallbackError);
                    return this.error("Popup callback authentication error.");
                }
            case LoginMode.Redirect:
                try {
                    let user = await this.userManager.signinRedirectCallback(url);
                    this.updateState(user);
                    return this.success(response.state.userState);
                } catch (redirectCallbackError) {
                    console.log("Redirect callback authentication error: ", redirectCallbackError);
                    return this.error("Redirect callback authentication error.");
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
    async signOut(state) {
        await this.ensureUserManagerInitialized();
        try {
            await this.userManager.signoutPopup(this.createArguments(LoginMode.PopUp));
            this.updateState(undefined);
            return this.success(state);
        } catch (popupSignOutError) {
            console.log("Popup signout error: ", popupSignOutError);
            try {
                const signOutRequest = await this.userManager.createSignoutRequest(
                    this.createArguments(LoginMode.Redirect, state));
                return this.redirect(signOutRequest.url);
            } catch (redirectSignOutError) {
                console.log("Redirect signout error: ", redirectSignOutError);
                return this.error(redirectSignOutError);
            }
        }
    }

    // We are receiving a callback from the IdP. This code can be running in 2 situations:
    // 1) As a PopUp window started by a pop-up login on signOut (above). The code in the main
    //    browser window will close the pop-up window after returning from signOutPopUp
    // 2) On the main browser window when the IdP redirects back to the app. We will process
    //    the response and redirect to the logged-out url or display an error message.
    async completeSignOut(url) {
        await this.ensureUserManagerInitialized();
        let response = undefined;
        try {
            response = await await this.getSignOutResponse(url);
        } catch (processSignOutResponseError) {
            console.log("There was an error processing the sign-out response: ", processSignOutResponseError);
            response = processSignOutResponseError;
        }

        if (!!response.error) {
            return this.error(`${response.error}: ${response.error_description}`);
        }

        const authenticationState = response.state;
        const mode = (authenticationState && authenticationState.mode) ||
            !!window.opener ? LoginMode.PopUp : LoginMode.Redirect;

        switch (mode) {
            case LoginMode.PopUp:
                try {
                    await this.userManager.signoutPopupCallback(url);
                    return this.success(response.state && response.state.userState);
                } catch (popupCallbackError) {
                    console.log("Popup signout callback error: ", popupCallbackError);
                    return this.error("Popup signout callback error");
                }
            case LoginMode.Redirect:
                try {
                    await this.userManager.signoutRedirectCallback(url);
                    this.updateState(undefined);
                    return this.success(response.state.userState);
                } catch (redirectCallbackError) {
                    console.log("Redirect signout callback error: ", redirectCallbackError);
                    return this.error("Redirect signout callback error");
                }
            default:
                throw new Error(`Invalid LoginMode '${mode}'.`);
        }
    }

    updateState(user) {
        this._user = user;
        this._isAuthenticated = !!this._user;
        this.notifySubscribers();
    }

    subscribe(callback) {
        this._callbacks.push({ callback, subscription: this._nextSubscriptionId++ });
        return this._nextSubscriptionId - 1;
    }

    unsubscribe(subscriptionId) {
        const subscriptionIndex = this._callbacks
            .map((element, index) => element.subscription === subscriptionId ? { found: true, index } : { found: false })
            .filter(element => element.found === true);
        if (subscriptionIndex.length !== 1) {
            throw new Error(`Found an invalid number of subscriptions ${subscriptionIndex.length}`);
        }

        this._callbacks = this._callbacks.splice(subscriptionIndex[0].index, 1);
    }

    notifySubscribers() {
        for (let i = 0; i < this._callbacks.length; i++) {
            const callback = this._callbacks[i].callback;
            callback();
        }
    }

    async getSignInResponse(url) {
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

    async getSignOutResponse(url) {
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

    createArguments(mode, state) {
        if (mode !== LoginMode.Silent) {
            return { data: { mode, userState: state } };
        } else {
            return { data: { mode, userState: state }, redirect_uri: this.userManager.settings.redirect_uri };
        }
    }

    error(message) {
        return { status: AuthenticationResultStatus.Fail, message };
    }

    success(state) {
        return { status: AuthenticationResultStatus.Success, state };
    }

    redirect(redirectUrl) {
        return { status: AuthenticationResultStatus.Redirect, redirectUrl };
    }

    async ensureUserManagerInitialized() {
        if (this.userManager !== undefined) {
            return;
        }

        let response = await fetch(ApplicationPaths.ApiAuthorizationClientConfigurationUrl);
        if (!response.ok) {
            throw new Error(`Could not load settings for '${ApplicationName}'`);
        }

        let settings = await response.json();
        settings.automaticSilentRenew = true;
        settings.includeIdTokenInSilentRenew = true;
        settings.userStore = new WebStorageStateStore({
            prefix: ApplicationName
        });

        this.userManager = new UserManager(settings);
    }

    static get instance() { return authService }
}

const LoginMode = {
    Silent: 'silent',
    PopUp: 'popup',
    Redirect: 'redirect'
}

const authService = new AuthorizeService();

export default authService;

export const AuthenticationResultStatus = {
    Redirect: 'redirect',
    Success: 'success',
    Fail: 'fail'
};
