// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

import { DataReceived, TransportClosed } from "./Common";
import { HttpClient, HttpRequest } from "./HttpClient";
import { HttpError, TimeoutError } from "./Errors";
import { ILogger, LogLevel } from "./ILogger";
import { IConnection } from "./IConnection";
import { AbortController } from "./AbortController";

export enum TransportType {
    WebSockets,
    ServerSentEvents,
    LongPolling
}

export const enum TransferMode {
    Text = 1,
    Binary
}

export interface ITransport {
    connect(url: string, requestedTransferMode: TransferMode, connection: IConnection): Promise<TransferMode>;
    send(data: any): Promise<void>;
    stop(): Promise<void>;
    onreceive: DataReceived;
    onclose: TransportClosed;
}

export class WebSocketTransport implements ITransport {
    private readonly logger: ILogger;
    private readonly accessTokenFactory: () => string;
    private webSocket: WebSocket;

    constructor(accessTokenFactory: () => string, logger: ILogger) {
        this.logger = logger;
        this.accessTokenFactory = accessTokenFactory || (() => null);
    }

    connect(url: string, requestedTransferMode: TransferMode, connection: IConnection): Promise<TransferMode> {

        return new Promise<TransferMode>((resolve, reject) => {
            url = url.replace(/^http/, "ws");
            let token = this.accessTokenFactory();
            if (token) {
                url += (url.indexOf("?") < 0 ? "?" : "&") + `access_token=${encodeURIComponent(token)}`;
            }

            let webSocket = new WebSocket(url);
            if (requestedTransferMode == TransferMode.Binary) {
                webSocket.binaryType = "arraybuffer";
            }

            webSocket.onopen = (event: Event) => {
                this.logger.log(LogLevel.Information, `WebSocket connected to ${url}`);
                this.webSocket = webSocket;
                resolve(requestedTransferMode);
            };

            webSocket.onerror = (event: Event) => {
                reject();
            };

            webSocket.onmessage = (message: MessageEvent) => {
                this.logger.log(LogLevel.Trace, `(WebSockets transport) data received: ${message.data}`);
                if (this.onreceive) {
                    this.onreceive(message.data);
                }
            }

            webSocket.onclose = (event: CloseEvent) => {
                // webSocket will be null if the transport did not start successfully
                if (this.onclose && this.webSocket) {
                    if (event.wasClean === false || event.code !== 1000) {
                        this.onclose(new Error(`Websocket closed with status code: ${event.code} (${event.reason})`));
                    }
                    else {
                        this.onclose();
                    }
                }
            }
        });
    }

    send(data: any): Promise<void> {
        if (this.webSocket && this.webSocket.readyState === WebSocket.OPEN) {
            this.webSocket.send(data);
            return Promise.resolve();
        }

        return Promise.reject("WebSocket is not in the OPEN state");
    }

    stop(): Promise<void> {
        if (this.webSocket) {
            this.webSocket.close();
            this.webSocket = null;
        }
        return Promise.resolve();
    }

    onreceive: DataReceived;
    onclose: TransportClosed;
}

export class ServerSentEventsTransport implements ITransport {
    private readonly httpClient: HttpClient;
    private readonly accessTokenFactory: () => string;
    private readonly logger: ILogger;
    private eventSource: EventSource;
    private url: string;

    constructor(httpClient: HttpClient, accessTokenFactory: () => string, logger: ILogger) {
        this.httpClient = httpClient;
        this.accessTokenFactory = accessTokenFactory || (() => null);
        this.logger = logger;
    }

    connect(url: string, requestedTransferMode: TransferMode, connection: IConnection): Promise<TransferMode> {
        if (typeof (EventSource) === "undefined") {
            Promise.reject("EventSource not supported by the browser.");
        }

        this.url = url;
        return new Promise<TransferMode>((resolve, reject) => {
            let token = this.accessTokenFactory();
            if (token) {
                url += (url.indexOf("?") < 0 ? "?" : "&") + `access_token=${encodeURIComponent(token)}`;
            }

            let eventSource = new EventSource(url);

            try {
                eventSource.onmessage = (e: MessageEvent) => {
                    if (this.onreceive) {
                        try {
                            this.logger.log(LogLevel.Trace, `(SSE transport) data received: ${e.data}`);
                            this.onreceive(e.data);
                        } catch (error) {
                            if (this.onclose) {
                                this.onclose(error);
                            }
                            return;
                        }
                    }
                };

                eventSource.onerror = (e: any) => {
                    reject();

                    // don't report an error if the transport did not start successfully
                    if (this.eventSource && this.onclose) {
                        this.onclose(new Error(e.message || "Error occurred"));
                    }
                }

                eventSource.onopen = () => {
                    this.logger.log(LogLevel.Information, `SSE connected to ${this.url}`);
                    this.eventSource = eventSource;
                    // SSE is a text protocol
                    resolve(TransferMode.Text);
                }
            }
            catch (e) {
                return Promise.reject(e);
            }
        });
    }

    async send(data: any): Promise<void> {
        return send(this.httpClient, this.url, this.accessTokenFactory, data);
    }

    stop(): Promise<void> {
        if (this.eventSource) {
            this.eventSource.close();
            this.eventSource = null;
        }
        return Promise.resolve();
    }

    onreceive: DataReceived;
    onclose: TransportClosed;
}

export class LongPollingTransport implements ITransport {
    private readonly httpClient: HttpClient;
    private readonly accessTokenFactory: () => string;
    private readonly logger: ILogger;

    private url: string;
    private pollXhr: XMLHttpRequest;
    private pollAbort: AbortController;

    constructor(httpClient: HttpClient, accessTokenFactory: () => string, logger: ILogger) {
        this.httpClient = httpClient;
        this.accessTokenFactory = accessTokenFactory || (() => null);
        this.logger = logger;
        this.pollAbort = new AbortController();
    }

    connect(url: string, requestedTransferMode: TransferMode, connection: IConnection): Promise<TransferMode> {
        this.url = url;

        // Set a flag indicating we have inherent keep-alive in this transport.
        connection.features.inherentKeepAlive = true;

        if (requestedTransferMode === TransferMode.Binary && (typeof new XMLHttpRequest().responseType !== "string")) {
            // This will work if we fix: https://github.com/aspnet/SignalR/issues/742
            throw new Error("Binary protocols over XmlHttpRequest not implementing advanced features are not supported.");
        }

        this.poll(this.url, requestedTransferMode);
        return Promise.resolve(requestedTransferMode);
    }

    private async poll(url: string, transferMode: TransferMode): Promise<void> {
        let pollOptions: HttpRequest = {
            timeout: 120000,
            abortSignal: this.pollAbort.signal,
            headers: new Map<string, string>(),
        };

        if (transferMode === TransferMode.Binary) {
            pollOptions.responseType = "arraybuffer";
        }

        let token = this.accessTokenFactory();
        if (token) {
            pollOptions.headers.set("Authorization", `Bearer ${token}`);
        }

        while (!this.pollAbort.signal.aborted) {
            try {
                let pollUrl = `${url}&_=${Date.now()}`;
                this.logger.log(LogLevel.Trace, `(LongPolling transport) polling: ${pollUrl}`);
                let response = await this.httpClient.get(pollUrl, pollOptions)
                if (response.statusCode === 204) {
                    this.logger.log(LogLevel.Information, "(LongPolling transport) Poll terminated by server");

                    // Poll terminated by server
                    if (this.onclose) {
                        this.onclose();
                    }
                    this.pollAbort.abort();
                }
                else if (response.statusCode !== 200) {
                    this.logger.log(LogLevel.Error, `(LongPolling transport) Unexpected response code: ${response.statusCode}`);

                    // Unexpected status code
                    if (this.onclose) {
                        this.onclose(new HttpError(response.statusText, response.statusCode));
                    }
                    this.pollAbort.abort();
                }
                else {
                    // Process the response
                    if (response.content) {
                        this.logger.log(LogLevel.Trace, `(LongPolling transport) data received: ${response.content}`);
                        if (this.onreceive) {
                            this.onreceive(response.content);
                        }
                    }
                    else {
                        // This is another way timeout manifest.
                        this.logger.log(LogLevel.Trace, "(LongPolling transport) Poll timed out, reissuing.");
                    }
                }
            } catch (e) {
                if (e instanceof TimeoutError) {
                    // Ignore timeouts and reissue the poll.
                    this.logger.log(LogLevel.Trace, "(LongPolling transport) Poll timed out, reissuing.");
                } else {
                    // Close the connection with the error as the result.
                    if (this.onclose) {
                        this.onclose(e);
                    }
                    this.pollAbort.abort();
                }
            }
        }
    }

    async send(data: any): Promise<void> {
        return send(this.httpClient, this.url, this.accessTokenFactory, data);
    }

    stop(): Promise<void> {
        this.pollAbort.abort();
        return Promise.resolve();
    }

    onreceive: DataReceived;
    onclose: TransportClosed;
}

async function send(httpClient: HttpClient, url: string, accessTokenFactory: () => string, content: string | ArrayBuffer): Promise<void> {
    let headers;
    let token = accessTokenFactory();
    if (token) {
        headers = new Map<string, string>();
        headers.set("Authorization", `Bearer ${accessTokenFactory()}`)
    }

    await httpClient.post(url, {
        content,
        headers
    });
}
