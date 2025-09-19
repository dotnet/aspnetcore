// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// Removed HeaderNames import to reduce bundle size; using literal key.
import { HttpClient, HttpRequest, HttpResponse } from "./HttpClient";

/** @private */
export class AccessTokenHttpClient extends HttpClient {
    private readonly _innerClient: HttpClient;
    _accessToken: string | undefined;
    _accessTokenFactory: (() => string | Promise<string>) | undefined;

    constructor(innerClient: HttpClient, accessTokenFactory: (() => string | Promise<string>) | undefined) {
        super();

        this._innerClient = innerClient;
        this._accessTokenFactory = accessTokenFactory;
    }

    public async send(request: HttpRequest): Promise<HttpResponse> {
        const needsToken = !!(this._accessTokenFactory && (!this._accessToken || (request.url && request.url.indexOf('/negotiate?') > 0)));
        const retry = !needsToken;
        if (needsToken) this._accessToken = await this._accessTokenFactory!();
        this._setAuthorizationHeader(request);
        try {
            const r = await this._innerClient.send(request);
            return (retry && this._accessTokenFactory && r.statusCode === 401) ? this._refreshTokenAndRetry(request, r) : r;
        } catch (err: unknown) {
            if (!retry || !this._accessTokenFactory) throw err;
            const e = err as any, s = +(e.statusCode ?? e.status);
            if (s === 401) return this._refreshTokenAndRetry(request, e);
            throw err;
        }
    }

    private async _refreshTokenAndRetry(request: HttpRequest, o: HttpResponse | Error): Promise<HttpResponse> {
        const t = await this._accessTokenFactory!();
        if (!t) {
            this._accessToken = undefined;
            if (request.headers) delete (request.headers as any).Authorization;
            if (request.abortSignal) return this._innerClient.send(request);
            if (o instanceof HttpResponse) return o;
            return Promise.reject(o);
        }
        this._accessToken = t;
        this._setAuthorizationHeader(request);
        return this._innerClient.send(request);
    }

    private _setAuthorizationHeader(request: HttpRequest) {
        const h = request.headers || (request.headers = {});
        if (this._accessToken) h.Authorization = 'Bearer ' + this._accessToken; else if (this._accessTokenFactory) delete h.Authorization;
    }

    public getCookieString(url: string): string {
        return this._innerClient.getCookieString(url);
    }
}