import * as Msal from '@azure/msal-browser';

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
    expires: Date | null;
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
    getUser(): Promise<object | undefined>;
    getAccessToken(request?: AccessTokenRequestOptions): Promise<AccessTokenResult>;
    signIn(state: any): Promise<AuthenticationResult>;
    completeSignIn(state: any): Promise<AuthenticationResult>;
    signOut(state: any): Promise<AuthenticationResult>;
    completeSignOut(url: string): Promise<AuthenticationResult>;
}

// These are the values for the .NET logger LogLevel. 
// We only use debug and trace
export enum LogLevel {
    Trace = 0,
    Debug = 1
}

interface JavaScriptLoggingOptions {
    debugEnabled: boolean;
    traceEnabled: boolean;
}

export class Logger {
    public debug: boolean;
    public trace: boolean;
    public constructor(options: JavaScriptLoggingOptions) {
        this.debug = options.debugEnabled;
        this.trace = options.traceEnabled;
    }

    log(level: LogLevel, message: string): void {
        if ((level == LogLevel.Trace && this.trace) ||
            (level == LogLevel.Debug && this.debug)) {
            const levelString = level == LogLevel.Trace ? 'trce' : 'dbug';
            console.debug(
                // Logs in the following format to keep consistency with the way ASP.NET Core logs to the console while avoiding the
                // additional overhead of passing the logger as a JSObjectReference
                // dbug: Microsoft.AspNetCore.Components.WebAssembly.Authentication.RemoteAuthenticationService[0]
                //       <<message>>         
                // trce: Microsoft.AspNetCore.Components.WebAssembly.Authentication.RemoteAuthenticationService[0]
                //       <<message>>
                `${levelString}: Microsoft.AspNetCore.Components.WebAssembly.Authentication.RemoteAuthenticationService[0]
      ${message}`);
        }
    }
}

export interface AuthenticationContext {
    state?: unknown;
    interactiveRequest: InteractiveAuthenticationRequest;
}

export interface InteractiveAuthenticationRequest {
    interaction: string;
    scopes?: string[];
    additionalRequestParameters?: { [key: string]: any };
};

interface AuthorizeServiceConfiguration extends Msal.Configuration {
    defaultAccessTokenScopes: string[];
    additionalScopesToConsent: string[];
    loginMode: string;
}

class MsalAuthorizeService implements AuthorizeService {
    private readonly _msalApplication: Msal.PublicClientApplication;
    private _account: Msal.AccountInfo | null | undefined;
    private _redirectCallback: Promise<AuthenticationResult | null> | undefined;

    constructor(private readonly _settings: AuthorizeServiceConfiguration, private readonly _logger: Logger) {
        if (this._settings.auth?.knownAuthorities?.length == 0) {
            this._settings.auth.knownAuthorities = [new URL(this._settings.auth.authority!).hostname]
        }

        this._settings.system = this._settings.system || {};

        this._settings.system.navigationClient = {
            async navigateInternal(url: string, options: Msal.NavigationOptions): Promise<boolean> {
                // We always replace the URL
                _logger.log(LogLevel.Trace, `Navigating to ${url}`);
                location.replace(url);
                return false;
            },
            async navigateExternal(url: string, options: Msal.NavigationOptions): Promise<boolean> {
                // We always replace the URL
                _logger.log(LogLevel.Trace, `Navigating to ${url}`);
                location.replace(url);
                return false;
            }
        }

        this._settings.system.loggerOptions = {
            logLevel: _logger.trace ? Msal.LogLevel.Trace : (_logger.debug ? Msal.LogLevel.Verbose : Msal.LogLevel.Warning),
            loggerCallback: (level, message, containsPii) => {
                if (containsPii) {
                    return;
                }
                if (level === Msal.LogLevel.Trace) {
                    _logger.log(LogLevel.Trace, message);
                    return;
                }

                // We only have Debug/Trace, so anything above, we only log when Debug is enabled.
                _logger.log(LogLevel.Debug, message);
                return;
            }
        };

        this._msalApplication = new Msal.PublicClientApplication(this._settings);
    }

    getAccount() {
        if (this._account) {
            return this._account;
        }

        const accounts = this._msalApplication.getAllAccounts();
        if (accounts && accounts.length) {
            return accounts[0];
        }

        return null;
    }

    async getUser() {
        const account = this.getAccount();
        if (!account) {
            return;
        }

        return account.idTokenClaims;
    }

    async getAccessToken(request?: AccessTokenRequestOptions): Promise<AccessTokenResult> {
        try {
            this.trace('getAccessToken', request);
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
        const account = this.getAccount();
        if (!account) {
            throw new Error('Failed to retrieve token, no account found.');
        }

        const silentRequest = {
            redirectUri: this._settings.auth?.redirectUri,
            account: account,
            scopes: scopes || this._settings.defaultAccessTokenScopes
        };

        this.debug(`Provisioning a token silently for scopes '${silentRequest.scopes}'`)
        this.trace('_msalApplication.acquireTokenSilent', silentRequest);
        const response = await this._msalApplication.acquireTokenSilent(silentRequest);
        this.trace('_msalApplication.acquireTokenSilent-response', response);

        if (response.scopes.length === 0 || response.accessToken === '') {
            throw new Error('Scopes not granted.');
        }

        const result = {
            value: response.accessToken,
            grantedScopes: response.scopes,
            expires: response.expiresOn
        };

        this.trace('getAccessToken-result', result);

        return result;
    }

    async signIn(context: AuthenticationContext) {
        this.trace('signIn', context);
        try {
            // Before we start any sign-in flow, clear out any previous state so that it doesn't pile up.
            this.purgeState();

            const { state, interactiveRequest } = context;

            if (interactiveRequest && interactiveRequest.interaction === 'GetToken') {
                this.debug('Acquiring additional token.');
                const request: Msal.RedirectRequest = {
                    scopes: interactiveRequest.scopes || [],
                    state: this.saveState(context.state),
                    ...interactiveRequest.additionalRequestParameters
                };
                this.trace('getInteractiveToken-Request', request);
                await this._msalApplication.acquireTokenRedirect(request);
                return this.success(state);
            } else {
                const request: Partial<Msal.AuthorizationUrlRequest> = {
                    redirectUri: this._settings.auth.redirectUri!,
                    state: this.saveState(context.state),
                    ...interactiveRequest?.additionalRequestParameters
                };

                request.scopes = request.scopes || this._settings.defaultAccessTokenScopes || [];

                const result = await this.signInCore(request);
                this.trace('signIn-Response', result);
                if (!result) {
                    return this.redirect();
                } else if (this.isMsalError(result)) {
                    return this.error(result.errorMessage);
                }

                return this.success(state);
            }
        } catch (e) {
            const message = (e as Error).message;
            this.debug(`Sign in error '${message}'`);
            return this.error(message);
        }
    }

    async signInCore(request: Partial<Msal.AuthorizationUrlRequest>): Promise<Msal.AuthenticationResult | Msal.AuthError | undefined> {
        this.trace('signIn-Request', request);
        const loginMode = this._settings.loginMode.toLowerCase();
        if (loginMode === 'redirect') {
            return this.signInWithRedirect(request as Msal.RedirectRequest);
        } else {
            return this.signInWithPopup(request as Msal.PopupRequest);
        }
    }

    private async signInWithRedirect(request: Msal.RedirectRequest) {
        try {
            this.debug('Starting sign-in redirect.');
            return await this._msalApplication.loginRedirect(request);
        } catch (e) {
            this.debug(`Sign-in redirect failed: '${(e as Error).message}'.`);
            return e as any;
        }
    }

    private async signInWithPopup(request: Msal.PopupRequest) {
        try {
            this.debug('Starting sign-in pop-up');
            return await this._msalApplication.loginPopup(request);
        } catch (e) {
            // If the user explicitly cancelled the pop-up, avoid performing a redirect.
            if (this.isMsalError(e) && e.errorCode !== Msal.BrowserAuthErrorMessage.userCancelledError.code) {
                this.debug('User canceled sign-in pop-up');
                this.signInWithRedirect(request);
            } else {
                this.debug(`Sign-in pop-up failed: '${(e as Error).message}'.`);
                return e as any;
            }
        }
    }

    async completeSignIn() {
        // Make sure that the redirect handler has completed execution before
        // completing sign in.
        try {
            this.debug('Completing sign-in redirect.');
            var authenticationResult = await this._redirectCallback;
            this.trace('completeSignIn-result', authenticationResult);
            if (authenticationResult) {
                this.trace('completeSignIn-success', authenticationResult);
                return authenticationResult;
            }
            this.debug('No authentication result.');
            return this.operationCompleted();
        } catch (e) {
            this.debug(`completeSignIn-error:'${(e as Error).message}'`);
            return this.error((e as Error).message);
        }
    }

    async signOut(context: AuthenticationContext) {
        this.trace('signOut', context);
        try {
            // Before we start any sign-in flow, clear out any previous state so that it doesn't pile up.
            this.purgeState();

            const { state, interactiveRequest } = context;

            const request: Partial<Msal.EndSessionRequest> = {
                postLogoutRedirectUri: this._settings.auth.postLogoutRedirectUri,
                state: this.saveState(state),
                ...interactiveRequest?.additionalRequestParameters
            };
            this.trace('signOut-Request', request);

            await this._msalApplication.logoutRedirect(request);

            // We are about to be redirected.
            return this.redirect();
        } catch (e) {
            return this.error((e as Error).message);
        }
    }

    async completeSignOut(url: string) {
        this.trace('completeSignOut-request', url);
        try {
            this.debug('Completing sign-out redirect.');
            var authenticationResult = await this._redirectCallback;
            this.trace('completeSignOut-result', authenticationResult);
            if (authenticationResult) {
                this.trace('completeSignOut-success', authenticationResult);
                return authenticationResult;
            }
            this.debug('No authentication result.');
            return this.operationCompleted();
        } catch (e) {
            this.debug(`completeSignOut-error:'${(e as Error).message}'`);
            return this.error((e as Error).message);
        }
    }

    // msal.js only allows a string as the account state and it simply attaches it to the sign-in request state.
    // Given that we don't want to serialize the entire state and put it in the query string, we need to serialize the
    // state ourselves and pass an identifier to retrieve it while in the callback flow.
    saveState<T>(state: T): string {
        const identifier = window.crypto.randomUUID();
        sessionStorage.setItem(`${AuthenticationService._infrastructureKey}.AuthorizeService.${identifier}`, JSON.stringify(state));
        return identifier;
    }

    retrieveState<T>(url: string | null, providedState: string | null = null, isLogout: boolean = false): T | undefined {
        let stateFromUrl;
        // Parse the state key from the `search` query parameter in the URL if provided
        if (url) {
            const parsedUrl = new URL(url);
            stateFromUrl = parsedUrl.searchParams && parsedUrl.searchParams.getAll('state');
        }

        // Chose  the provided state from MSAL. Otherwise, choose the state computed from the URL
        const state = providedState || stateFromUrl;

        if (!state) {
            return undefined;
        }

        const stateKey = `${AuthenticationService._infrastructureKey}.AuthorizeService.${state}`;
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

    initializeMsalHandler() {
        this._redirectCallback = this.completeAuthentication();
    }

    private async completeAuthentication() {
        try {
            const result = await this._msalApplication.handleRedirectPromise();
            const res = this.handleResult(result);
            return res;
        } catch (error) {
            if (this.isMsalError(error)) {
                return this.error(error.errorMessage);
            } else {
                return this.error((error as Error).message);
            }
        };
    }

    private handleResult(result: Msal.AuthenticationResult | null) {
        const logoutState = this.retrieveState(location.href, undefined);
        if (result) {
            this._account = result.account;
            return this.success(this.retrieveState(null, result.state));
        } else if (logoutState) {
            return this.success(logoutState);
        } else {
            return this.operationCompleted();
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

    private debug(message: string) {
        this._logger?.log(LogLevel.Debug, message);
    }

    private trace(message: string, data: any) {
        this._logger?.log(LogLevel.Trace, `${message}: ${JSON.stringify(data)}`);
    }
}

export class AuthenticationService {

    static _infrastructureKey = 'Microsoft.Authentication.WebAssembly.Msal';
    static _initialized: boolean;
    static instance: MsalAuthorizeService;

    public static async init(settings: AuthorizeServiceConfiguration, jsLoggingOptions: JavaScriptLoggingOptions) {
        if (!AuthenticationService._initialized) {
            AuthenticationService.instance = new MsalAuthorizeService(settings, new Logger(jsLoggingOptions));
            AuthenticationService.instance.initializeMsalHandler();
            AuthenticationService._initialized = true;
        }
        return Promise.resolve();
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
