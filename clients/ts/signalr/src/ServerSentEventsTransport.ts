// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

import { HttpClient } from "./HttpClient";
import { ILogger, LogLevel } from "./ILogger";
import { ITransport, TransferFormat } from "./ITransport";
import { EventSourceConstructor } from "./Polyfills";
import { Arg, getDataDetail, sendMessage } from "./Utils";

/** @private */
export class ServerSentEventsTransport implements ITransport {
    private readonly httpClient: HttpClient;
    private readonly accessTokenFactory: (() => string | Promise<string>) | undefined;
    private readonly logger: ILogger;
    private readonly logMessageContent: boolean;
    private readonly eventSourceConstructor: EventSourceConstructor;
    private eventSource?: EventSource;
    private url?: string;

    public onreceive: ((data: string | ArrayBuffer) => void) | null;
    public onclose: ((error?: Error) => void) | null;

    constructor(httpClient: HttpClient, accessTokenFactory: (() => string | Promise<string>) | undefined, logger: ILogger,
                logMessageContent: boolean, eventSourceConstructor: EventSourceConstructor) {
        this.httpClient = httpClient;
        this.accessTokenFactory = accessTokenFactory;
        this.logger = logger;
        this.logMessageContent = logMessageContent;
        this.eventSourceConstructor = eventSourceConstructor;

        this.onreceive = null;
        this.onclose = null;
    }

    public async connect(url: string, transferFormat: TransferFormat): Promise<void> {
        Arg.isRequired(url, "url");
        Arg.isRequired(transferFormat, "transferFormat");
        Arg.isIn(transferFormat, TransferFormat, "transferFormat");

        this.logger.log(LogLevel.Trace, "(SSE transport) Connecting.");

        // set url before accessTokenFactory because this.url is only for send and we set the auth header instead of the query string for send
        this.url = url;

        if (this.accessTokenFactory) {
            const token = await this.accessTokenFactory();
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
            if (typeof window !== "undefined") {
                eventSource = new this.eventSourceConstructor(url, { withCredentials: true });
            } else {
                // Non-browser passes cookies via the dictionary
                const cookies = this.httpClient.getCookieString(url);
                eventSource = new this.eventSourceConstructor(url, { withCredentials: true, headers: { Cookie: cookies } } as EventSourceInit);
            }

            try {
                eventSource.onmessage = (e: MessageEvent) => {
                    if (this.onreceive) {
                        try {
                            this.logger.log(LogLevel.Trace, `(SSE transport) data received. ${getDataDetail(e.data, this.logMessageContent)}.`);
                            this.onreceive(e.data);
                        } catch (error) {
                            this.close(error);
                            return;
                        }
                    }
                };

                eventSource.onerror = (e: MessageEvent) => {
                    const error = new Error(e.data || "Error occurred");
                    if (opened) {
                        this.close(error);
                    } else {
                        reject(error);
                    }
                };

                eventSource.onopen = () => {
                    this.logger.log(LogLevel.Information, `SSE connected to ${this.url}`);
                    this.eventSource = eventSource;
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
        if (!this.eventSource) {
            return Promise.reject(new Error("Cannot send until the transport is connected"));
        }
        return sendMessage(this.logger, "SSE", this.httpClient, this.url!, this.accessTokenFactory, data, this.logMessageContent);
    }

    public stop(): Promise<void> {
        this.close();
        return Promise.resolve();
    }

    private close(e?: Error) {
        if (this.eventSource) {
            this.eventSource.close();
            this.eventSource = undefined;

            if (this.onclose) {
                this.onclose(e);
            }
        }
    }
}
