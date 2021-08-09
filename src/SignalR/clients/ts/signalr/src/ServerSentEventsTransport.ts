// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import { HttpClient } from "./HttpClient";
import { MessageHeaders } from "./IHubProtocol";
import { ILogger, LogLevel } from "./ILogger";
import { ITransport, TransferFormat } from "./ITransport";
import { Arg, getDataDetail, getUserAgentHeader, Platform, sendMessage } from "./Utils";
import { IHttpConnectionOptions } from "./IHttpConnectionOptions";

/** @private */
export class ServerSentEventsTransport implements ITransport {
    private readonly _httpClient: HttpClient;
    private readonly _accessTokenFactory: (() => string | Promise<string>) | undefined;
    private readonly _logger: ILogger;
    private readonly _options: IHttpConnectionOptions;
    private _eventSource?: EventSource;
    private _url?: string;

    public onreceive: ((data: string | ArrayBuffer) => void) | null;
    public onclose: ((error?: Error) => void) | null;

    constructor(httpClient: HttpClient, accessTokenFactory: (() => string | Promise<string>) | undefined, logger: ILogger,
                options: IHttpConnectionOptions) {
        this._httpClient = httpClient;
        this._accessTokenFactory = accessTokenFactory;
        this._logger = logger;
        this._options = options;

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
                eventSource = new this._options.EventSource!(url, { withCredentials: this._options.withCredentials });
            } else {
                // Non-browser passes cookies via the dictionary
                const cookies = this._httpClient.getCookieString(url);
                const headers: MessageHeaders = {};
                headers.Cookie = cookies;
                const [name, value] = getUserAgentHeader();
                headers[name] = value;

                eventSource = new this._options.EventSource!(url, { withCredentials: this._options.withCredentials, headers: { ...headers, ...this._options.headers} } as EventSourceInit);
            }

            try {
                eventSource.onmessage = (e: MessageEvent) => {
                    if (this.onreceive) {
                        try {
                            this._logger.log(LogLevel.Trace, `(SSE transport) data received. ${getDataDetail(e.data, this._options.logMessageContent!)}.`);
                            this.onreceive(e.data);
                        } catch (error) {
                            this._close(error);
                            return;
                        }
                    }
                };

                // @ts-ignore: not using event on purpose
                eventSource.onerror = (e: Event) => {
                    // EventSource doesn't give any useful information about server side closes.
                    if (opened) {
                        this._close();
                    } else {
                        reject(new Error("EventSource failed to connect. The connection could not be found on the server,"
                        + " either the connection ID is not present on the server, or a proxy is refusing/buffering the connection."
                        + " If you have multiple servers check that sticky sessions are enabled."));
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
        return sendMessage(this._logger, "SSE", this._httpClient, this._url!, this._accessTokenFactory, data, this._options);
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
