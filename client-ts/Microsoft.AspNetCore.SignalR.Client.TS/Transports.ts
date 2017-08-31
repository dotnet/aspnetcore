// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

import { DataReceived, TransportClosed } from "./Common"
import { IHttpClient } from "./HttpClient"
import { HttpError } from "./HttpError"
import { ILogger, LogLevel } from "./ILogger"

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
    connect(url: string, requestedTransferMode: TransferMode): Promise<TransferMode>;
    send(data: any): Promise<void>;
    stop(): void;
    onDataReceived: DataReceived;
    onClosed: TransportClosed;
}

export class WebSocketTransport implements ITransport {
    private readonly logger: ILogger;
    private webSocket: WebSocket;

    constructor(logger: ILogger) {
        this.logger = logger;
    }

    connect(url: string, requestedTransferMode: TransferMode): Promise<TransferMode> {

        return new Promise<TransferMode>((resolve, reject) => {
            url = url.replace(/^http/, "ws");

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
                this.logger.log(LogLevel.Information, `(WebSockets transport) data received: ${message.data}`);
                if (this.onDataReceived) {
                    this.onDataReceived(message.data);
                }
            }

            webSocket.onclose = (event: CloseEvent) => {
                // webSocket will be null if the transport did not start successfully
                if (this.onClosed && this.webSocket) {
                    if (event.wasClean === false || event.code !== 1000) {
                        this.onClosed(new Error(`Websocket closed with status code: ${event.code} (${event.reason})`));
                    }
                    else {
                        this.onClosed();
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

    stop(): void {
        if (this.webSocket) {
            this.webSocket.close();
            this.webSocket = null;
        }
    }

    onDataReceived: DataReceived;
    onClosed: TransportClosed;
}

export class ServerSentEventsTransport implements ITransport {
    private readonly httpClient: IHttpClient;
    private readonly logger: ILogger;
    private eventSource: EventSource;
    private url: string;

    constructor(httpClient: IHttpClient, logger: ILogger) {
        this.httpClient = httpClient;
        this.logger = logger;
    }

    connect(url: string, requestedTransferMode: TransferMode): Promise<TransferMode> {
        if (typeof (EventSource) === "undefined") {
            Promise.reject("EventSource not supported by the browser.");
        }
        this.url = url;

        return new Promise<TransferMode>((resolve, reject) => {
            let eventSource = new EventSource(this.url);

            try {
                eventSource.onmessage = (e: MessageEvent) => {
                    if (this.onDataReceived) {
                        try {
                            this.logger.log(LogLevel.Information, `(SSE transport) data received: ${e.data}`);
                            this.onDataReceived(e.data);
                        } catch (error) {
                            if (this.onClosed) {
                                this.onClosed(error);
                            }
                            return;
                        }
                    }
                };

                eventSource.onerror = (e: ErrorEvent) => {
                    reject();

                    // don't report an error if the transport did not start successfully
                    if (this.eventSource && this.onClosed) {
                        this.onClosed(new Error(e.message || "Error occurred"));
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
        return send(this.httpClient, this.url, data);
    }

    stop(): void {
        if (this.eventSource) {
            this.eventSource.close();
            this.eventSource = null;
        }
    }

    onDataReceived: DataReceived;
    onClosed: TransportClosed;
}

export class LongPollingTransport implements ITransport {
    private readonly httpClient: IHttpClient;
    private readonly logger: ILogger;

    private url: string;
    private pollXhr: XMLHttpRequest;
    private shouldPoll: boolean;

    constructor(httpClient: IHttpClient, logger: ILogger) {
        this.httpClient = httpClient;
        this.logger = logger;
    }

    connect(url: string, requestedTransferMode: TransferMode): Promise<TransferMode> {
        this.url = url;
        this.shouldPoll = true;

        if (requestedTransferMode === TransferMode.Binary && (typeof new XMLHttpRequest().responseType !== "string")) {
            // This will work if we fix: https://github.com/aspnet/SignalR/issues/742
            throw new Error("Binary protocols over XmlHttpRequest not implementing advanced features are not supported.");
        }

        this.poll(this.url, requestedTransferMode);
        return Promise.resolve(requestedTransferMode);
    }

    private poll(url: string, transferMode: TransferMode): void {
        if (!this.shouldPoll) {
            return;
        }

        let pollXhr = new XMLHttpRequest();

        pollXhr.onload = () => {
            if (pollXhr.status == 200) {
                if (this.onDataReceived) {
                    try {
                        let response = transferMode === TransferMode.Text
                            ? pollXhr.responseText
                            : pollXhr.response;

                        if (response) {
                            this.logger.log(LogLevel.Information, `(LongPolling transport) data received: ${response}`);
                            this.onDataReceived(response);
                        }
                        else {
                            this.logger.log(LogLevel.Information, "(LongPolling transport) timed out");
                        }
                    } catch (error) {
                        if (this.onClosed) {
                            this.onClosed(error);
                        }
                        return;
                    }
                }
                this.poll(url, transferMode);
            }
            else if (this.pollXhr.status == 204) {
                if (this.onClosed) {
                    this.onClosed();
                }
            }
            else {
                if (this.onClosed) {
                    this.onClosed(new HttpError(pollXhr.statusText, pollXhr.status));
                }
            }
        };

        pollXhr.onerror = () => {
            if (this.onClosed) {
                // network related error or denied cross domain request
                this.onClosed(new Error("Sending HTTP request failed."));
            }
        };

        pollXhr.ontimeout = () => {
            this.poll(url, transferMode);
        }

        this.pollXhr = pollXhr;
        this.pollXhr.open("GET", url, true);
        if (transferMode === TransferMode.Binary) {
            this.pollXhr.responseType = "arraybuffer";
        }
        // IE caches xhr requests
        this.pollXhr.setRequestHeader("Cache-Control", "no-cache");
        // TODO: consider making timeout configurable
        this.pollXhr.timeout = 120000;
        this.pollXhr.send();
    }

    async send(data: any): Promise<void> {
        return send(this.httpClient, this.url, data);
    }

    stop(): void {
        this.shouldPoll = false;
        if (this.pollXhr) {
            this.pollXhr.abort();
            this.pollXhr = null;
        }
    }

    onDataReceived: DataReceived;
    onClosed: TransportClosed;
}

const headers = new Map<string, string>();

async function send(httpClient: IHttpClient, url: string, data: any): Promise<void> {
    await httpClient.post(url, data, headers);
}