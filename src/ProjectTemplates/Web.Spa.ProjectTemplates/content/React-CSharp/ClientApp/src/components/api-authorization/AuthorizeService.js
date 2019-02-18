import { UserManager } from 'oidc-client';

export class AuthorizeService {
    _callbacks = [];
    _nextSubscriptionId = 0;
    _user = null;
    _isAuthenticated = false;

    async isAuthenticated() {
        const user = await this.getUser();
        return !!user;
    }

    async getAccessToken() {
        await this.ensureUserManagerInitialized();
        const user = await this.userManager.getUser();
        return user && user.access_token;
    }

    async getUser() {
        if (this._user && this._user.profile) {
            return this._user;
        }

        await this.ensureUserManagerInitialized();
        const user = await this.userManager.getUser();
        // this.updateState(user && user.profile);
        return user && user.profile;
    }

    async signOut(state) {
        await this.ensureUserManagerInitialized();
        try {
            await this.userManager.signoutPopup(this.createArguments(LoginMode.PopUp));
            this.notifySubscribers();
            return this.success(state);
        } catch (exception) {
            // PopUps might be blocked by the user, fallback to redirect
            try {
                const signOutRequest = await this.userManager.createSignoutRequest(
                    this.createArguments(LoginMode.Redirect, state));
                return this.redirect(signOutRequest.url);
            } catch (error) {
                return this.error(error);
            }
        }
    }

    async completeSignOut(url) {
        await this.ensureUserManagerInitialized();
        const response = await this.getSignOutResponse(url);

        if (!!response.error) {
            return this.error(`${response.error}: ${response.error_description}`);
        }

        const authenticationState = response.state;
        const mode = (authenticationState && authenticationState.mode) ||
            !!window.opener ? LoginMode.PopUp : LoginMode.Redirect;

        switch (mode) {
            case LoginMode.PopUp:
                await this.userManager.signoutPopupCallback(url);
                window.close();
                // The call above will close the window so we'll never get here.
                throw new Error("Should never get here.");
            case LoginMode.Redirect:
                this.updateState(await this.userManager.signoutRedirectCallback(url));
                return this.success(response.state.userState);
            default:
                throw new Error(`Invalid login mode '${mode}'.`);
        }
    }

    async authenticate(state) {
        await this.ensureUserManagerInitialized();
        try {
            this.updateState(await this.userManager.signinSilent(this.createArguments(LoginMode.Silent)));
            return this.success(state);
        } catch (exception) {
            // User might not be authenticated, fallback to popup authentication
            try {
                this.updateState(await this.userManager.signinPopup(this.createArguments(LoginMode.PopUp)));
                return this.success(state);
            } catch (exception) {
                // PopUps might be blocked by the user, fallback to redirect
                try {
                    const signInRequest = await this.userManager.createSigninRequest(
                        this.createArguments(LoginMode.Redirect, state));
                    return this.redirect(signInRequest.url);
                } catch (error) {
                    return this.error(error);
                }
            }
        }
    }

    async completeAuthentication(url) {
        await this.ensureUserManagerInitialized();
        const response = await this.getAuthorizationResponse(url);

        if (!!response.error) {
            return this.error(`${response.error}: ${response.error_description}`);
        }

        const authenticationState = response.state;
        const mode = authenticationState.mode;

        switch (mode) {
            case LoginMode.Silent:
                await this.userManager.signinSilentCallback(url);
                // The call above will close the window so we'll never get here.
                throw new Error("Should never get here.");
            case LoginMode.PopUp:
                await this.userManager.signinPopupCallback(url);
                // The call above will close the window so we'll never get here.
                throw new Error("Should never get here.");
            case LoginMode.Redirect:
                this.updateState(await this.userManager.signinRedirectCallback(url));
                return this.success(response.state.userState);
            default:
                throw new Error(`Invalid login mode '${mode}'.`);
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
        var subscriptionIndex = this._callbacks
            .map((element, index) => element.subscription === subscriptionId ? { found: true, index } : { found: false })
            .filter(element => element.found === true);
        if (subscriptionIndex.length !== 1) {
            throw new Error(`Found an invalid number of subscriptions ${subscriptionIndex.length}`);
        }

        this._callbacks = this._callbacks.splice(subscriptionIndex[0].index, 1);
    }

    notifySubscribers() {
        for (let i = 0; i < this._callbacks.length; i++) {
            let callback = this._callbacks[i].callback;
            callback();
        }
    }

    async getAuthorizationResponse(url) {
        const keys = await this.userManager.settings.stateStore.getAllKeys();
        const states = keys.map(key => ({ key, state: this.userManager.settings.stateStore.get(key) }));
        for (const state of states) {
            state.state = await state.state;
        }
        const response = await this.userManager.processSigninResponse(url);
        for (const state of states) {
            await this.userManager.settings.stateStore.set(state.key, state.state);
        }
        return response;
    }

    async getSignOutResponse(url) {
        const keys = await this.userManager.settings.stateStore.getAllKeys();
        const states = keys.map(key => ({ key, state: this.userManager.settings.stateStore.get(key) }));
        for (const state of states) {
            state.state = await state.state;
        }
        const response = await this.userManager.processSignoutResponse(url);
        for (const state of states) {
            await this.userManager.settings.stateStore.set(state.key, state.state);
        }
        return response;
    }

    createArguments(mode, state) {
        return { data: { mode, userState: state } };
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

        let response = await fetch('/_configuration/react');
        if (!response.ok) {
            throw new Error(`Could not load settings for 'reactSPA'`);
        }

        let settings = await response.json();
        settings.post_logout_redirect_uri = settings.post_logout_redirect_uri.replace('login-callback', 'logout-callback');
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