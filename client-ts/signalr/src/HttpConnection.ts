// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

import { DataReceived, ConnectionClosed } from "./Common"
import { IConnection } from "./IConnection"
import { ITransport, TransferMode, TransportType, WebSocketTransport, ServerSentEventsTransport, LongPollingTransport } from "./Transports"
import { HttpClient, DefaultHttpClient } from "./HttpClient"
import { ILogger, LogLevel } from "./ILogger"
import { LoggerFactory } from "./Loggers"

export interface IHttpConnectionOptions {
    httpClient?: HttpClient;
    transport?: TransportType | ITransport;
    logger?: ILogger | LogLevel;
    accessTokenFactory?: () => string;
}

const enum ConnectionState {
    Connecting,
    Connected,
    Disconnected
}

interface INegotiateResponse {
    connectionId: string
    availableTransports: string[]
}

export class HttpConnection implements IConnection {
    private connectionState: ConnectionState;
    private baseUrl: string;
    private url: string;
    private readonly httpClient: HttpClient;
    private readonly logger: ILogger;
    private readonly options: IHttpConnectionOptions;
    private transport: ITransport;
    private connectionId: string;
    private startPromise: Promise<void>;

    readonly features: any = {};

    constructor(url: string, options: IHttpConnectionOptions = {}) {
        this.logger = LoggerFactory.createLogger(options.logger);
        this.baseUrl = this.resolveUrl(url);

        options = options || {};
        options.accessTokenFactory = options.accessTokenFactory || (() => null);

        this.httpClient = options.httpClient || new DefaultHttpClient();
        this.connectionState = ConnectionState.Disconnected;
        this.options = options;
    }

    async start(): Promise<void> {
        if (this.connectionState !== ConnectionState.Disconnected) {
            return Promise.reject(new Error("Cannot start a connection that is not in the 'Disconnected' state."));
        }

        this.connectionState = ConnectionState.Connecting;

        this.startPromise = this.startInternal();
        return this.startPromise;
    }

    private async startInternal(): Promise<void> {
        try {
            if (this.options.transport === TransportType.WebSockets) {
                // No need to add a connection ID in this case
                this.url = this.baseUrl;
                this.transport = this.createTransport(this.options.transport, [TransportType[TransportType.WebSockets]]);
            }
            else {
                let headers;
                let token = this.options.accessTokenFactory();
                if (token) {
                    headers = new Map<string, string>();
                    headers.set("Authorization", `Bearer ${token}`);
                }

                let negotiatePayload = await this.httpClient.post(this.resolveNegotiateUrl(this.baseUrl), {
                    content: "",
                    headers
                });

                let negotiateResponse: INegotiateResponse = JSON.parse(<string>negotiatePayload.content);
                this.connectionId = negotiateResponse.connectionId;

                // the user tries to stop the the connection when it is being started
                if (this.connectionState == ConnectionState.Disconnected) {
                    return;
                }

                if (this.connectionId) {
                    this.url = this.baseUrl + (this.baseUrl.indexOf("?") === -1 ? "?" : "&") + `id=${this.connectionId}`;
                    this.transport = this.createTransport(this.options.transport, negotiateResponse.availableTransports);
                }
            }

            this.transport.onreceive = this.onreceive;
            this.transport.onclose = e => this.stopConnection(true, e);

            let requestedTransferMode =
                this.features.transferMode === TransferMode.Binary
                    ? TransferMode.Binary
                    : TransferMode.Text;

            this.features.transferMode = await this.transport.connect(this.url, requestedTransferMode, this);

            // only change the state if we were connecting to not overwrite
            // the state if the connection is already marked as Disconnected
            this.changeState(ConnectionState.Connecting, ConnectionState.Connected);
        }
        catch (e) {
            this.logger.log(LogLevel.Error, "Failed to start the connection. " + e);
            this.connectionState = ConnectionState.Disconnected;
            this.transport = null;
            throw e;
        };
    }

    private createTransport(transport: TransportType | ITransport, availableTransports: string[]): ITransport {
        if ((transport === null || transport === undefined) && availableTransports.length > 0) {
            transport = TransportType[availableTransports[0]];
        }
        if (transport === TransportType.WebSockets && availableTransports.indexOf(TransportType[transport]) >= 0) {
            return new WebSocketTransport(this.options.accessTokenFactory, this.logger);
        }
        if (transport === TransportType.ServerSentEvents && availableTransports.indexOf(TransportType[transport]) >= 0) {
            return new ServerSentEventsTransport(this.httpClient, this.options.accessTokenFactory, this.logger);
        }
        if (transport === TransportType.LongPolling && availableTransports.indexOf(TransportType[transport]) >= 0) {
            return new LongPollingTransport(this.httpClient, this.options.accessTokenFactory, this.logger);
        }

        if (this.isITransport(transport)) {
            return transport;
        }

        throw new Error("No available transports found.");
    }

    private isITransport(transport: any): transport is ITransport {
        return typeof (transport) === "object" && "connect" in transport;
    }

    private changeState(from: ConnectionState, to: ConnectionState): Boolean {
        if (this.connectionState == from) {
            this.connectionState = to;
            return true;
        }
        return false;
    }

    send(data: any): Promise<void> {
        if (this.connectionState != ConnectionState.Connected) {
            throw new Error("Cannot send data if the connection is not in the 'Connected' State");
        }

        return this.transport.send(data);
    }

    async stop(error?: Error): Promise<void> {
        let previousState = this.connectionState;
        this.connectionState = ConnectionState.Disconnected;

        try {
            await this.startPromise;
        }
        catch (e) {
            // this exception is returned to the user as a rejected Promise from the start method
        }
        this.stopConnection(/*raiseClosed*/ previousState == ConnectionState.Connected, error);
    }

    private stopConnection(raiseClosed: Boolean, error?: Error) {
        if (this.transport) {
            this.transport.stop();
            this.transport = null;
        }

        if (error) {
            this.logger.log(LogLevel.Error, `Connection disconnected with error '${error}'.`);
        } else {
            this.logger.log(LogLevel.Information, "Connection disconnected.");
        }

        this.connectionState = ConnectionState.Disconnected;

        if (raiseClosed && this.onclose) {
            this.onclose(error);
        }
    }

    private resolveUrl(url: string): string {
        // startsWith is not supported in IE
        if (url.lastIndexOf("https://", 0) === 0 || url.lastIndexOf("http://", 0) === 0) {
            return url;
        }

        if (typeof window === 'undefined' || !window || !window.document) {
            throw new Error(`Cannot resolve '${url}'.`);
        }

        let parser = window.document.createElement("a");
        parser.href = url;

        let baseUrl = (!parser.protocol || parser.protocol === ":")
            ? `${window.document.location.protocol}//${(parser.host || window.document.location.host)}`
            : `${parser.protocol}//${parser.host}`;

        if (!url || url[0] != '/') {
            url = '/' + url;
        }

        let normalizedUrl = baseUrl + url;
        this.logger.log(LogLevel.Information, `Normalizing '${url}' to '${normalizedUrl}'`);
        return normalizedUrl;
    }

    private resolveNegotiateUrl(url: string): string {
        let index = url.indexOf("?");
        let negotiateUrl = url.substring(0, index === -1 ? url.length : index);
        if (negotiateUrl[negotiateUrl.length - 1] !== "/") {
            negotiateUrl += "/";
        }
        negotiateUrl += "negotiate";
        negotiateUrl += index === -1 ? "" : url.substring(index);
        return negotiateUrl;
    }

    onreceive: DataReceived;
    onclose: ConnectionClosed;
}
