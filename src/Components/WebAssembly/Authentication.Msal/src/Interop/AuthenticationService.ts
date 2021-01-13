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
    OperationCompleted = "operationCompleted"
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
    readonly _msalApplication: Msal.UserAgentApplication;
    readonly _callbackPromise: Promise<AuthenticationResult>;

    constructor(private readonly _settings: AuthorizeServiceConfiguration) {

        // It is important that we capture the callback-url here as msal will remove the auth parameters
        // from the url as soon as it gets initialized.
        const callbackUrl = location.href;
        this._msalApplication = new Msal.UserAgentApplication(this._settings);

        // This promise will only resolve in callback-paths, which is where we check it.
        this._callbackPromise = this.createCallbackResult(callbackUrl);
    }

    async getUser() {
        const account = this._msalApplication.getAccount();
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

        const response = await this._msalApplication.acquireTokenSilent(tokenScopes);
        return {
            value: response.accessToken,
            grantedScopes: response.scopes,
            expires: response.expiresOn
        };
    }

    async signIn(state: any) {
        try {
            // Before we start any sign-in flow, clear out any previous state so that it doesn't pile up.
            this.purgeState();

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
            // If the user explicitly cancelled the pop-up, avoid performing a redirect.
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

    completeSignIn() {
        return this._callbackPromise;
    }

    async signOut(state: any) {
        // We are about to sign out, so clear any state before we do so and leave just the sign out state for
        // the current sign out flow.
        this.purgeState();

        const logoutStateId = await this.saveState(state);

        // msal.js doesn't support providing logout state, so we shim it by putting the identifier in session storage
        // and using that on the logout callback to workout the problems.
        sessionStorage.setItem(`${AuthenticationService._infrastructureKey}.LogoutState`, logoutStateId);

        this._msalApplication.logout();

        // We are about to be redirected.
        return this.redirect();
    }

    async completeSignOut(url: string) {
        const logoutStateId = sessionStorage.getItem(`${AuthenticationService._infrastructureKey}.LogoutState`);
        const updatedUrl = new URL(url);
        updatedUrl.search = `?state=${logoutStateId}`;
        const logoutState = await this.retrieveState(updatedUrl.href, /*isLogout*/ true);

        sessionStorage.removeItem(`${AuthenticationService._infrastructureKey}.LogoutState`);

        if (logoutState) {
            return this.success(logoutState);
        } else {
            return this.operationCompleted();
        }
    }

    // msal.js only allows a string as the account state and it simply attaches it to the sign-in request state.
    // Given that we don't want to serialize the entire state and put it in the query string, we need to serialize the
    // state ourselves and pass an identifier to retrieve it while in the callback flow.
    async saveState<T>(state: T): Promise<string> {
        const base64UrlIdentifier = await new Promise<string>((resolve, reject) => {
            const reader = new FileReader();
            reader.onloadend = evt => resolve((evt?.target?.result as string)
                // The result comes back as a base64 string inside a dataUrl.
                // We remove the prefix and convert it to base64url by replacing '+' with '-', '/' with '_' and removing '='.
                .split(',')[1].replace(/\+/g, '-').replace(/\//g, '_').replace(/=/g, ''));
            reader.onerror = evt => reject(evt.target?.error?.message);

            // We generate a base 64 url encoded string of random data.
            const entropy = window.crypto.getRandomValues(new Uint8Array(32));
            reader.readAsDataURL(new Blob([entropy]));
        });

        sessionStorage.setItem(`${AuthenticationService._infrastructureKey}.AuthorizeService.${base64UrlIdentifier}`, JSON.stringify(state));
        return base64UrlIdentifier;
    }

    async retrieveState<T>(url: string, isLogout: boolean = false): Promise<T | undefined> {
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

        // We need to calculate the state key in two different ways. The reason for it is that
        // msal.js doesn't support the state parameter on logout flows, which forces us to shim our own logout state.
        // The format then is different, as msal follows the pattern state=<<guid>>|<<user_state>> and our format
        // simple uses <<base64urlIdentifier>>.
        const appState = !isLogout ? this._msalApplication.getAccountState(state[0]) : state[0];
        const stateKey = `${AuthenticationService._infrastructureKey}.AuthorizeService.${appState}`;
        const stateString = sessionStorage.getItem(stateKey);
        if (stateString) {
            sessionStorage.removeItem(stateKey);
            const savedState = JSON.parse(stateString);
            return savedState;
        }

        return undefined;
    }

    purgeState() {
        for (let i = 0; i < sessionStorage.length; i++) {
            const key = sessionStorage.key(i);
            if (key?.startsWith(AuthenticationService._infrastructureKey)) {
                sessionStorage.removeItem(key);
            }
        }
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

    static _infrastructureKey = 'Microsoft.Authentication.WebAssembly.Msal';
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
