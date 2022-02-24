// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import { HeaderNames } from "./HeaderNames";
import { HttpClient } from "./HttpClient";
import { MessageHeaders } from "./IHubProtocol";
import { ILogger, LogLevel } from "./ILogger";
import { ITransport, TransferFormat } from "./ITransport";
import { WebSocketConstructor } from "./Polyfills";
import { Arg, getDataDetail, getUserAgentHeader, Platform } from "./Utils";

/** @private */
export class WebSocketTransport implements ITransport {
    private readonly _logger: ILogger;
    private readonly _accessTokenFactory: (() => string | Promise<string>) | undefined;
    private readonly _logMessageContent: boolean;
    private readonly _webSocketConstructor: WebSocketConstructor;
    private readonly _httpClient: HttpClient;
    private _webSocket?: WebSocket;
    private _headers: MessageHeaders;

    public onreceive: ((data: string | ArrayBuffer) => void) | null;
    public onclose: ((error?: Error) => void) | null;

    constructor(httpClient: HttpClient, accessTokenFactory: (() => string | Promise<string>) | undefined, logger: ILogger,
                logMessageContent: boolean, webSocketConstructor: WebSocketConstructor, headers: MessageHeaders) {
        this._logger = logger;
        this._accessTokenFactory = accessTokenFactory;
        this._logMessageContent = logMessageContent;
        this._webSocketConstructor = webSocketConstructor;
        this._httpClient = httpClient;

        this.onreceive = null;
        this.onclose = null;
        this._headers = headers;
    }

    public async connect(url: string, transferFormat: TransferFormat): Promise<void> {
        Arg.isRequired(url, "url");
        Arg.isRequired(transferFormat, "transferFormat");
        Arg.isIn(transferFormat, TransferFormat, "transferFormat");
        this._logger.log(LogLevel.Trace, "(WebSockets transport) Connecting.");

        let token: string;
        if (this._accessTokenFactory) {
            token = await this._accessTokenFactory();
        }

        return new Promise<void>((resolve, reject) => {
            url = url.replace(/^http/, "ws");
            let webSocket: WebSocket | undefined;
            const cookies = this._httpClient.getCookieString(url);
            let opened = false;

            if (Platform.isNode || Platform.isReactNative) {
                const headers: {[k: string]: string} = {};
                const [name, value] = getUserAgentHeader();
                headers[name] = value;
                if (token) {
                    headers[HeaderNames.Authorization] = `Bearer ${token}`;
                }

                if (cookies) {
                    headers[HeaderNames.Cookie] = cookies;
                }

                // Only pass headers when in non-browser environments
                webSocket = new this._webSocketConstructor(url, undefined, {
                    headers: { ...headers, ...this._headers },
                });
            }
            else
            {
                if (token) {
                    url += (url.indexOf("?") < 0 ? "?" : "&") + `access_token=${encodeURIComponent(token)}`;
                }
            }

            if (!webSocket) {
                // Chrome is not happy with passing 'undefined' as protocol
                webSocket = new this._webSocketConstructor(url);
            }

            if (transferFormat === TransferFormat.Binary) {
                webSocket.binaryType = "arraybuffer";
            }

            webSocket.onopen = (_event: Event) => {
                this._logger.log(LogLevel.Information, `WebSocket connected to ${url}.`);
                this._webSocket = webSocket;
                opened = true;
                resolve();
            };

            webSocket.onerror = (event: Event) => {
                let error: any = null;
                // ErrorEvent is a browser only type we need to check if the type exists before using it
                if (typeof ErrorEvent !== "undefined" && event instanceof ErrorEvent) {
                    error = event.error;
                } else {
                    error = "There was an error with the transport";
                }

                this._logger.log(LogLevel.Information, `(WebSockets transport) ${error}.`);
            };

            webSocket.onmessage = (message: MessageEvent) => {
                this._logger.log(LogLevel.Trace, `(WebSockets transport) data received. ${getDataDetail(message.data, this._logMessageContent)}.`);
                if (this.onreceive) {
                    try {
                        this.onreceive(message.data);
                    } catch (error) {
                        this._close(error);
                        return;
                    }
                }
            };

            webSocket.onclose = (event: CloseEvent) => {
                // Don't call close handler if connection was never established
                // We'll reject the connect call instead
                if (opened) {
                    this._close(event);
                } else {
                    let error: any = null;
                    // ErrorEvent is a browser only type we need to check if the type exists before using it
                    if (typeof ErrorEvent !== "undefined" && event instanceof ErrorEvent) {
                        error = event.error;
                    } else {
                        error = "WebSocket failed to connect. The connection could not be found on the server,"
                        + " either the endpoint may not be a SignalR endpoint,"
                        + " the connection ID is not present on the server, or there is a proxy blocking WebSockets."
                        + " If you have multiple servers check that sticky sessions are enabled.";
                    }

                    reject(new Error(error));
                }
            };
        });
    }

    public send(data: any): Promise<void> {
        if (this._webSocket && this._webSocket.readyState === this._webSocketConstructor.OPEN) {
            this._logger.log(LogLevel.Trace, `(WebSockets transport) sending data. ${getDataDetail(data, this._logMessageContent)}.`);
            this._webSocket.send(data);
            return Promise.resolve();
        }

        return Promise.reject("WebSocket is not in the OPEN state");
    }

    public stop(): Promise<void> {
        if (this._webSocket) {
            // Manually invoke onclose callback inline so we know the HttpConnection was closed properly before returning
            // This also solves an issue where websocket.onclose could take 18+ seconds to trigger during network disconnects
            this._close(undefined);
        }

        return Promise.resolve();
    }

    private _close(event?: CloseEvent | Error): void {
        // webSocket will be null if the transport did not start successfully
        if (this._webSocket) {
            // Clear websocket handlers because we are considering the socket closed now
            this._webSocket.onclose = () => {};
            this._webSocket.onmessage = () => {};
            this._webSocket.onerror = () => {};
            this._webSocket.close();
            this._webSocket = undefined;
        }

        this._logger.log(LogLevel.Trace, "(WebSockets transport) socket closed.");
        if (this.onclose) {
            if (this._isCloseEvent(event) && (event.wasClean === false || event.code !== 1000)) {
                this.onclose(new Error(`WebSocket closed with status code: ${event.code} (${event.reason || "no reason given"}).`));
            } else if (event instanceof Error) {
                this.onclose(event);
            } else {
                this.onclose();
            }
        }
    }

    private _isCloseEvent(event?: any): event is CloseEvent {
        return event && typeof event.wasClean === "boolean" && typeof event.code === "number";
    }
}
