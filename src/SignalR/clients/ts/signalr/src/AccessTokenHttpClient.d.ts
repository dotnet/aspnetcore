import { HttpClient, HttpRequest, HttpResponse } from "./HttpClient";
/** @private */
export declare class AccessTokenHttpClient extends HttpClient {
    private _innerClient;
    _accessToken: string | undefined;
    _accessTokenFactory: (() => string | Promise<string>) | undefined;
    constructor(innerClient: HttpClient, accessTokenFactory: (() => string | Promise<string>) | undefined);
    send(request: HttpRequest): Promise<HttpResponse>;
    private _setAuthorizationHeader;
    getCookieString(url: string): string;
}
//# sourceMappingURL=AccessTokenHttpClient.d.ts.map