import * as Msal from 'msal';
import { StringDict } from 'msal/lib-commonjs/MsalTypes';

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
    getUser(): Promise<any>;
    getAccessToken(request?: AccessTokenRequestOptions): Promise<AccessTokenResult>;
    signIn(state: any): Promise<AuthenticationResult>;
    completeSignIn(state: any): Promise<AuthenticationResult>;
    signOut(state: any): Promise<AuthenticationResult>;
    completeSignOut(url: string): Promise<AuthenticationResult>;
}

interface AuthorizeServiceConfiguration extends Msal.Configuration {
    defaultAccessTokenScopes: string[]
}

class MsalAuthorizeService implements AuthorizeService {
    _user: StringDict | null = null;
    _isAuthenticated = false;
    _scopes: string[] = [];
    _settings: AuthorizeServiceConfiguration;
    _msalApplication: Msal.UserAgentApplication;
    _callbackPromise: Promise<AuthenticationResult>;

    constructor(settings: AuthorizeServiceConfiguration) {
        this._settings = settings;
        console.log('Settings:', settings);
        this._msalApplication = new Msal.UserAgentApplication(this._settings);
        this._callbackPromise = new Promise((resolve, _) => {
            this._msalApplication.handleRedirectCallback(
                trc => {
                    if (window.self !== window.top) {
                        resolve(this.operationCompleted());
                    } else {
                        resolve(this.success(this.retrieveState(location.href)));
                    }
                },
                erc => {
                    resolve(this.error(erc.message));
                });
        });
    }

    async getUser() {
        if (this._user) {
            return this._user;
        }

        var account = this._msalApplication.getAccount();
        if (account && account.idTokenClaims) {
            this._user = account.idTokenClaims;
            return account.idTokenClaims;
        }

        return this._user;
    }

    async getAccessToken(request?: AccessTokenRequestOptions): Promise<AccessTokenResult> {
        const result = await this.getTokenCore(request?.scopes);
        if (result && hasAllScopes(request, result.grantedScopes)) {
            return {
                status: AccessTokenResultStatus.Success,
                token: result
            };
        } else {
            try {
                const scopes = request && request.scopes ?
                    request.scopes : undefined;

                const newToken = await this.getTokenCore(scopes);

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

        function hasAllScopes(request: AccessTokenRequestOptions | undefined, currentScopes: string[]) {
            const set = new Set(currentScopes);
            if (request && request.scopes) {
                for (let current of request.scopes) {
                    if (!set.has(current)) {
                        return false;
                    }
                }
            }

            return true;
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
            try {
                response = await this._msalApplication.acquireTokenPopup(tokenScopes);
            } catch (e) {
                this._msalApplication.acquireTokenRedirect(tokenScopes);
            }
        }

        return undefined;
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

            const result = await this.signInCore(request);
            if (!result) {
                return this.redirect();
            }

            let accessToken: Msal.AuthResponse | undefined = undefined;
            try {
                if (this._settings.defaultAccessTokenScopes?.length > 0) {
                    accessToken = await this._msalApplication.acquireTokenSilent(request);
                }
            } catch (e) {
                return this.error(e.message);
            }

            this.updateState(result.idTokenClaims);
            this._scopes = accessToken?.scopes || [];

            return this.success(state);
        } catch (e) {
            return this.error(e.message);
        }
    }

    async signInCore(request: Msal.AuthenticationParameters): Promise<Msal.AuthResponse | undefined> {
        try {
            const response = await this._msalApplication.loginPopup(request);
            response.idTokenClaims
            return response;
        } catch (e) {
            if (/*e.errorCode !== 'user_cancelled'*/true) {
                try {
                    this._msalApplication.loginRedirect(request);
                } catch (e) {
                    console.log(e);
                }
            } else {
                throw e;
            }
        }
    }

    async completeSignIn(url: string) {
        return this._callbackPromise;
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

    updateState(user: StringDict) {
        this._user = user;
        this._isAuthenticated = !!this._user;
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
        const params = new URL(url).searchParams;
        const state = params.getAll('state');
        if (!state || state.length !== 1) {
            return undefined;
        }

        const stateKey = `AuthorizeService.${state[0]}`;
        const stateString = sessionStorage.getItem(stateKey);
        if (stateString) {
            sessionStorage.removeItem(stateKey);
            return JSON.parse(stateString);
        }

        return undefined;
    }

    private createArguments(state?: any) {
        return { useReplaceToNavigate: true, data: state };
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

    public static getAccessToken() {
        return AuthenticationService.instance.getAccessToken();
    }

    public static signIn(state: any) {
        return AuthenticationService.instance.signIn(state);
    }

    public static completeSignIn(url: string) {
        return AuthenticationService.instance.completeSignIn(url);
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
