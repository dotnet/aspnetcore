// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

import { AbortSignal } from "./AbortController";
import { HttpError, TimeoutError } from "./Errors";
import { ILogger, LogLevel } from "./ILogger";

/** Represents an HTTP request. */
export interface HttpRequest {
    /** The HTTP method to use for the request. */
    method?: string;

    /** The URL for the request. */
    url?: string;

    /** The body content for the request. May be a string or an ArrayBuffer (for binary data). */
    content?: string | ArrayBuffer;

    /** An object describing headers to apply to the request. */
    headers?: { [key: string]: string };

    /** The XMLHttpRequestResponseType to apply to the request. */
    responseType?: XMLHttpRequestResponseType;

    /** An AbortSignal that can be monitored for cancellation. */
    abortSignal?: AbortSignal;

    /** The time to wait for the request to complete before throwing a TimeoutError. Measured in milliseconds. */
    timeout?: number;
}

/** Represents an HTTP response. */
export class HttpResponse {
    /** Constructs a new instance of {@link @aspnet/signalr.HttpResponse} with the specified status code.
     *
     * @param {number} statusCode The status code of the response.
     */
    constructor(statusCode: number);

    /** Constructs a new instance of {@link @aspnet/signalr.HttpResponse} with the specified status code and message.
     *
     * @param {number} statusCode The status code of the response.
     * @param {string} statusText The status message of the response.
     */
    constructor(statusCode: number, statusText: string);

    /** Constructs a new instance of {@link @aspnet/signalr.HttpResponse} with the specified status code, message and string content.
     *
     * @param {number} statusCode The status code of the response.
     * @param {string} statusText The status message of the response.
     * @param {string} content The content of the response.
     */
    constructor(statusCode: number, statusText: string, content: string);

    /** Constructs a new instance of {@link @aspnet/signalr.HttpResponse} with the specified status code, message and binary content.
     *
     * @param {number} statusCode The status code of the response.
     * @param {string} statusText The status message of the response.
     * @param {ArrayBuffer} content The content of the response.
     */
    constructor(statusCode: number, statusText: string, content: ArrayBuffer);
    constructor(
        public readonly statusCode: number,
        public readonly statusText?: string,
        public readonly content?: string | ArrayBuffer) {
    }
}

/** Abstraction over an HTTP client.
 *
 * This class provides an abstraction over an HTTP client so that a different implementation can be provided on different platforms.
 */
export abstract class HttpClient {
    /** Issues an HTTP GET request to the specified URL, returning a Promise that resolves with an {@link @aspnet/signalr.HttpResponse} representing the result.
     *
     * @param {string} url The URL for the request.
     * @returns {Promise<HttpResponse>} A Promise that resolves with an {@link @aspnet/signalr.HttpResponse} describing the response, or rejects with an Error indicating a failure.
     */
    public get(url: string): Promise<HttpResponse>;

    /** Issues an HTTP GET request to the specified URL, returning a Promise that resolves with an {@link @aspnet/signalr.HttpResponse} representing the result.
     *
     * @param {string} url The URL for the request.
     * @param {HttpRequest} options Additional options to configure the request. The 'url' field in this object will be overridden by the url parameter.
     * @returns {Promise<HttpResponse>} A Promise that resolves with an {@link @aspnet/signalr.HttpResponse} describing the response, or rejects with an Error indicating a failure.
     */
    public get(url: string, options: HttpRequest): Promise<HttpResponse>;
    public get(url: string, options?: HttpRequest): Promise<HttpResponse> {
        return this.send({
            ...options,
            method: "GET",
            url,
        });
    }

    /** Issues an HTTP POST request to the specified URL, returning a Promise that resolves with an {@link @aspnet/signalr.HttpResponse} representing the result.
     *
     * @param {string} url The URL for the request.
     * @returns {Promise<HttpResponse>} A Promise that resolves with an {@link @aspnet/signalr.HttpResponse} describing the response, or rejects with an Error indicating a failure.
     */
    public post(url: string): Promise<HttpResponse>;

    /** Issues an HTTP POST request to the specified URL, returning a Promise that resolves with an {@link @aspnet/signalr.HttpResponse} representing the result.
     *
     * @param {string} url The URL for the request.
     * @param {HttpRequest} options Additional options to configure the request. The 'url' field in this object will be overridden by the url parameter.
     * @returns {Promise<HttpResponse>} A Promise that resolves with an {@link @aspnet/signalr.HttpResponse} describing the response, or rejects with an Error indicating a failure.
     */
    public post(url: string, options: HttpRequest): Promise<HttpResponse>;
    public post(url: string, options?: HttpRequest): Promise<HttpResponse> {
        return this.send({
            ...options,
            method: "POST",
            url,
        });
    }

    /** Issues an HTTP DELETE request to the specified URL, returning a Promise that resolves with an {@link @aspnet/signalr.HttpResponse} representing the result.
     *
     * @param {string} url The URL for the request.
     * @returns {Promise<HttpResponse>} A Promise that resolves with an {@link @aspnet/signalr.HttpResponse} describing the response, or rejects with an Error indicating a failure.
     */
    public delete(url: string): Promise<HttpResponse>;

    /** Issues an HTTP DELETE request to the specified URL, returning a Promise that resolves with an {@link @aspnet/signalr.HttpResponse} representing the result.
     *
     * @param {string} url The URL for the request.
     * @param {HttpRequest} options Additional options to configure the request. The 'url' field in this object will be overridden by the url parameter.
     * @returns {Promise<HttpResponse>} A Promise that resolves with an {@link @aspnet/signalr.HttpResponse} describing the response, or rejects with an Error indicating a failure.
     */
    public delete(url: string, options: HttpRequest): Promise<HttpResponse>;
    public delete(url: string, options?: HttpRequest): Promise<HttpResponse> {
        return this.send({
            ...options,
            method: "DELETE",
            url,
        });
    }

    /** Issues an HTTP request to the specified URL, returning a {@link Promise} that resolves with an {@link @aspnet/signalr.HttpResponse} representing the result.
     *
     * @param {HttpRequest} request An {@link @aspnet/signalr.HttpRequest} describing the request to send.
     * @returns {Promise<HttpResponse>} A Promise that resolves with an HttpResponse describing the response, or rejects with an Error indicating a failure.
     */
    public abstract send(request: HttpRequest): Promise<HttpResponse>;
}

/** Default implementation of {@link @aspnet/signalr.HttpClient}. */
export class DefaultHttpClient extends HttpClient {
    private readonly logger: ILogger;

    /** Creates a new instance of the {@link @aspnet/signalr.DefaultHttpClient}, using the provided {@link @aspnet/signalr.ILogger} to log messages. */
    public constructor(logger: ILogger) {
        super();
        this.logger = logger;
    }

    /** @inheritDoc */
    public send(request: HttpRequest): Promise<HttpResponse> {
        return new Promise<HttpResponse>((resolve, reject) => {
            const xhr = new XMLHttpRequest();

            xhr.open(request.method, request.url, true);
            xhr.withCredentials = true;
            xhr.setRequestHeader("X-Requested-With", "XMLHttpRequest");
            // Explicitly setting the Content-Type header for React Native on Android platform.
            xhr.setRequestHeader("Content-Type", "text/plain;charset=UTF-8");

            if (request.headers) {
                Object.keys(request.headers)
                    .forEach((header) => xhr.setRequestHeader(header, request.headers[header]));
            }

            if (request.responseType) {
                xhr.responseType = request.responseType;
            }

            if (request.abortSignal) {
                request.abortSignal.onabort = () => {
                    xhr.abort();
                };
            }

            if (request.timeout) {
                xhr.timeout = request.timeout;
            }

            xhr.onload = () => {
                if (request.abortSignal) {
                    request.abortSignal.onabort = null;
                }

                if (xhr.status >= 200 && xhr.status < 300) {
                    resolve(new HttpResponse(xhr.status, xhr.statusText, xhr.response || xhr.responseText));
                } else {
                    reject(new HttpError(xhr.statusText, xhr.status));
                }
            };

            xhr.onerror = () => {
                this.logger.log(LogLevel.Warning, `Error from HTTP request. ${xhr.status}: ${xhr.statusText}`);
                reject(new HttpError(xhr.statusText, xhr.status));
            };

            xhr.ontimeout = () => {
                this.logger.log(LogLevel.Warning, `Timeout from HTTP request.`);
                reject(new TimeoutError());
            };

            xhr.send(request.content || "");
        });
    }
}
