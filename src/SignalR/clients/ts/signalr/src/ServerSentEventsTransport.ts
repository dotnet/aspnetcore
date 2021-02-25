// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

import { HttpClient } from "./HttpClient";
import { MessageHeaders } from "./IHubProtocol";
import { ILogger, LogLevel } from "./ILogger";
import { ITransport, TransferFormat } from "./ITransport";
import { EventSourceConstructor } from "./Polyfills";
import { Arg, getDataDetail, getUserAgentHeader, Platform, sendMessage } from "./Utils";

/** @private */
export class ServerSentEventsTransport implements ITransport {
    private readonly _httpClient: HttpClient;
    private readonly _accessTokenFactory: (() => string | Promise<string>) | undefined;
    private readonly _logger: ILogger;
    private readonly _logMessageContent: boolean;
    private readonly _withCredentials: boolean;
    private readonly _eventSourceConstructor: EventSourceConstructor;
    private _eventSource?: EventSource;
    private _url?: string;
    private _headers: MessageHeaders;

    public onreceive: ((data: string | ArrayBuffer) => void) | null;
    public onclose: ((error?: Error) => void) | null;

    constructor(httpClient: HttpClient, accessTokenFactory: (() => string | Promise<string>) | undefined, logger: ILogger,
                logMessageContent: boolean, eventSourceConstructor: EventSourceConstructor, withCredentials: boolean, headers: MessageHeaders) {
        this._httpClient = httpClient;
        this._accessTokenFactory = accessTokenFactory;
        this._logger = logger;
        this._logMessageContent = logMessageContent;
        this._withCredentials = withCredentials;
        this._eventSourceConstructor = eventSourceConstructor;
        this._headers = headers;

        this.onreceive = null;
        this.onclose = null;
    }

    public async connect(url: string, transferFormat: TransferFormat): Promise<void> {
        Arg.isRequired(url, "url");
        Arg.isRequired(transferFormat, "transferFormat");
        Arg.isIn(transferFormat, TransferFormat, "transferFormat");

        this._logger.log(LogLevel.Trace, "(SSE transport) Connecting.");

        // set url before accessTokenFactory because this.url is only for send and we set the auth header instead of the query string for send
        this._url = url;

        if (this._accessTokenFactory) {
            const token = await this._accessTokenFactory();
            if (token) {
                url += (url.indexOf("?") < 0 ? "?" : "&") + `access_token=${encodeURIComponent(token)}`;
            }
        }

        return new Promise<void>((resolve, reject) => {
            let opened = false;
            if (transferFormat !== TransferFormat.Text) {
                reject(new Error("The Server-Sent Events transport only supports the 'Text' transfer format"));
                return;
            }

            let eventSource: EventSource;
            if (Platform.isBrowser || Platform.isWebWorker) {
                eventSource = new this._eventSourceConstructor(url, { withCredentials: this._withCredentials });
            } else {
                // Non-browser passes cookies via the dictionary
                const cookies = this._httpClient.getCookieString(url);
                const headers: MessageHeaders = {};
                headers.Cookie = cookies;
                const [name, value] = getUserAgentHeader();
                headers[name] = value;

                eventSource = new this._eventSourceConstructor(url, { withCredentials: this._withCredentials, headers: { ...headers, ...this._headers} } as EventSourceInit);
            }

            try {
                eventSource.onmessage = (e: MessageEvent) => {
                    if (this.onreceive) {
                        try {
                            this._logger.log(LogLevel.Trace, `(SSE transport) data received. ${getDataDetail(e.data, this._logMessageContent)}.`);
                            this.onreceive(e.data);
                        } catch (error) {
                            this._close(error);
                            return;
                        }
                    }
                };

                // @ts-ignore: not using event on purpose
                eventSource.onerror = (e: Event) => {
                    const error = new Error("Error occurred while starting EventSource");
                    if (opened) {
                        this._close(error);
                    } else {
                        reject(error);
                    }
                };

                eventSource.onopen = () => {
                    this._logger.log(LogLevel.Information, `SSE connected to ${this._url}`);
                    this._eventSource = eventSource;
                    opened = true;
                    resolve();
                };
            } catch (e) {
                reject(e);
                return;
            }
        });
    }

    public async send(data: any): Promise<void> {
        if (!this._eventSource) {
            return Promise.reject(new Error("Cannot send until the transport is connected"));
        }
        return sendMessage(this._logger, "SSE", this._httpClient, this._url!, this._accessTokenFactory, data, this._logMessageContent, this._withCredentials, this._headers);
    }

    public stop(): Promise<void> {
        this._close();
        return Promise.resolve();
    }

    private _close(e?: Error) {
        if (this._eventSource) {
            this._eventSource.close();
            this._eventSource = undefined;

            if (this.onclose) {
                this.onclose(e);
            }
        }
    }
}
