// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

import { AbortController } from "./AbortController";
import { DataReceived, TransportClosed } from "./Common";
import { HttpError, TimeoutError } from "./Errors";
import { HttpClient, HttpRequest } from "./HttpClient";
import { ILogger, LogLevel } from "./ILogger";
import { Arg } from "./Utils";

export enum TransportType {
    WebSockets,
    ServerSentEvents,
    LongPolling,
}

export enum TransferFormat {
    Text = 1,
    Binary,
}

export interface ITransport {
    connect(url: string, transferFormat: TransferFormat): Promise<void>;
    send(data: any): Promise<void>;
    stop(): Promise<void>;
    onreceive: DataReceived;
    onclose: TransportClosed;
}

export class WebSocketTransport implements ITransport {
    private readonly logger: ILogger;
    private readonly accessTokenFactory: () => string | Promise<string>;
    private readonly logMessageContent: boolean;
    private webSocket: WebSocket;

    constructor(accessTokenFactory: () => string | Promise<string>, logger: ILogger, logMessageContent: boolean) {
        this.logger = logger;
        this.accessTokenFactory = accessTokenFactory || (() => null);
        this.logMessageContent = logMessageContent;
    }

    public async connect(url: string, transferFormat: TransferFormat): Promise<void> {
        Arg.isRequired(url, "url");
        Arg.isRequired(transferFormat, "transferFormat");
        Arg.isIn(transferFormat, TransferFormat, "transferFormat");

        if (typeof (WebSocket) === "undefined") {
            throw new Error("'WebSocket' is not supported in your environment.");
        }

        this.logger.log(LogLevel.Trace, "(WebSockets transport) Connecting");

        const token = await this.accessTokenFactory();
        return new Promise<void>((resolve, reject) => {
            url = url.replace(/^http/, "ws");
            if (token) {
                url += (url.indexOf("?") < 0 ? "?" : "&") + `access_token=${encodeURIComponent(token)}`;
            }

            const webSocket = new WebSocket(url);
            if (transferFormat === TransferFormat.Binary) {
                webSocket.binaryType = "arraybuffer";
            }

            webSocket.onopen = (event: Event) => {
                this.logger.log(LogLevel.Information, `WebSocket connected to ${url}`);
                this.webSocket = webSocket;
                resolve();
            };

            webSocket.onerror = (event: ErrorEvent) => {
                reject(event.error);
            };

            webSocket.onmessage = (message: MessageEvent) => {
                this.logger.log(LogLevel.Trace, `(WebSockets transport) data received. ${getDataDetail(message.data, this.logMessageContent)}.`);
                if (this.onreceive) {
                    this.onreceive(message.data);
                }
            };

            webSocket.onclose = (event: CloseEvent) => {
                // webSocket will be null if the transport did not start successfully
                if (this.onclose && this.webSocket) {
                    if (event.wasClean === false || event.code !== 1000) {
                        this.onclose(new Error(`Websocket closed with status code: ${event.code} (${event.reason})`));
                    } else {
                        this.onclose();
                    }
                }
            };
        });
    }

    public send(data: any): Promise<void> {
        if (this.webSocket && this.webSocket.readyState === WebSocket.OPEN) {
            this.logger.log(LogLevel.Trace, `(WebSockets transport) sending data. ${getDataDetail(data, this.logMessageContent)}.`);
            this.webSocket.send(data);
            return Promise.resolve();
        }

        return Promise.reject("WebSocket is not in the OPEN state");
    }

    public stop(): Promise<void> {
        if (this.webSocket) {
            this.webSocket.close();
            this.webSocket = null;
        }
        return Promise.resolve();
    }

    public onreceive: DataReceived;
    public onclose: TransportClosed;
}

export class ServerSentEventsTransport implements ITransport {
    private readonly httpClient: HttpClient;
    private readonly accessTokenFactory: () => string | Promise<string>;
    private readonly logger: ILogger;
    private readonly logMessageContent: boolean;
    private eventSource: EventSource;
    private url: string;

    constructor(httpClient: HttpClient, accessTokenFactory: () => string | Promise<string>, logger: ILogger, logMessageContent: boolean) {
        this.httpClient = httpClient;
        this.accessTokenFactory = accessTokenFactory || (() => null);
        this.logger = logger;
        this.logMessageContent = logMessageContent;
    }

    public async connect(url: string, transferFormat: TransferFormat): Promise<void> {
        Arg.isRequired(url, "url");
        Arg.isRequired(transferFormat, "transferFormat");
        Arg.isIn(transferFormat, TransferFormat, "transferFormat");

        if (typeof (EventSource) === "undefined") {
            throw new Error("'EventSource' is not supported in your environment.");
        }

        this.logger.log(LogLevel.Trace, "(SSE transport) Connecting");

        this.url = url;
        const token = await this.accessTokenFactory();
        return new Promise<void>((resolve, reject) => {
            if (transferFormat !== TransferFormat.Text) {
                reject(new Error("The Server-Sent Events transport only supports the 'Text' transfer format"));
            }

            if (token) {
                url += (url.indexOf("?") < 0 ? "?" : "&") + `access_token=${encodeURIComponent(token)}`;
            }

            const eventSource = new EventSource(url, { withCredentials: true });

            try {
                eventSource.onmessage = (e: MessageEvent) => {
                    if (this.onreceive) {
                        try {
                            this.logger.log(LogLevel.Trace, `(SSE transport) data received. ${getDataDetail(e.data, this.logMessageContent)}.`);
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
                    reject(new Error(e.message || "Error occurred"));

                    // don't report an error if the transport did not start successfully
                    if (this.eventSource && this.onclose) {
                        this.onclose(new Error(e.message || "Error occurred"));
                    }
                };

                eventSource.onopen = () => {
                    this.logger.log(LogLevel.Information, `SSE connected to ${this.url}`);
                    this.eventSource = eventSource;
                    // SSE is a text protocol
                    resolve();
                };
            } catch (e) {
                return Promise.reject(e);
            }
        });
    }

    public async send(data: any): Promise<void> {
        return send(this.logger, "SSE", this.httpClient, this.url, this.accessTokenFactory, data, this.logMessageContent);
    }

    public stop(): Promise<void> {
        if (this.eventSource) {
            this.eventSource.close();
            this.eventSource = null;
        }
        return Promise.resolve();
    }

    public onreceive: DataReceived;
    public onclose: TransportClosed;
}

export class LongPollingTransport implements ITransport {
    private readonly httpClient: HttpClient;
    private readonly accessTokenFactory: () => string | Promise<string>;
    private readonly logger: ILogger;
    private readonly logMessageContent: boolean;

    private url: string;
    private pollXhr: XMLHttpRequest;
    private pollAbort: AbortController;

    constructor(httpClient: HttpClient, accessTokenFactory: () => string | Promise<string>, logger: ILogger, logMessageContent: boolean) {
        this.httpClient = httpClient;
        this.accessTokenFactory = accessTokenFactory || (() => null);
        this.logger = logger;
        this.pollAbort = new AbortController();
        this.logMessageContent = logMessageContent;
    }

    public connect(url: string, transferFormat: TransferFormat): Promise<void> {
        Arg.isRequired(url, "url");
        Arg.isRequired(transferFormat, "transferFormat");
        Arg.isIn(transferFormat, TransferFormat, "transferFormat");

        this.url = url;

        this.logger.log(LogLevel.Trace, "(LongPolling transport) Connecting");

        if (transferFormat === TransferFormat.Binary && (typeof new XMLHttpRequest().responseType !== "string")) {
            // This will work if we fix: https://github.com/aspnet/SignalR/issues/742
            throw new Error("Binary protocols over XmlHttpRequest not implementing advanced features are not supported.");
        }

        this.poll(this.url, transferFormat);
        return Promise.resolve();
    }

    private async poll(url: string, transferFormat: TransferFormat): Promise<void> {
        const pollOptions: HttpRequest = {
            abortSignal: this.pollAbort.signal,
            headers: {},
            timeout: 90000,
        };

        if (transferFormat === TransferFormat.Binary) {
            pollOptions.responseType = "arraybuffer";
        }

        const token = await this.accessTokenFactory();
        if (token) {
            // tslint:disable-next-line:no-string-literal
            pollOptions.headers["Authorization"] = `Bearer ${token}`;
        }

        while (!this.pollAbort.signal.aborted) {
            try {
                const pollUrl = `${url}&_=${Date.now()}`;
                this.logger.log(LogLevel.Trace, `(LongPolling transport) polling: ${pollUrl}`);
                const response = await this.httpClient.get(pollUrl, pollOptions);
                if (response.statusCode === 204) {
                    this.logger.log(LogLevel.Information, "(LongPolling transport) Poll terminated by server");

                    // Poll terminated by server
                    if (this.onclose) {
                        this.onclose();
                    }
                    this.pollAbort.abort();
                } else if (response.statusCode !== 200) {
                    this.logger.log(LogLevel.Error, `(LongPolling transport) Unexpected response code: ${response.statusCode}`);

                    // Unexpected status code
                    if (this.onclose) {
                        this.onclose(new HttpError(response.statusText, response.statusCode));
                    }
                    this.pollAbort.abort();
                } else {
                    // Process the response
                    if (response.content) {
                        this.logger.log(LogLevel.Trace, `(LongPolling transport) data received. ${getDataDetail(response.content, this.logMessageContent)}`);
                        if (this.onreceive) {
                            this.onreceive(response.content);
                        }
                    } else {
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

    public async send(data: any): Promise<void> {
        return send(this.logger, "LongPolling", this.httpClient, this.url, this.accessTokenFactory, data, this.logMessageContent);
    }

    public stop(): Promise<void> {
        this.pollAbort.abort();
        return Promise.resolve();
    }

    public onreceive: DataReceived;
    public onclose: TransportClosed;
}

function getDataDetail(data: any, includeContent: boolean): string {
    let length: string = null;
    if (data instanceof ArrayBuffer) {
        length = `Binary data of length ${data.byteLength}.`;
        if (includeContent) {
            length += ` Content: '${formatArrayBuffer(data)}'.`;
        }
    } else if (typeof data === "string") {
        length = `String data of length ${data.length}.`;
        if (includeContent) {
            length += ` Content: '${data}'.`;
        }
    }
    return length;
}

function formatArrayBuffer(data: ArrayBuffer): string {
    const view = new Uint8Array(data);

    // Uint8Array.map only supports returning another Uint8Array?
    let str = "";
    view.forEach((num) => {
        const pad = num < 16 ? "0" : "";
        str += `0x${pad}${num.toString(16)} `;
    });

    // Trim of trailing space.
    return str.substr(0, str.length - 1);
}

async function send(logger: ILogger, transportName: string, httpClient: HttpClient, url: string, accessTokenFactory: () => string | Promise<string>, content: string | ArrayBuffer, logMessageContent: boolean): Promise<void> {
    let headers;
    const token = await accessTokenFactory();
    if (token) {
        headers = {
            ["Authorization"]: `Bearer ${token}`,
        };
    }

    logger.log(LogLevel.Trace, `(${transportName} transport) sending data. ${getDataDetail(content, logMessageContent)}.`);

    const response = await httpClient.post(url, {
        content,
        headers,
    });

    logger.log(LogLevel.Trace, `(${transportName} transport) request complete. Response status: ${response.statusCode}.`);
}
