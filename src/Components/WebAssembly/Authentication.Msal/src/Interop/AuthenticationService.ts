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
    _user: StringDict | null = null;
    _settings: AuthorizeServiceConfiguration;
    _msalApplication: Msal.UserAgentApplication;
    _callbackPromise: Promise<AuthenticationResult>;

    constructor(settings: AuthorizeServiceConfiguration) {
        this._settings = settings;
        const callbackUrl = location.href;
        this._msalApplication = new Msal.UserAgentApplication(this._settings);
        this._callbackPromise = new Promise(this.createCallbackResult.bind(this, callbackUrl));
    }

    async getUser() {
        var account = this._msalApplication.getAccount();
        if (account && account.idTokenClaims) {
            this._user = account.idTokenClaims;
            return account.idTokenClaims;
        }

        return undefined;
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

            if (this._settings.additionalScopesToConsent && this._settings.additionalScopesToConsent.length > 0) {
                request.extraScopesToConsent = this._settings.additionalScopesToConsent;
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

    updateState(user: StringDict) {
        this._user = user;
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

    private createCallbackResult(callbackUrl: string, resolve: (result: AuthenticationResult) => void, _: (error: AuthenticationResult) => void) {
        this._msalApplication.handleRedirectCallback(
            authenticationResponse => {
                if (window.self !== window.top) {
                    resolve(this.operationCompleted());
                } else {
                    this.retrieveState(callbackUrl)
                        .then(state => {
                            resolve(this.success(state));
                        });
                }
            },
            authenticationError => {
                resolve(this.error(authenticationError.message));
            });
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
