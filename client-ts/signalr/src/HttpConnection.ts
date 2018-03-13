// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

import { ConnectionClosed, DataReceived } from "./Common";
import { DefaultHttpClient, HttpClient } from "./HttpClient";
import { IConnection } from "./IConnection";
import { ILogger, LogLevel } from "./ILogger";
import { LoggerFactory } from "./Loggers";
import { ITransport, LongPollingTransport, ServerSentEventsTransport, TransferFormat, TransportType, WebSocketTransport } from "./Transports";
import { Arg } from "./Utils";

export interface IHttpConnectionOptions {
    httpClient?: HttpClient;
    transport?: TransportType | ITransport;
    logger?: ILogger | LogLevel;
    accessTokenFactory?: () => string;
}

const enum ConnectionState {
    Connecting,
    Connected,
    Disconnected,
}

interface INegotiateResponse {
    connectionId: string;
    availableTransports: IAvailableTransport[];
}

interface IAvailableTransport {
    transport: keyof typeof TransportType;
    transferFormats: Array<keyof typeof TransferFormat>;
}

export class HttpConnection implements IConnection {
    private connectionState: ConnectionState;
    private baseUrl: string;
    private url: string;
    private readonly httpClient: HttpClient;
    private readonly logger: ILogger;
    private readonly options: IHttpConnectionOptions;
    private readonly transferFormat: TransferFormat;
    private transport: ITransport;
    private connectionId: string;
    private startPromise: Promise<void>;

    public readonly features: any = {};

    constructor(url: string, transferFormat: TransferFormat, options: IHttpConnectionOptions = {}) {
        Arg.isRequired(url, "url");
        Arg.isRequired(transferFormat, "transferFormat");
        Arg.isIn(transferFormat, TransferFormat, "transferFormat");

        this.transferFormat = transferFormat;
        this.logger = LoggerFactory.createLogger(options.logger);
        this.baseUrl = this.resolveUrl(url);

        options = options || {};
        options.accessTokenFactory = options.accessTokenFactory || (() => null);

        this.httpClient = options.httpClient || new DefaultHttpClient();
        this.connectionState = ConnectionState.Disconnected;
        this.options = options;
    }

    public async start(): Promise<void> {
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
                this.transport = this.constructTransport(TransportType.WebSockets);
            } else {
                let headers;
                const token = this.options.accessTokenFactory();
                if (token) {
                    headers = new Map<string, string>();
                    headers.set("Authorization", `Bearer ${token}`);
                }

                const negotiatePayload = await this.httpClient.post(this.resolveNegotiateUrl(this.baseUrl), {
                    content: "",
                    headers,
                });

                const negotiateResponse: INegotiateResponse = JSON.parse(negotiatePayload.content as string);
                this.connectionId = negotiateResponse.connectionId;

                // the user tries to stop the the connection when it is being started
                if (this.connectionState === ConnectionState.Disconnected) {
                    return;
                }

                if (this.connectionId) {
                    this.url = this.baseUrl + (this.baseUrl.indexOf("?") === -1 ? "?" : "&") + `id=${this.connectionId}`;
                    this.transport = this.createTransport(this.options.transport, negotiateResponse.availableTransports, this.transferFormat);
                }
            }

            this.transport.onreceive = this.onreceive;
            this.transport.onclose = (e) => this.stopConnection(true, e);

            await this.transport.connect(this.url, this.transferFormat, this);

            // only change the state if we were connecting to not overwrite
            // the state if the connection is already marked as Disconnected
            this.changeState(ConnectionState.Connecting, ConnectionState.Connected);
        } catch (e) {
            this.logger.log(LogLevel.Error, "Failed to start the connection: " + e);
            this.connectionState = ConnectionState.Disconnected;
            this.transport = null;
            throw e;
        }
    }

    private createTransport(requestedTransport: TransportType | ITransport, availableTransports: IAvailableTransport[], requestedTransferFormat: TransferFormat): ITransport {
        if (this.isITransport(requestedTransport)) {
            this.logger.log(LogLevel.Trace, "Connection was provided an instance of ITransport, using that directly.");
            return requestedTransport;
        }

        for (const endpoint of availableTransports) {
            const transport = this.resolveTransport(endpoint, requestedTransport, requestedTransferFormat);
            if (transport) {
                return this.constructTransport(transport);
            }
        }

        throw new Error("Unable to initialize any of the available transports.");
    }

    private constructTransport(transport: TransportType) {
        switch (transport) {
            case TransportType.WebSockets:
                return new WebSocketTransport(this.options.accessTokenFactory, this.logger);
            case TransportType.ServerSentEvents:
                return new ServerSentEventsTransport(this.httpClient, this.options.accessTokenFactory, this.logger);
            case TransportType.LongPolling:
                return new LongPollingTransport(this.httpClient, this.options.accessTokenFactory, this.logger);
            default:
                throw new Error(`Unknown transport: ${transport}.`);
        }
    }

    private resolveTransport(endpoint: IAvailableTransport, requestedTransport: TransportType, requestedTransferFormat: TransferFormat): TransportType | null {
        const transport = TransportType[endpoint.transport];
        if (!transport) {
            this.logger.log(LogLevel.Trace, `Skipping transport '${endpoint.transport}' because it is not supported by this client.`);
        } else {
            const transferFormats = endpoint.transferFormats.map((s) => TransferFormat[s]);
            if (!requestedTransport || transport === requestedTransport) {
                if (transferFormats.indexOf(requestedTransferFormat) >= 0) {
                    this.logger.log(LogLevel.Trace, `Selecting transport '${transport}'`);
                    return transport;
                } else {
                    this.logger.log(LogLevel.Trace, `Skipping transport '${transport}' because it does not support the requested transfer format '${requestedTransferFormat}'.`);
                }
            } else {
                this.logger.log(LogLevel.Trace, `Skipping transport '${transport}' because it was disabled by the client.`);
            }
        }
        return null;
    }

    private isITransport(transport: any): transport is ITransport {
        return typeof (transport) === "object" && "connect" in transport;
    }

    private changeState(from: ConnectionState, to: ConnectionState): boolean {
        if (this.connectionState === from) {
            this.connectionState = to;
            return true;
        }
        return false;
    }

    public send(data: any): Promise<void> {
        if (this.connectionState !== ConnectionState.Connected) {
            throw new Error("Cannot send data if the connection is not in the 'Connected' State.");
        }

        return this.transport.send(data);
    }

    public async stop(error?: Error): Promise<void> {
        const previousState = this.connectionState;
        this.connectionState = ConnectionState.Disconnected;

        try {
            await this.startPromise;
        } catch (e) {
            // this exception is returned to the user as a rejected Promise from the start method
        }
        this.stopConnection(/*raiseClosed*/ previousState === ConnectionState.Connected, error);
    }

    private stopConnection(raiseClosed: boolean, error?: Error) {
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

        if (typeof window === "undefined" || !window || !window.document) {
            throw new Error(`Cannot resolve '${url}'.`);
        }

        const parser = window.document.createElement("a");
        parser.href = url;

        const baseUrl = (!parser.protocol || parser.protocol === ":")
            ? `${window.document.location.protocol}//${(parser.host || window.document.location.host)}`
            : `${parser.protocol}//${parser.host}`;

        if (!url || url[0] !== "/") {
            url = "/" + url;
        }

        const normalizedUrl = baseUrl + url;
        this.logger.log(LogLevel.Information, `Normalizing '${url}' to '${normalizedUrl}'.`);
        return normalizedUrl;
    }

    private resolveNegotiateUrl(url: string): string {
        const index = url.indexOf("?");
        let negotiateUrl = url.substring(0, index === -1 ? url.length : index);
        if (negotiateUrl[negotiateUrl.length - 1] !== "/") {
            negotiateUrl += "/";
        }
        negotiateUrl += "negotiate";
        negotiateUrl += index === -1 ? "" : url.substring(index);
        return negotiateUrl;
    }

    public onreceive: DataReceived;
    public onclose: ConnectionClosed;
}
