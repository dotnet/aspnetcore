// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import { HttpError } from "./Errors";
import { HeaderNames } from "./HeaderNames";
import { HttpClient, HttpRequest, HttpResponse } from "./HttpClient";

interface AccessTokenHttpRequest extends HttpRequest {
    _negotiate?: boolean;
    _authenticationRefresh?: boolean;
}

/** @private */
export class AccessTokenHttpClient extends HttpClient {
    private _innerClient: HttpClient;
    private _refreshAccessTokenFactory: (() => string | Promise<string>) | undefined;
    private readonly _refreshRequestTokens = new WeakMap<HttpResponse, string | undefined>();
    _accessToken: string | undefined;
    _accessTokenFactory: (() => string | Promise<string>) | undefined;

    constructor(innerClient: HttpClient, accessTokenFactory: (() => string | Promise<string>) | undefined) {
        super();

        this._innerClient = innerClient;
        this._accessTokenFactory = accessTokenFactory;
        this._refreshAccessTokenFactory = accessTokenFactory;
    }

    public async send(request: HttpRequest): Promise<HttpResponse> {
        let allowRetry = true;
        const accessTokenRequest = request as AccessTokenHttpRequest;
        const isNegotiate = accessTokenRequest._negotiate === true;
        const isRefresh = accessTokenRequest._authenticationRefresh === true;
        let accessToken = isRefresh ? undefined : this._accessToken;

        if (isRefresh && this._refreshAccessTokenFactory) {
            // Refresh belongs to the application auth plane. Do not overwrite the cached transport token until the server accepts the refresh.
            allowRetry = false;
            accessToken = await this._refreshAccessTokenFactory();
        } else if (this._accessTokenFactory && (!this._accessToken || isNegotiate)) {
            // don't retry if the request is a negotiate or if we just got a potentially new token from the access token factory
            allowRetry = false;
            accessToken = await this._accessTokenFactory();
            this._accessToken = accessToken;
        }
        this._setAuthorizationHeader(request, accessToken);

        let response: HttpResponse;
        try {
            response = await this._innerClient.send(request);
        } catch (error) {
            // The inner client throws an HttpError for non-2xx responses, which prevents
            // the 401 handling below from ever seeing the status code. Surface a thrown
            // 401 here so the access token can be renewed and the request retried once.
            if (allowRetry && error instanceof HttpError && error.statusCode === 401 && this._accessTokenFactory) {
                return await this._retryWithNewToken(request);
            }
            throw error;
        }

        if (isRefresh) {
            this._refreshRequestTokens.set(response, accessToken);
        }

        if (allowRetry && response.statusCode === 401 && this._accessTokenFactory) {
            return await this._retryWithNewToken(request);
        }
        return response;
    }

    /**
     * Renews the access token via the access token factory and resends the request.
     * The retried request is intentionally not guarded against another 401 so a
     * failing retry surfaces to the caller instead of retrying indefinitely.
     */
    private async _retryWithNewToken(request: HttpRequest): Promise<HttpResponse> {
        this._accessToken = await this._accessTokenFactory!();
        this._setAuthorizationHeader(request, this._accessToken);
        return await this._innerClient.send(request);
    }

    public markNegotiateRequest(request: HttpRequest): void {
        (request as AccessTokenHttpRequest)._negotiate = true;
    }

    public markAuthenticationRefreshRequest(request: HttpRequest): void {
        (request as AccessTokenHttpRequest)._authenticationRefresh = true;
    }

    public setRefreshAccessTokenFactory(accessTokenFactory: (() => string | Promise<string>) | undefined): void {
        this._refreshAccessTokenFactory = accessTokenFactory;
    }

    public updateCachedToken(accessToken: string | undefined): void {
        this._accessToken = accessToken;
    }

    public getRefreshRequestToken(response: HttpResponse): string | undefined {
        return this._refreshRequestTokens.get(response);
    }

    private _setAuthorizationHeader(request: HttpRequest, accessToken: string | undefined) {
        if (!request.headers) {
            request.headers = {};
        }
        if (accessToken) {
            request.headers[HeaderNames.Authorization] = `Bearer ${accessToken}`;
        }
        // don't remove the header if there isn't an access token factory, the user manually added the header in this case
        else if (this._accessTokenFactory) {
            if (request.headers[HeaderNames.Authorization]) {
                delete request.headers[HeaderNames.Authorization];
            }
        }
    }

    public getCookieString(url: string): string {
        return this._innerClient.getCookieString(url);
    }
}