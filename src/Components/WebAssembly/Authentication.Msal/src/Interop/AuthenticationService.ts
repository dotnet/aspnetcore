import * as Msal from 'msal';
import { StringDict } from 'msal/lib-commonjs/MsalTypes';
import { ClientAuthErrorMessage } from 'msal/lib-commonjs/error/ClientAuthError';

interface AccessTokenRequestOptions {
    scopes: string[];
    returnUrl: string;
}

interface AccessTokenResult {
    status: AccessTokenResultStatus;
    token?: AccessToken;
}

interface AccessToken {
    value: string;
    expires: Date;
    grantedScopes: string[];
}

enum AccessTokenResultStatus {
    Success = "success",
    RequiresRedirect = "requiresRedirect"
}

enum AuthenticationResultStatus {
    Redirect = "redirect",
    Success = "success",
    Failure = "failure",
    OperationCompleted = "operation-completed"
}

interface AuthenticationResult {
    status: AuthenticationResultStatus;
    state?: any;
    message?: string;
}

interface AuthorizeService {
    getUser(): Promise<StringDict | undefined>;
    getAccessToken(request?: AccessTokenRequestOptions): Promise<AccessTokenResult>;
    signIn(state: any): Promise<AuthenticationResult>;
    completeSignIn(state: any): Promise<AuthenticationResult>;
    signOut(state: any): Promise<AuthenticationResult>;
    completeSignOut(url: string): Promise<AuthenticationResult>;
}

interface AuthorizeServiceConfiguration extends Msal.Configuration {
    defaultAccessTokenScopes: string[];
    additionalScopesToConsent: string[]
}

class MsalAuthorizeService implements AuthorizeService {
    _settings: AuthorizeServiceConfiguration;
    _msalApplication: Msal.UserAgentApplication;
    _callbackPromise: Promise<AuthenticationResult>;

    constructor(settings: AuthorizeServiceConfiguration) {
        this._settings = settings;
        const callbackUrl = location.href;
        this._msalApplication = new Msal.UserAgentApplication(this._settings);
        this._callbackPromise = this.createCallbackResult(callbackUrl);
    }

    async getUser() {
        var account = this._msalApplication.getAccount();
        return account?.idTokenClaims;
    }

    async getAccessToken(request?: AccessTokenRequestOptions): Promise<AccessTokenResult> {
        try {
            const newToken = await this.getTokenCore(request?.scopes);

            return {
                status: AccessTokenResultStatus.Success,
                token: newToken
            };

        } catch (e) {
            return {
                status: AccessTokenResultStatus.RequiresRedirect
            };
        }
    }

    async getTokenCore(scopes?: string[]): Promise<AccessToken | undefined> {
        const tokenScopes = {
            redirectUri: this._settings.auth.redirectUri as string,
            scopes: scopes || this._settings.defaultAccessTokenScopes
        };

        let response: Msal.AuthResponse;

        try {
            response = await this._msalApplication.acquireTokenSilent(tokenScopes);
            return {
                value: response.accessToken,
                grantedScopes: response.scopes,
                expires: response.expiresOn
            };
        } catch (e) {
            return undefined;
        }
    }

    async signIn(state: any) {
        try {
            const request: Msal.AuthenticationParameters = {
                redirectUri: this._settings.auth.redirectUri as string,
                state: await this.saveState(state)
            };

            if (this._settings.defaultAccessTokenScopes && this._settings.defaultAccessTokenScopes.length > 0) {
                request.scopes = this._settings.defaultAccessTokenScopes;
            }

            if (this._settings.additionalScopesToConsent && this._settings.additionalScopesToConsent.length > 0) {
                request.extraScopesToConsent = this._settings.additionalScopesToConsent;
            }

            const result = await this.signInCore(request);
            if (!result) {
                return this.redirect();
            } else if (this.isMsalError(result)) {
                return this.error(result.errorMessage);
            }

            try {
                if (this._settings.defaultAccessTokenScopes?.length > 0) {
                    // This provisions the token as part of the sign-in flow eagerly so that is already in the cache
                    // when the app asks for it.
                    await this._msalApplication.acquireTokenSilent(request);
                }
            } catch (e) {
                return this.error(e.errorMessage);
            }

            return this.success(state);
        } catch (e) {
            return this.error(e.message);
        }

    }

    async signInCore(request: Msal.AuthenticationParameters): Promise<Msal.AuthResponse | Msal.AuthError | undefined> {
        try {
            return await this._msalApplication.loginPopup(request);
        } catch (e) {
            if (this.isMsalError(e) && e.errorCode !== ClientAuthErrorMessage.userCancelledError.code) {
                try {
                    this._msalApplication.loginRedirect(request);
                } catch (e) {
                    return e;
                }
            } else {
                return e;
            }
        }
    }

    async completeSignIn() {
        const result = await this._callbackPromise;
        return result;
    }

    async signOut(state: any) {
        const logoutStateId = await this.saveState(state);
        sessionStorage.setItem('LogoutState', logoutStateId);
        console.log(this._msalApplication.getCurrentConfiguration().auth.postLogoutRedirectUri);
        this._msalApplication.logout();
        return this.operationCompleted();
    }

    async completeSignOut(url: string) {
        const logoutStateId = sessionStorage.getItem('LogoutState');
        const updatedUrl = new URL(url);
        updatedUrl.search = `?state=${logoutStateId}`;
        const logoutState = await this.retrieveState(updatedUrl.href);
        if (logoutState) {
            return this.success(logoutState);
        } else {
            return this.operationCompleted();
        }
    }

    async saveState<T>(state: T): Promise<string> {
        const entropy = window.crypto.getRandomValues(new Uint8Array(32));
        const base64 = await new Promise<string>((resolve, _) => {
            const reader = new FileReader();
            reader.onloadend = evt => resolve((evt?.target?.result as string).split(',')[1].replace(/\+/g, '-').replace(/\//g, '_').replace(/=/g, ''));
            reader.readAsDataURL(new Blob([entropy]));
        })
        sessionStorage.setItem(`AuthorizeService.${base64}`, JSON.stringify(state));
        return base64;
    }

    async retrieveState<T>(url: string): Promise<T | undefined> {
        const parsedUrl = new URL(url);
        const fromHash = parsedUrl.hash && parsedUrl.hash.length > 0 && new URLSearchParams(parsedUrl.hash.substring(1));
        let state = fromHash && fromHash.getAll('state');
        if (state && state.length > 1) {
            return undefined;
        } else if (!state || state.length == 0) {
            state = parsedUrl.searchParams && parsedUrl.searchParams.getAll('state');
            if (!state || state.length !== 1) {
                return undefined;
            }
        }

        const appState = this._msalApplication.getAccountState(state[0])
        const signInStateKey = `AuthorizeService.${appState}`;
        const signInStateString = sessionStorage.getItem(signInStateKey);
        if (signInStateString) {
            sessionStorage.removeItem(signInStateKey);
            const savedState = JSON.parse(signInStateString);
            return savedState;
        }

        const signOutStateKey = `AuthorizeService.${state[0]}`;
        const signOutStateString = sessionStorage.getItem(signOutStateKey);
        if (signOutStateString) {
            sessionStorage.removeItem(signOutStateKey);
            const savedState = JSON.parse(signOutStateString);
            return savedState;
        }

        return undefined;
    }

    private async createCallbackResult(callbackUrl: string): Promise<AuthenticationResult> {
        // msal.js requires a callback to be registered during app initialization to handle redirect flows.
        // To map that behavior to our API we register a callback early and store the result of that callback
        // as a promise on an instance field to be able to serve the state back to the main app.
        const promiseFactory = (resolve: (result: Msal.AuthResponse) => void, reject: (error: Msal.AuthError) => void): void => {
            this._msalApplication.handleRedirectCallback(
                authenticationResponse => {
                    resolve(authenticationResponse);
                },
                authenticationError => {
                    reject(authenticationError);
                });
        }

        try {
            // Evaluate the promise to capture any authentication errors
            await new Promise<Msal.AuthResponse>(promiseFactory);
            // See https://github.com/AzureAD/microsoft-authentication-library-for-js/wiki/FAQs#q6-how-to-avoid-page-reloads-when-acquiring-and-renewing-tokens-silently
            if (window !== window.parent && !window.opener) {
                return this.operationCompleted();
            } else {
                const state = await this.retrieveState(callbackUrl);
                return this.success(state);
            }
        } catch (e) {
            if (this.isMsalError(e)) {
                return this.error(e.errorMessage);
            } else {
                return this.error(e);
            }
        }
    }

    private isMsalError(resultOrError: any): resultOrError is Msal.AuthError {
        return resultOrError?.errorCode;
    }

    private error(message: string) {
        return { status: AuthenticationResultStatus.Failure, errorMessage: message };
    }

    private success(state: any) {
        return { status: AuthenticationResultStatus.Success, state };
    }

    private redirect() {
        return { status: AuthenticationResultStatus.Redirect };
    }

    private operationCompleted() {
        return { status: AuthenticationResultStatus.OperationCompleted };
    }
}

export class AuthenticationService {

    static _infrastructureKey = 'Microsoft.AspNetCore.Components.WebAssembly.Authentication';
    static _initialized = false;
    static instance: MsalAuthorizeService;

    public static async init(settings: AuthorizeServiceConfiguration) {
        if (!AuthenticationService._initialized) {
            AuthenticationService._initialized = true;
            AuthenticationService.instance = new MsalAuthorizeService(settings);
        }
    }

    public static getUser() {
        return AuthenticationService.instance.getUser();
    }

    public static getAccessToken(request: AccessTokenRequestOptions) {
        return AuthenticationService.instance.getAccessToken(request);
    }

    public static signIn(state: any) {
        return AuthenticationService.instance.signIn(state);
    }

    // url is not used in the msal.js implementation but we keep it here
    // as it is part of the default RemoteAuthenticationService contract implementation.
    // The unused parameter here just reflects that.
    public static completeSignIn(url: string) {
        return AuthenticationService.instance.completeSignIn();
    }

    public static signOut(state: any) {
        return AuthenticationService.instance.signOut(state);
    }

    public static completeSignOut(url: string) {
        return AuthenticationService.instance.completeSignOut(url);
    }
}

declare global {
    interface Window { AuthenticationService: AuthenticationService; }
}

window.AuthenticationService = AuthenticationService;
