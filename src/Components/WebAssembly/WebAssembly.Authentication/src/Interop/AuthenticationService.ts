import { UserManager, UserManagerSettings, User } from 'oidc-client'

type Writeable<T> = { -readonly [P in keyof T]: T[P] };

type ExtendedUserManagerSettings = Writeable<UserManagerSettings & AuthorizeServiceSettings>

type OidcAuthorizeServiceSettings = ExtendedUserManagerSettings | ApiAuthorizationSettings;

function isApiAuthorizationSettings(settings: OidcAuthorizeServiceSettings): settings is ApiAuthorizationSettings {
    return settings.hasOwnProperty('configurationEndpoint');
}

interface AuthorizeServiceSettings {
    defaultScopes: string[];
}

interface ApiAuthorizationSettings {
    configurationEndpoint: string;
}

export interface AccessTokenRequestOptions {
    scopes: string[];
    returnUrl: string;
}

export interface AccessTokenResult {
    status: AccessTokenResultStatus;
    token?: AccessToken;
}

export interface AccessToken {
    value: string;
    expires: Date;
    grantedScopes: string[];
}

export enum AccessTokenResultStatus {
    Success = 'Success',
    RequiresRedirect = 'RequiresRedirect'
}

export enum AuthenticationResultStatus {
    Redirect = 'Redirect',
    Success = 'Success',
    Failure = 'Failure',
    OperationCompleted = 'OperationCompleted'
};

export interface AuthenticationResult {
    status: AuthenticationResultStatus;
    state?: unknown;
    message?: string;
}

export interface AuthenticationContext {
    state?: unknown;
    interactiveRequest: InteractiveAuthenticationRequest;
}

export interface InteractiveAuthenticationRequest {
    scopes?: string[];
    additionalRequestParameters?: { [key: string]: any };
};

export interface AuthorizeService {
    getUser(): Promise<unknown>;
    getAccessToken(request?: AccessTokenRequestOptions): Promise<AccessTokenResult>;
    signIn(context: AuthenticationContext): Promise<AuthenticationResult>;
    completeSignIn(state: unknown): Promise<AuthenticationResult>;
    signOut(context: AuthenticationContext): Promise<AuthenticationResult>;
    completeSignOut(url: string): Promise<AuthenticationResult>;
}

interface JavaScriptLoggingOptions {
    debugEnabled: boolean;
    traceEnabled: boolean;
}

export class ManagedLogger {
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

// These are the values for the .NET logger LogLevel. 
// We only use debug and trace
export enum LogLevel {
    Trace = 0,
    Debug = 1
}

class OidcAuthorizeService implements AuthorizeService {
    private _userManager: UserManager;
    private _logger: ManagedLogger | undefined;
    private _intialSilentSignIn: Promise<void> | undefined;
    constructor(userManager: UserManager, logger?: ManagedLogger) {
        this._userManager = userManager;
        this._logger = logger;
    }

    async trySilentSignIn() {
        if (!this._intialSilentSignIn) {
            this._intialSilentSignIn = (async () => {
                try {
                    this.debug('Beginning initial silent sign in.');
                    await this._userManager.signinSilent();
                    this.debug('Initial silent sign in succeeded.');
                } catch (e) {
                    if (e instanceof Error) {
                        this.debug(`Initial silent sign in failed '${e.message}'`);
                    }
                    // It is ok to swallow the exception here.
                    // The user might not be logged in and in that case it
                    // is expected for signinSilent to fail and throw
                }
            })();
        }

        return this._intialSilentSignIn;
    }

    async getUser() {
        if (window.parent === window && !window.opener && !window.frameElement && this._userManager.settings.redirect_uri &&
            !location.href.startsWith(this._userManager.settings.redirect_uri)) {
            // If we are not inside a hidden iframe, try authenticating silently.
            await this.trySilentSignIn();
        }

        const user = await this._userManager.getUser();
        return user && user.profile;
    }

    async getAccessToken(request?: AccessTokenRequestOptions): Promise<AccessTokenResult> {
        this.trace('getAccessToken', request);
        const user = await this._userManager.getUser();
        if (hasValidAccessToken(user) && hasAllScopes(request, user.scopes)) {
            this.debug(`Valid access token present expiring at '${getExpiration(user.expires_in).toISOString()}'`)
            return {
                status: AccessTokenResultStatus.Success,
                token: {
                    grantedScopes: user.scopes,
                    expires: getExpiration(user.expires_in),
                    value: user.access_token
                }
            };
        } else {
            try {
                const parameters = request && request.scopes ?
                    { scope: request.scopes.join(' ') } : undefined;

                this.debug(`Provisioning a token silently for scopes '${parameters?.scope}'`)
                this.trace('userManager.signinSilent', parameters);
                const newUser = await this._userManager.signinSilent(parameters);

                this.debug(`Provisioned an access token expiring at '${getExpiration(newUser.expires_in).toISOString()}'`)

                const result = {
                    status: AccessTokenResultStatus.Success,
                    token: {
                        grantedScopes: newUser.scopes,
                        expires: getExpiration(newUser.expires_in),
                        value: newUser.access_token
                    }
                };

                this.trace('getAccessToken-result', result);
                return result;

            } catch (e) {
                if (e instanceof Error) {
                    this.debug(`Failed to provision a token silently '${e.message}'`)
                }

                return {
                    status: AccessTokenResultStatus.RequiresRedirect
                };
            }
        }

        function hasValidAccessToken(user: User | null): user is User {
            return !!(user && user.access_token && !user.expired && user.scopes);
        }

        function getExpiration(expiresIn: number) {
            const now = new Date();
            now.setTime(now.getTime() + expiresIn * 1000);
            return now;
        }

        function hasAllScopes(request: AccessTokenRequestOptions | undefined, currentScopes: string[]) {
            const set = new Set(currentScopes);
            if (request && request.scopes) {
                for (const current of request.scopes) {
                    if (!set.has(current)) {
                        return false;
                    }
                }
            }

            return true;
        }
    }

    async signIn(context: AuthenticationContext) {
        this.trace('signIn', context);
        if (!context.interactiveRequest) {
            try {
                this.debug('Silent sign in starting');
                await this._userManager.clearStaleState();
                await this._userManager.signinSilent(this.createArguments(undefined, context.interactiveRequest));
                this.debug('Silent sign in succeeded');
                return this.success(context.state);
            } catch (silentError) {
                if (silentError instanceof Error) {
                    this.debug(`Silent sign in failed, redirecting to the identity provider '${silentError.message}'.`);
                }
                return await this.signInInteractive(context);
            }
        } else {
            this.debug('Interactive sign in starting.');
            return this.signInInteractive(context);
        }
    }

    async signInInteractive(context: AuthenticationContext) {
        this.trace('signInInteractive', context);
        try {
            await this._userManager.clearStaleState();
            await this._userManager.signinRedirect(this.createArguments(context.state, context.interactiveRequest));
            this.debug('Redirect sign in succeeded');
            return this.redirect();
        } catch (redirectError) {
            const message = this.getExceptionMessage(redirectError);
            this.debug(`Redirect sign in failed '${message}'.`);
            return this.error(message);
        }
    }

    async completeSignIn(url: string) {
        this.trace('completeSignIn', url);
        const requiresLogin = await this.loginRequired(url);
        const stateExists = await this.stateExists(url);
        try {
            const user = await this._userManager.signinCallback(url);
            if (window.self !== window.top) {
                return this.operationCompleted();
            } else {
                this.trace('completeSignIn-result', user);
                return this.success(user && user.state);
            }
        } catch (error) {
            if (requiresLogin || window.self !== window.top || !stateExists) {
                return this.operationCompleted();
            }

            return this.error('There was an error signing in.');
        }
    }

    async signOut(context: AuthenticationContext) {
        this.trace('signOut', context);
        try {
            if (!(await this._userManager.metadataService.getEndSessionEndpoint())) {
                await this._userManager.removeUser();
                return this.success(context.state);
            }
            await this._userManager.signoutRedirect(this.createArguments(context.state, context.interactiveRequest));
            return this.redirect();
        } catch (redirectSignOutError) {
            const message = this.getExceptionMessage(redirectSignOutError);
            this.debug(`Sign out error '${message}'.`);
            return this.error(message);
        }
    }

    async completeSignOut(url: string) {
        this.trace('completeSignOut', url);
        try {
            if (await this.stateExists(url)) {
                const response = await this._userManager.signoutCallback(url);
                return this.success(response && response.state);
            } else {
                return this.operationCompleted();
            }
        } catch (error) {
            const message = this.getExceptionMessage(error);
            this.debug(`Complete sign out error '${message}'`);
            return this.error(message);
        }
    }

    private getExceptionMessage(error: any) {
        if (isOidcError(error)) {
            return error.error_description;
        } else if (isRegularError(error)) {
            return error.message;
        } else {
            return error.toString();
        }

        function isOidcError(error: any): error is (Oidc.SigninResponse & Oidc.SignoutResponse) {
            return error && error.error_description;
        }

        function isRegularError(error: any): error is Error {
            return error && error.message;
        }
    }

    private async stateExists(url: string) {
        const stateParam = new URLSearchParams(new URL(url).search).get('state');
        if (stateParam && this._userManager.settings.stateStore) {
            return await this._userManager.settings.stateStore.get(stateParam);
        } else {
            return undefined;
        }
    }

    private async loginRequired(url: string) {
        const errorParameter = new URLSearchParams(new URL(url).search).get('error');
        if (errorParameter && this._userManager.settings.stateStore) {
            const error = await this._userManager.settings.stateStore.get(errorParameter);
            return error === 'login_required';
        } else {
            return false;
        }
    }

    private createArguments(state: unknown, interactiveRequest: InteractiveAuthenticationRequest | undefined) {
        return {
            useReplaceToNavigate: true,
            data: state,
            scope: interactiveRequest?.scopes ? interactiveRequest.scopes.join(' ') : undefined,
            ...interactiveRequest?.additionalRequestParameters
        };
    }

    private error(message: string) {
        return { status: AuthenticationResultStatus.Failure, errorMessage: message };
    }

    private success(state: unknown) {
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

    static _infrastructureKey = 'Microsoft.AspNetCore.Components.WebAssembly.Authentication';
    static _initialized: Promise<void>;
    static instance: OidcAuthorizeService;
    static _pendingOperations: { [key: string]: Promise<AuthenticationResult> | undefined } = {}

    public static init(settings: UserManagerSettings & AuthorizeServiceSettings, logger: any) {
        // Multiple initializations can start concurrently and we want to avoid that.
        // In order to do so, we create an initialization promise and the first call to init
        // tries to initialize the app and sets up a promise other calls can await on.
        if (!AuthenticationService._initialized) {
            AuthenticationService._initialized = AuthenticationService.initializeCore(settings, new ManagedLogger(logger));
        }

        return AuthenticationService._initialized;
    }

    public static handleCallback() {
        return AuthenticationService.initializeCore();
    }

    private static async initializeCore(settings?: UserManagerSettings & AuthorizeServiceSettings, logger?: ManagedLogger) {
        const finalSettings = settings || AuthenticationService.resolveCachedSettings();
        const cachedLoggerOptions = AuthenticationService.resolveCachedLoggerOptions();
        const finalLogger = logger || (cachedLoggerOptions && new ManagedLogger(cachedLoggerOptions))
        if (!settings && finalSettings && !logger && finalLogger) {
            const userManager = AuthenticationService.createUserManagerCore(finalSettings);

            if (window.parent !== window && !window.opener && (window.frameElement && userManager.settings.redirect_uri &&
                location.href.startsWith(userManager.settings.redirect_uri))) {
                // If we are inside a hidden iframe, try completing the sign in early.
                // This prevents loading the blazor app inside a hidden iframe, which speeds up the authentication operations
                // and avoids wasting resources (CPU and memory from bootstrapping the Blazor app)
                AuthenticationService.instance = new OidcAuthorizeService(userManager, finalLogger);

                // This makes sure that if the blazor app has time to load inside the hidden iframe,
                // it is not able to perform another auth operation until this operation has completed.
                AuthenticationService._initialized = (async (): Promise<void> => {
                    await AuthenticationService.instance.completeSignIn(location.href);
                    return;
                })();
            }
        } else if (settings && logger) {
            const userManager = await AuthenticationService.createUserManager(settings);
            AuthenticationService.instance = new OidcAuthorizeService(userManager, logger);
            window.sessionStorage.setItem(
                `${AuthenticationService._infrastructureKey}.CachedJSLoggingOptions`,
                JSON.stringify({debugEnabled: logger.debug, traceEnabled: logger.trace}));
        } else {
            // HandleCallback gets called unconditionally, so we do nothing for normal paths.
            // Cached settings are only used on handling the redirect_uri path and if the settings are not there
            // the app will fallback to the default logic for handling the redirect.
        }
    }

    private static resolveCachedSettings(): UserManagerSettings | undefined {
        const cachedSettings = window.sessionStorage.getItem(`${AuthenticationService._infrastructureKey}.CachedAuthSettings`);
        return cachedSettings ? JSON.parse(cachedSettings) : undefined;
    }

    private static resolveCachedLoggerOptions(): JavaScriptLoggingOptions | undefined {
        const cachedSettings = window.sessionStorage.getItem(`${AuthenticationService._infrastructureKey}.CachedJSLoggingOptions`);
        return cachedSettings ? JSON.parse(cachedSettings) : undefined;
    }

    public static getUser() {
        return AuthenticationService.instance.getUser();
    }

    public static getAccessToken(options: AccessTokenRequestOptions) {
        return AuthenticationService.instance.getAccessToken(options);
    }

    public static signIn(context: AuthenticationContext) {
        return AuthenticationService.instance.signIn(context);
    }

    public static async completeSignIn(url: string) {
        let operation = this._pendingOperations[url];
        if (!operation) {
            operation = AuthenticationService.instance.completeSignIn(url);
            await operation;
            delete this._pendingOperations[url];
        }

        return operation;
    }

    public static signOut(context: AuthenticationContext) {
        return AuthenticationService.instance.signOut(context);
    }

    public static async completeSignOut(url: string) {
        let operation = this._pendingOperations[url];
        if (!operation) {
            operation = AuthenticationService.instance.completeSignOut(url);
            await operation;
            delete this._pendingOperations[url];
        }

        return operation;
    }

    private static async createUserManager(settings: OidcAuthorizeServiceSettings): Promise<UserManager> {
        let finalSettings: UserManagerSettings;
        if (isApiAuthorizationSettings(settings)) {
            const response = await fetch(settings.configurationEndpoint);
            if (!response.ok) {
                throw new Error(`Could not load settings from '${settings.configurationEndpoint}'`);
            }

            const downloadedSettings = await response.json();

            finalSettings = downloadedSettings;
        } else {
            if (!settings.scope) {
                settings.scope = settings.defaultScopes.join(' ');
            }

            if (settings.response_type === null) {
                // If the response type is not set, it gets serialized as null. OIDC-client behaves differently than when the value is undefined, so we explicitly check for a null value and remove the property instead.
                delete settings.response_type;
            }

            finalSettings = settings;
        }

        window.sessionStorage.setItem(`${AuthenticationService._infrastructureKey}.CachedAuthSettings`, JSON.stringify(finalSettings));

        return AuthenticationService.createUserManagerCore(finalSettings);
    }

    private static createUserManagerCore(finalSettings: UserManagerSettings) {
        const userManager = new UserManager(finalSettings);
        userManager.events.addUserSignedOut(async () => {
            userManager.removeUser();
        });
        return userManager;
    }
}

declare global {
    interface Window { AuthenticationService: AuthenticationService }
}

AuthenticationService.handleCallback();

window.AuthenticationService = AuthenticationService;
