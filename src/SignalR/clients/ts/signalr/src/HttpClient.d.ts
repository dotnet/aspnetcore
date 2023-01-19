import { AbortSignal } from "./AbortController";
import { MessageHeaders } from "./IHubProtocol";
/** Represents an HTTP request. */
export interface HttpRequest {
    /** The HTTP method to use for the request. */
    method?: string;
    /** The URL for the request. */
    url?: string;
    /** The body content for the request. May be a string or an ArrayBuffer (for binary data). */
    content?: string | ArrayBuffer;
    /** An object describing headers to apply to the request. */
    headers?: MessageHeaders;
    /** The XMLHttpRequestResponseType to apply to the request. */
    responseType?: XMLHttpRequestResponseType;
    /** An AbortSignal that can be monitored for cancellation. */
    abortSignal?: AbortSignal;
    /** The time to wait for the request to complete before throwing a TimeoutError. Measured in milliseconds. */
    timeout?: number;
    /** This controls whether credentials such as cookies are sent in cross-site requests. */
    withCredentials?: boolean;
}
/** Represents an HTTP response. */
export declare class HttpResponse {
    readonly statusCode: number;
    readonly statusText?: string | undefined;
    readonly content?: string | ArrayBuffer | undefined;
    /** Constructs a new instance of {@link @microsoft/signalr.HttpResponse} with the specified status code.
     *
     * @param {number} statusCode The status code of the response.
     */
    constructor(statusCode: number);
    /** Constructs a new instance of {@link @microsoft/signalr.HttpResponse} with the specified status code and message.
     *
     * @param {number} statusCode The status code of the response.
     * @param {string} statusText The status message of the response.
     */
    constructor(statusCode: number, statusText: string);
    /** Constructs a new instance of {@link @microsoft/signalr.HttpResponse} with the specified status code, message and string content.
     *
     * @param {number} statusCode The status code of the response.
     * @param {string} statusText The status message of the response.
     * @param {string} content The content of the response.
     */
    constructor(statusCode: number, statusText: string, content: string);
    /** Constructs a new instance of {@link @microsoft/signalr.HttpResponse} with the specified status code, message and binary content.
     *
     * @param {number} statusCode The status code of the response.
     * @param {string} statusText The status message of the response.
     * @param {ArrayBuffer} content The content of the response.
     */
    constructor(statusCode: number, statusText: string, content: ArrayBuffer);
    /** Constructs a new instance of {@link @microsoft/signalr.HttpResponse} with the specified status code, message and binary content.
     *
     * @param {number} statusCode The status code of the response.
     * @param {string} statusText The status message of the response.
     * @param {string | ArrayBuffer} content The content of the response.
     */
    constructor(statusCode: number, statusText: string, content: string | ArrayBuffer);
}
/** Abstraction over an HTTP client.
 *
 * This class provides an abstraction over an HTTP client so that a different implementation can be provided on different platforms.
 */
export declare abstract class HttpClient {
    /** Issues an HTTP GET request to the specified URL, returning a Promise that resolves with an {@link @microsoft/signalr.HttpResponse} representing the result.
     *
     * @param {string} url The URL for the request.
     * @returns {Promise<HttpResponse>} A Promise that resolves with an {@link @microsoft/signalr.HttpResponse} describing the response, or rejects with an Error indicating a failure.
     */
    get(url: string): Promise<HttpResponse>;
    /** Issues an HTTP GET request to the specified URL, returning a Promise that resolves with an {@link @microsoft/signalr.HttpResponse} representing the result.
     *
     * @param {string} url The URL for the request.
     * @param {HttpRequest} options Additional options to configure the request. The 'url' field in this object will be overridden by the url parameter.
     * @returns {Promise<HttpResponse>} A Promise that resolves with an {@link @microsoft/signalr.HttpResponse} describing the response, or rejects with an Error indicating a failure.
     */
    get(url: string, options: HttpRequest): Promise<HttpResponse>;
    /** Issues an HTTP POST request to the specified URL, returning a Promise that resolves with an {@link @microsoft/signalr.HttpResponse} representing the result.
     *
     * @param {string} url The URL for the request.
     * @returns {Promise<HttpResponse>} A Promise that resolves with an {@link @microsoft/signalr.HttpResponse} describing the response, or rejects with an Error indicating a failure.
     */
    post(url: string): Promise<HttpResponse>;
    /** Issues an HTTP POST request to the specified URL, returning a Promise that resolves with an {@link @microsoft/signalr.HttpResponse} representing the result.
     *
     * @param {string} url The URL for the request.
     * @param {HttpRequest} options Additional options to configure the request. The 'url' field in this object will be overridden by the url parameter.
     * @returns {Promise<HttpResponse>} A Promise that resolves with an {@link @microsoft/signalr.HttpResponse} describing the response, or rejects with an Error indicating a failure.
     */
    post(url: string, options: HttpRequest): Promise<HttpResponse>;
    /** Issues an HTTP DELETE request to the specified URL, returning a Promise that resolves with an {@link @microsoft/signalr.HttpResponse} representing the result.
     *
     * @param {string} url The URL for the request.
     * @returns {Promise<HttpResponse>} A Promise that resolves with an {@link @microsoft/signalr.HttpResponse} describing the response, or rejects with an Error indicating a failure.
     */
    delete(url: string): Promise<HttpResponse>;
    /** Issues an HTTP DELETE request to the specified URL, returning a Promise that resolves with an {@link @microsoft/signalr.HttpResponse} representing the result.
     *
     * @param {string} url The URL for the request.
     * @param {HttpRequest} options Additional options to configure the request. The 'url' field in this object will be overridden by the url parameter.
     * @returns {Promise<HttpResponse>} A Promise that resolves with an {@link @microsoft/signalr.HttpResponse} describing the response, or rejects with an Error indicating a failure.
     */
    delete(url: string, options: HttpRequest): Promise<HttpResponse>;
    /** Issues an HTTP request to the specified URL, returning a {@link Promise} that resolves with an {@link @microsoft/signalr.HttpResponse} representing the result.
     *
     * @param {HttpRequest} request An {@link @microsoft/signalr.HttpRequest} describing the request to send.
     * @returns {Promise<HttpResponse>} A Promise that resolves with an HttpResponse describing the response, or rejects with an Error indicating a failure.
     */
    abstract send(request: HttpRequest): Promise<HttpResponse>;
    /** Gets all cookies that apply to the specified URL.
     *
     * @param url The URL that the cookies are valid for.
     * @returns {string} A string containing all the key-value cookie pairs for the specified URL.
     */
    getCookieString(url: string): string;
}
//# sourceMappingURL=HttpClient.d.ts.map