// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

import { HttpClient } from "./HttpClient";
import { MessageHeaders } from "./IHubProtocol";
import { ILogger, LogLevel } from "./ILogger";
import { ITransport, TransferFormat } from "./ITransport";
import { WebSocketConstructor } from "./Polyfills";
import { Arg, getDataDetail, getUserAgentHeader, Platform } from "./Utils";

/** @private */
export class WebSocketTransport implements ITransport {
    private readonly logger: ILogger;
    private readonly accessTokenFactory: (() => string | Promise<string>) | undefined;
    private readonly logMessageContent: boolean;
    private readonly webSocketConstructor: WebSocketConstructor;
    private readonly httpClient: HttpClient;
    private webSocket?: WebSocket;
    private headers: MessageHeaders;

    public onreceive: ((data: string | ArrayBuffer) => void) | null;
    public onclose: ((error?: Error) => void) | null;

    constructor(httpClient: HttpClient, accessTokenFactory: (() => string | Promise<string>) | undefined, logger: ILogger,
                logMessageContent: boolean, webSocketConstructor: WebSocketConstructor, headers: MessageHeaders) {
        this.logger = logger;
        this.accessTokenFactory = accessTokenFactory;
        this.logMessageContent = logMessageContent;
        this.webSocketConstructor = webSocketConstructor;
        this.httpClient = httpClient;

        this.onreceive = null;
        this.onclose = null;
        this.headers = headers;
    }

    public async connect(url: string, transferFormat: TransferFormat): Promise<void> {
        Arg.isRequired(url, "url");
        Arg.isRequired(transferFormat, "transferFormat");
        Arg.isIn(transferFormat, TransferFormat, "transferFormat");
        this.logger.log(LogLevel.Trace, "(WebSockets transport) Connecting.");

        if (this.accessTokenFactory) {
            const token = await this.accessTokenFactory();
            if (token) {
                url += (url.indexOf("?") < 0 ? "?" : "&") + `access_token=${encodeURIComponent(token)}`;
            }
        }

        return new Promise<void>((resolve, reject) => {
            url = url.replace(/^http/, "ws");
            let webSocket: WebSocket | undefined;
            const cookies = this.httpClient.getCookieString(url);
            let opened = false;

            if (Platform.isNode) {
                const headers = {};
                const [name, value] = getUserAgentHeader();
                headers[name] = value;

                if (cookies) {
                    headers[`Cookie`] = `${cookies}`;
                }

                // Only pass headers when in non-browser environments
                webSocket = new this.webSocketConstructor(url, undefined, {
                    headers: { ...headers, ...this.headers },
                });
            }

            if (!webSocket) {
                // Chrome is not happy with passing 'undefined' as protocol
                webSocket = new this.webSocketConstructor(url);
            }

            if (transferFormat === TransferFormat.Binary) {
                webSocket.binaryType = "arraybuffer";
            }

            // tslint:disable-next-line:variable-name
            webSocket.onopen = (_event: Event) => {
                this.logger.log(LogLevel.Information, `WebSocket connected to ${url}.`);
                this.webSocket = webSocket;
                opened = true;
                resolve();
            };

            webSocket.onerror = (event: Event) => {
                let error: any = null;
                // ErrorEvent is a browser only type we need to check if the type exists before using it
                if (typeof ErrorEvent !== "undefined" && event instanceof ErrorEvent) {
                    error = event.error;
                } else {
                    error = new Error("There was an error with the transport.");
                }

                reject(error);
            };

            webSocket.onmessage = (message: MessageEvent) => {
                this.logger.log(LogLevel.Trace, `(WebSockets transport) data received. ${getDataDetail(message.data, this.logMessageContent)}.`);
                if (this.onreceive) {
                    try {
                        this.onreceive(message.data);
                    } catch (error) {
                        this.close(error);
                        return;
                    }
                }
            };

            webSocket.onclose = (event: CloseEvent) => {
                // Don't call close handler if connection was never established
                // We'll reject the connect call instead
                if (opened) {
                    this.close(event);
                } else {
                    let error: any = null;
                    // ErrorEvent is a browser only type we need to check if the type exists before using it
                    if (typeof ErrorEvent !== "undefined" && event instanceof ErrorEvent) {
                        error = event.error;
                    } else {
                        error = new Error("There was an error with the transport.");
                    }

                    reject(error);
                }
            };
        });
    }

    public send(data: any): Promise<void> {
        if (this.webSocket && this.webSocket.readyState === this.webSocketConstructor.OPEN) {
            this.logger.log(LogLevel.Trace, `(WebSockets transport) sending data. ${getDataDetail(data, this.logMessageContent)}.`);
            this.webSocket.send(data);
            return Promise.resolve();
        }

        return Promise.reject("WebSocket is not in the OPEN state");
    }

    public stop(): Promise<void> {
        if (this.webSocket) {
            // Manually invoke onclose callback inline so we know the HttpConnection was closed properly before returning
            // This also solves an issue where websocket.onclose could take 18+ seconds to trigger during network disconnects
            this.close(undefined);
        }

        return Promise.resolve();
    }

    private close(event?: CloseEvent | Error): void {
        // webSocket will be null if the transport did not start successfully
        if (this.webSocket) {
            // Clear websocket handlers because we are considering the socket closed now
            this.webSocket.onclose = () => {};
            this.webSocket.onmessage = () => {};
            this.webSocket.onerror = () => {};
            this.webSocket.close();
            this.webSocket = undefined;
        }

        this.logger.log(LogLevel.Trace, "(WebSockets transport) socket closed.");
        if (this.onclose) {
            if (this.isCloseEvent(event) && (event.wasClean === false || event.code !== 1000)) {
                this.onclose(new Error(`WebSocket closed with status code: ${event.code} (${event.reason}).`));
            } else if (event instanceof Error) {
                this.onclose(event);
            } else {
                this.onclose();
            }
        }
    }

    private isCloseEvent(event?: any): event is CloseEvent {
        return event && typeof event.wasClean === "boolean" && typeof event.code === "number";
    }
}
