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
    Success = 'success',
    RequiresRedirect = 'requiresRedirect'
}

export enum AuthenticationResultStatus {
    Redirect = 'redirect',
    Success = 'success',
    Failure = 'failure',
    OperationCompleted = 'operationCompleted'
};

export interface AuthenticationResult {
    status: AuthenticationResultStatus;
    state?: any;
    message?: string;
}

export interface AuthorizeService {
    getUser(): Promise<any>;
    getAccessToken(request?: AccessTokenRequestOptions): Promise<AccessTokenResult>;
    signIn(state: any): Promise<AuthenticationResult>;
    completeSignIn(state: any): Promise<AuthenticationResult>;
    signOut(state: any): Promise<AuthenticationResult>;
    completeSignOut(url: string): Promise<AuthenticationResult>;
}

class OidcAuthorizeService implements AuthorizeService {
    private _userManager: UserManager;

    constructor(userManager: UserManager) {
        this._userManager = userManager;
    }

    async getUser() {
        const user = await this._userManager.getUser();
        return user && user.profile;
    }

    async getAccessToken(request?: AccessTokenRequestOptions): Promise<AccessTokenResult> {
        const user = await this._userManager.getUser();
        if (hasValidAccessToken(user) && hasAllScopes(request, user.scopes)) {
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

                const newUser = await this._userManager.signinSilent(parameters);

                return {
                    status: AccessTokenResultStatus.Success,
                    token: {
                        grantedScopes: newUser.scopes,
                        expires: getExpiration(newUser.expires_in),
                        value: newUser.access_token
                    }
                };

            } catch (e) {
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
                for (let current of request.scopes) {
                    if (!set.has(current)) {
                        return false;
                    }
                }
            }

            return true;
        }
    }

    async signIn(state: any) {
        try {
            await this._userManager.clearStaleState();
            await this._userManager.signinSilent(this.createArguments());
            return this.success(state);
        } catch (silentError) {
            try {
                await this._userManager.clearStaleState();
                await this._userManager.signinRedirect(this.createArguments(state));
                return this.redirect();
            } catch (redirectError) {
                return this.error(this.getExceptionMessage(redirectError));
            }
        }
    }

    async completeSignIn(url: string) {
        const requiresLogin = await this.loginRequired(url);
        const stateExists = await this.stateExists(url);
        try {
            const user = await this._userManager.signinCallback(url);
            if (window.self !== window.top) {
                return this.operationCompleted();
            } else {
                return this.success(user && user.state);
            }
        } catch (error) {
            if (requiresLogin || window.self !== window.top || !stateExists) {
                return this.operationCompleted();
            }

            return this.error('There was an error signing in.');
        }
    }

    async signOut(state: any) {
        try {
            if (!(await this._userManager.metadataService.getEndSessionEndpoint())) {
                await this._userManager.removeUser();
                return this.success(state);
            }
            await this._userManager.signoutRedirect(this.createArguments(state));
            return this.redirect();
        } catch (redirectSignOutError) {
            return this.error(this.getExceptionMessage(redirectSignOutError));
        }
    }

    async completeSignOut(url: string) {
        try {
            if (await this.stateExists(url)) {
                const response = await this._userManager.signoutCallback(url);
                return this.success(response && response.state);
            } else {
                return this.operationCompleted();
            }
        } catch (error) {
            return this.error(this.getExceptionMessage(error));
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
        if (stateParam) {
            return await this._userManager.settings.stateStore!.get(stateParam);
        } else {
            return undefined;
        }
    }

    private async loginRequired(url: string) {
        const errorParameter = new URLSearchParams(new URL(url).search).get('error');
        if (errorParameter) {
            const error = await this._userManager.settings.stateStore!.get(errorParameter);
            return error === 'login_required';
        } else {
            return false;
        }
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
    static _initialized : Promise<void>;
    static instance: OidcAuthorizeService;

    public static async init(settings: UserManagerSettings & AuthorizeServiceSettings) {
        // Multiple initializations can start concurrently and we want to avoid that.
        // In order to do so, we create an initialization promise and the first call to init
        // tries to initialize the app and sets up a promise other calls can await on.
        if (!AuthenticationService._initialized) {
            this._initialized = new Promise(async (resolve, reject) => {
                try {
                    const userManager = await this.createUserManager(settings);
                    AuthenticationService.instance = new OidcAuthorizeService(userManager);
                    resolve();
                } catch (e) {
                    reject(e);
                }
            });
        }

        await this._initialized;
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

    private static async createUserManager(settings: OidcAuthorizeServiceSettings): Promise<UserManager> {
        let finalSettings: UserManagerSettings;
        if (isApiAuthorizationSettings(settings)) {
            let response = await fetch(settings.configurationEndpoint);
            if (!response.ok) {
                throw new Error(`Could not load settings from '${settings.configurationEndpoint}'`);
            }

            const downloadedSettings = await response.json();

            window.sessionStorage.setItem(`${AuthenticationService._infrastructureKey}.CachedAuthSettings`, JSON.stringify(settings));

            downloadedSettings.automaticSilentRenew = true;
            downloadedSettings.includeIdTokenInSilentRenew = true;

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

        const userManager = new UserManager(finalSettings);

        userManager.events.addUserSignedOut(async () => {
            await userManager.removeUser();
        });

        return userManager;
    }
}

declare global {
    interface Window { AuthenticationService: AuthenticationService; }
}

window.AuthenticationService = AuthenticationService;
