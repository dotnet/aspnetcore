// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import { HeaderNames } from "./HeaderNames";
import { HttpClient, HttpRequest, HttpResponse } from "./HttpClient";

// Internal helpers (not exported) for narrowing and status normalization
function isError(u: unknown): u is Error {
    return u instanceof Error;
}
function getStatus(u: unknown): number | undefined {
    if (typeof u !== "object" || u === null) { return undefined; }
    const rec = u as Record<string, unknown>;
    const raw = rec["statusCode"] ?? rec["status"];
    if (typeof raw === "number") { return raw; }
    if (typeof raw === "string") {
        const n = parseInt(raw, 10);
        return Number.isNaN(n) ? undefined : n;
    }
    return undefined;
}

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

        try {
            const response = await this._innerClient.send(request);

            if (allowRetry && this._accessTokenFactory && response.statusCode === 401) {
                return await this._refreshTokenAndRetry(request, response);
            }
            return response;
        } catch (err: unknown) {
            if (!allowRetry || !this._accessTokenFactory) {
                throw err;
            }
            if (!isError(err)) {
                throw err;
            }
            const status = getStatus(err);
            if (status === 401) {
                return await this._refreshTokenAndRetry(request, err);
            }
            throw err;
        }
    }

    private async _refreshTokenAndRetry(request: HttpRequest, original: HttpResponse | Error): Promise<HttpResponse> {
        if (!this._accessTokenFactory) {
            if (original instanceof HttpResponse) {
                return original;
            }
            throw original;
        }

        const newToken = await this._accessTokenFactory();
        if (!newToken) {
            if (original instanceof HttpResponse) {
                return original;
            }
            throw original;
        }
        this._accessToken = newToken;
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