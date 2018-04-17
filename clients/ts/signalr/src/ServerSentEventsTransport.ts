// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

import { HttpClient } from "./HttpClient";
import { ILogger, LogLevel } from "./ILogger";
import { ITransport, TransferFormat } from "./ITransport";
import { Arg, getDataDetail, sendMessage } from "./Utils";

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

        const token = await this.accessTokenFactory();
        if (token) {
            url += (url.indexOf("?") < 0 ? "?" : "&") + `access_token=${encodeURIComponent(token)}`;
        }

        this.url = url;
        return new Promise<void>((resolve, reject) => {
            let opened = false;
            if (transferFormat !== TransferFormat.Text) {
                reject(new Error("The Server-Sent Events transport only supports the 'Text' transfer format"));
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
                    const error = new Error(e.message || "Error occurred");
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
                return Promise.reject(e);
            }
        });
    }

    public async send(data: any): Promise<void> {
        if (!this.eventSource) {
            return Promise.reject(new Error("Cannot send until the transport is connected"));
        }
        return sendMessage(this.logger, "SSE", this.httpClient, this.url, this.accessTokenFactory, data, this.logMessageContent);
    }

    public stop(): Promise<void> {
        this.close();
        return Promise.resolve();
    }

    private close(e?: Error) {
        if (this.eventSource) {
            this.eventSource.close();
            this.eventSource = null;

            if (this.onclose) {
                this.onclose(e);
            }
        }
    }

    public onreceive: (data: string | ArrayBuffer) => void;
    public onclose: (error?: Error) => void;
}
