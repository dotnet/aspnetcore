// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

import { ILogger, LogLevel } from "./ILogger";
import { ITransport, TransferFormat } from "./ITransport";
import { Arg, getDataDetail } from "./Utils";

/** @private */
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
        if (token) {
            url += (url.indexOf("?") < 0 ? "?" : "&") + `access_token=${encodeURIComponent(token)}`;
        }

        return new Promise<void>((resolve, reject) => {
            url = url.replace(/^http/, "ws");
            const webSocket = new WebSocket(url);
            if (transferFormat === TransferFormat.Binary) {
                webSocket.binaryType = "arraybuffer";
            }

            // tslint:disable-next-line:variable-name
            webSocket.onopen = (_event: Event) => {
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
                this.logger.log(LogLevel.Trace, "(WebSockets transport) socket closed.");
                if (this.onclose) {
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

    public onreceive: (data: string | ArrayBuffer) => void;
    public onclose: (error?: Error) => void;
}
