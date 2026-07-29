// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import { HttpError } from "./Errors";
import { HeaderNames } from "./HeaderNames";
import { HttpClient, HttpRequest, HttpResponse } from "./HttpClient";

/** @private */
export class AccessTokenHttpClient extends HttpClient {
    private _innerClient: HttpClient;
    _accessToken: string | undefined;
    _accessTokenFactory: (() => string | Promise<string>) | undefined;

    constructor(innerClient: HttpClient, accessTokenFactory: (() => string | Promise<string>) | undefined) {
        super();

        this._innerClient = innerClient;
        this._accessTokenFactory = accessTokenFactory;
    }

    public async send(request: HttpRequest): Promise<HttpResponse> {
        let allowRetry = true;
        if (this._accessTokenFactory && (!this._accessToken || (request.url && request.url.indexOf("/negotiate?") > 0))) {
            // don't retry if the request is a negotiate or if we just got a potentially new token from the access token factory
            allowRetry = false;
            this._accessToken = await this._accessTokenFactory();
        }
        this._setAuthorizationHeader(request);

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
        this._setAuthorizationHeader(request);
        return await this._innerClient.send(request);
    }

    private _setAuthorizationHeader(request: HttpRequest) {
        if (!request.headers) {
            request.headers = {};
        }
        if (this._accessToken) {
            request.headers[HeaderNames.Authorization] = `Bearer ${this._accessToken}`
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