// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

import { DefaultHttpClient } from "./DefaultHttpClient";
import { HttpClient } from "./HttpClient";
import { IConnection } from "./IConnection";
import { IHttpConnectionOptions } from "./IHttpConnectionOptions";
import { ILogger, LogLevel } from "./ILogger";
import { HttpTransportType, ITransport, TransferFormat } from "./ITransport";
import { LongPollingTransport } from "./LongPollingTransport";
import { ServerSentEventsTransport } from "./ServerSentEventsTransport";
import { Arg, createLogger, getUserAgentHeader, Platform } from "./Utils";
import { WebSocketTransport } from "./WebSocketTransport";

/** @private */
const enum ConnectionState {
    Connecting = "Connecting",
    Connected = "Connected",
    Disconnected = "Disconnected",
    Disconnecting = "Disconnecting",
}

/** @private */
export interface INegotiateResponse {
    connectionId?: string;
    connectionToken?: string;
    negotiateVersion?: number;
    availableTransports?: IAvailableTransport[];
    url?: string;
    accessToken?: string;
    error?: string;
}

/** @private */
export interface IAvailableTransport {
    transport: keyof typeof HttpTransportType;
    transferFormats: Array<keyof typeof TransferFormat>;
}

const MAX_REDIRECTS = 100;

/** @private */
export class HttpConnection implements IConnection {
    private connectionState: ConnectionState;
    // connectionStarted is tracked independently from connectionState, so we can check if the
    // connection ever did successfully transition from connecting to connected before disconnecting.
    private connectionStarted: boolean;
    private readonly httpClient: HttpClient;
    private readonly logger: ILogger;
    private readonly options: IHttpConnectionOptions;
    private transport?: ITransport;
    private startInternalPromise?: Promise<void>;
    private stopPromise?: Promise<void>;
    private stopPromiseResolver: (value?: PromiseLike<void>) => void = () => {};
    private stopError?: Error;
    private accessTokenFactory?: () => string | Promise<string>;
    private sendQueue?: TransportSendQueue;

    public readonly features: any = {};
    public baseUrl: string;
    public connectionId?: string;
    public onreceive: ((data: string | ArrayBuffer) => void) | null;
    public onclose: ((e?: Error) => void) | null;

    private readonly negotiateVersion: number = 1;

    constructor(url: string, options: IHttpConnectionOptions = {}) {
        Arg.isRequired(url, "url");

        this.logger = createLogger(options.logger);
        this.baseUrl = this.resolveUrl(url);

        options = options || {};
        options.logMessageContent = options.logMessageContent === undefined ? false : options.logMessageContent;
        if (typeof options.withCredentials === "boolean" || options.withCredentials === undefined) {
            options.withCredentials = options.withCredentials === undefined ? true : options.withCredentials;
        } else {
            throw new Error("withCredentials option was not a 'boolean' or 'undefined' value");
        }

        let webSocketModule: any = null;
        let eventSourceModule: any = null;

        if (Platform.isNode && typeof require !== "undefined") {
            // In order to ignore the dynamic require in webpack builds we need to do this magic
            // @ts-ignore: TS doesn't know about these names
            const requireFunc = typeof __webpack_require__ === "function" ? __non_webpack_require__ : require;
            webSocketModule = requireFunc("ws");
            eventSourceModule = requireFunc("eventsource");
        }

        if (!Platform.isNode && typeof WebSocket !== "undefined" && !options.WebSocket) {
            options.WebSocket = WebSocket;
        } else if (Platform.isNode && !options.WebSocket) {
            if (webSocketModule) {
                options.WebSocket = webSocketModule;
            }
        }

        if (!Platform.isNode && typeof EventSource !== "undefined" && !options.EventSource) {
            options.EventSource = EventSource;
        } else if (Platform.isNode && !options.EventSource) {
            if (typeof eventSourceModule !== "undefined") {
                options.EventSource = eventSourceModule;
            }
        }

        this.httpClient = options.httpClient || new DefaultHttpClient(this.logger);
        this.connectionState = ConnectionState.Disconnected;
        this.connectionStarted = false;
        this.options = options;

        this.onreceive = null;
        this.onclose = null;
    }

    public start(): Promise<void>;
    public start(transferFormat: TransferFormat): Promise<void>;
    public async start(transferFormat?: TransferFormat): Promise<void> {
        transferFormat = transferFormat || TransferFormat.Binary;

        Arg.isIn(transferFormat, TransferFormat, "transferFormat");

        this.logger.log(LogLevel.Debug, `Starting connection with transfer format '${TransferFormat[transferFormat]}'.`);

        if (this.connectionState !== ConnectionState.Disconnected) {
            return Promise.reject(new Error("Cannot start an HttpConnection that is not in the 'Disconnected' state."));
        }

        this.connectionState = ConnectionState.Connecting;

        this.startInternalPromise = this.startInternal(transferFormat);
        await this.startInternalPromise;

        // The TypeScript compiler thinks that connectionState must be Connecting here. The TypeScript compiler is wrong.
        if (this.connectionState as any === ConnectionState.Disconnecting) {
            // stop() was called and transitioned the client into the Disconnecting state.
            const message = "Failed to start the HttpConnection before stop() was called.";
            this.logger.log(LogLevel.Error, message);

            // We cannot await stopPromise inside startInternal since stopInternal awaits the startInternalPromise.
            await this.stopPromise;

            return Promise.reject(new Error(message));
        } else if (this.connectionState as any !== ConnectionState.Connected) {
            // stop() was called and transitioned the client into the Disconnecting state.
            const message = "HttpConnection.startInternal completed gracefully but didn't enter the connection into the connected state!";
            this.logger.log(LogLevel.Error, message);
            return Promise.reject(new Error(message));
        }

        this.connectionStarted = true;
    }

    public send(data: string | ArrayBuffer): Promise<void> {
        if (this.connectionState !== ConnectionState.Connected) {
            return Promise.reject(new Error("Cannot send data if the connection is not in the 'Connected' State."));
        }

        if (!this.sendQueue) {
            this.sendQueue = new TransportSendQueue(this.transport!);
        }

        // Transport will not be null if state is connected
        return this.sendQueue.send(data);
    }

    public async stop(error?: Error): Promise<void> {
        if (this.connectionState === ConnectionState.Disconnected) {
            this.logger.log(LogLevel.Debug, `Call to HttpConnection.stop(${error}) ignored because the connection is already in the disconnected state.`);
            return Promise.resolve();
        }

        if (this.connectionState === ConnectionState.Disconnecting) {
            this.logger.log(LogLevel.Debug, `Call to HttpConnection.stop(${error}) ignored because the connection is already in the disconnecting state.`);
            return this.stopPromise;
        }

        this.connectionState = ConnectionState.Disconnecting;

        this.stopPromise = new Promise((resolve) => {
            // Don't complete stop() until stopConnection() completes.
            this.stopPromiseResolver = resolve;
        });

        // stopInternal should never throw so just observe it.
        await this.stopInternal(error);
        await this.stopPromise;
    }

    private async stopInternal(error?: Error): Promise<void> {
        // Set error as soon as possible otherwise there is a race between
        // the transport closing and providing an error and the error from a close message
        // We would prefer the close message error.
        this.stopError = error;

        try {
            await this.startInternalPromise;
        } catch (e) {
            // This exception is returned to the user as a rejected Promise from the start method.
        }

        // The transport's onclose will trigger stopConnection which will run our onclose event.
        // The transport should always be set if currently connected. If it wasn't set, it's likely because
        // stop was called during start() and start() failed.
        if (this.transport) {
            try {
                await this.transport.stop();
            } catch (e) {
                this.logger.log(LogLevel.Error, `HttpConnection.transport.stop() threw error '${e}'.`);
                this.stopConnection();
            }

            this.transport = undefined;
        } else {
            this.logger.log(LogLevel.Debug, "HttpConnection.transport is undefined in HttpConnection.stop() because start() failed.");
        }
    }

    private async startInternal(transferFormat: TransferFormat): Promise<void> {
        // Store the original base url and the access token factory since they may change
        // as part of negotiating
        let url = this.baseUrl;
        this.accessTokenFactory = this.options.accessTokenFactory;

        try {
            if (this.options.skipNegotiation) {
                if (this.options.transport === HttpTransportType.WebSockets) {
                    // No need to add a connection ID in this case
                    this.transport = this.constructTransport(HttpTransportType.WebSockets);
                    // We should just call connect directly in this case.
                    // No fallback or negotiate in this case.
                    await this.startTransport(url, transferFormat);
                } else {
                    throw new Error("Negotiation can only be skipped when using the WebSocket transport directly.");
                }
            } else {
                let negotiateResponse: INegotiateResponse | null = null;
                let redirects = 0;

                do {
                    negotiateResponse = await this.getNegotiationResponse(url);
                    // the user tries to stop the connection when it is being started
                    if (this.connectionState === ConnectionState.Disconnecting || this.connectionState === ConnectionState.Disconnected) {
                        throw new Error("The connection was stopped during negotiation.");
                    }

                    if (negotiateResponse.error) {
                        throw new Error(negotiateResponse.error);
                    }

                    if ((negotiateResponse as any).ProtocolVersion) {
                        throw new Error("Detected a connection attempt to an ASP.NET SignalR Server. This client only supports connecting to an ASP.NET Core SignalR Server. See https://aka.ms/signalr-core-differences for details.");
                    }

                    if (negotiateResponse.url) {
                        url = negotiateResponse.url;
                    }

                    if (negotiateResponse.accessToken) {
                        // Replace the current access token factory with one that uses
                        // the returned access token
                        const accessToken = negotiateResponse.accessToken;
                        this.accessTokenFactory = () => accessToken;
                    }

                    redirects++;
                }
                while (negotiateResponse.url && redirects < MAX_REDIRECTS);

                if (redirects === MAX_REDIRECTS && negotiateResponse.url) {
                    throw new Error("Negotiate redirection limit exceeded.");
                }

                await this.createTransport(url, this.options.transport, negotiateResponse, transferFormat);
            }

            if (this.transport instanceof LongPollingTransport) {
                this.features.inherentKeepAlive = true;
            }

            if (this.connectionState === ConnectionState.Connecting) {
                // Ensure the connection transitions to the connected state prior to completing this.startInternalPromise.
                // start() will handle the case when stop was called and startInternal exits still in the disconnecting state.
                this.logger.log(LogLevel.Debug, "The HttpConnection connected successfully.");
                this.connectionState = ConnectionState.Connected;
            }

            // stop() is waiting on us via this.startInternalPromise so keep this.transport around so it can clean up.
            // This is the only case startInternal can exit in neither the connected nor disconnected state because stopConnection()
            // will transition to the disconnected state. start() will wait for the transition using the stopPromise.
        } catch (e) {
            this.logger.log(LogLevel.Error, "Failed to start the connection: " + e);
            this.connectionState = ConnectionState.Disconnected;
            this.transport = undefined;

            // if start fails, any active calls to stop assume that start will complete the stop promise
            this.stopPromiseResolver();
            return Promise.reject(e);
        }
    }

    private async getNegotiationResponse(url: string): Promise<INegotiateResponse> {
        const headers = {};
        if (this.accessTokenFactory) {
            const token = await this.accessTokenFactory();
            if (token) {
                headers[`Authorization`] = `Bearer ${token}`;
            }
        }

        const [name, value] = getUserAgentHeader();
        headers[name] = value;

        const negotiateUrl = this.resolveNegotiateUrl(url);
        this.logger.log(LogLevel.Debug, `Sending negotiation request: ${negotiateUrl}.`);
        try {
            const response = await this.httpClient.post(negotiateUrl, {
                content: "",
                headers: { ...headers, ...this.options.headers },
                withCredentials: this.options.withCredentials,
            });

            if (response.statusCode !== 200) {
                return Promise.reject(new Error(`Unexpected status code returned from negotiate '${response.statusCode}'`));
            }

            const negotiateResponse = JSON.parse(response.content as string) as INegotiateResponse;
            if (!negotiateResponse.negotiateVersion || negotiateResponse.negotiateVersion < 1) {
                // Negotiate version 0 doesn't use connectionToken
                // So we set it equal to connectionId so all our logic can use connectionToken without being aware of the negotiate version
                negotiateResponse.connectionToken = negotiateResponse.connectionId;
            }
            return negotiateResponse;
        } catch (e) {
            this.logger.log(LogLevel.Error, "Failed to complete negotiation with the server: " + e);
            return Promise.reject(e);
        }
    }

    private createConnectUrl(url: string, connectionToken: string | null | undefined) {
        if (!connectionToken) {
            return url;
        }

        return url + (url.indexOf("?") === -1 ? "?" : "&") + `id=${connectionToken}`;
    }

    private async createTransport(url: string, requestedTransport: HttpTransportType | ITransport | undefined, negotiateResponse: INegotiateResponse, requestedTransferFormat: TransferFormat): Promise<void> {
        let connectUrl = this.createConnectUrl(url, negotiateResponse.connectionToken);
        if (this.isITransport(requestedTransport)) {
            this.logger.log(LogLevel.Debug, "Connection was provided an instance of ITransport, using that directly.");
            this.transport = requestedTransport;
            await this.startTransport(connectUrl, requestedTransferFormat);

            this.connectionId = negotiateResponse.connectionId;
            return;
        }

        const transportExceptions: any[] = [];
        const transports = negotiateResponse.availableTransports || [];
        let negotiate: INegotiateResponse | undefined = negotiateResponse;
        for (const endpoint of transports) {
            const transportOrError = this.resolveTransportOrError(endpoint, requestedTransport, requestedTransferFormat);
            if (transportOrError instanceof Error) {
                // Store the error and continue, we don't want to cause a re-negotiate in these cases
                transportExceptions.push(`${endpoint.transport} failed: ${transportOrError}`);
            } else if (this.isITransport(transportOrError)) {
                this.transport = transportOrError;
                if (!negotiate) {
                    try {
                        negotiate = await this.getNegotiationResponse(url);
                    } catch (ex) {
                        return Promise.reject(ex);
                    }
                    connectUrl = this.createConnectUrl(url, negotiate.connectionToken);
                }
                try {
                    await this.startTransport(connectUrl, requestedTransferFormat);
                    this.connectionId = negotiate.connectionId;
                    return;
                } catch (ex) {
                    this.logger.log(LogLevel.Error, `Failed to start the transport '${endpoint.transport}': ${ex}`);
                    negotiate = undefined;
                    transportExceptions.push(`${endpoint.transport} failed: ${ex}`);

                    if (this.connectionState !== ConnectionState.Connecting) {
                        const message = "Failed to select transport before stop() was called.";
                        this.logger.log(LogLevel.Debug, message);
                        return Promise.reject(new Error(message));
                    }
                }
            }
        }

        if (transportExceptions.length > 0) {
            return Promise.reject(new Error(`Unable to connect to the server with any of the available transports. ${transportExceptions.join(" ")}`));
        }
        return Promise.reject(new Error("None of the transports supported by the client are supported by the server."));
    }

    private constructTransport(transport: HttpTransportType): ITransport {
        switch (transport) {
            case HttpTransportType.WebSockets:
                if (!this.options.WebSocket) {
                    throw new Error("'WebSocket' is not supported in your environment.");
                }
                return new WebSocketTransport(this.httpClient, this.accessTokenFactory, this.logger, this.options.logMessageContent || false, this.options.WebSocket, this.options.headers || {});
            case HttpTransportType.ServerSentEvents:
                if (!this.options.EventSource) {
                    throw new Error("'EventSource' is not supported in your environment.");
                }
                return new ServerSentEventsTransport(this.httpClient, this.accessTokenFactory, this.logger, this.options.logMessageContent || false, this.options.EventSource, this.options.withCredentials!, this.options.headers || {});
            case HttpTransportType.LongPolling:
                return new LongPollingTransport(this.httpClient, this.accessTokenFactory, this.logger, this.options.logMessageContent || false, this.options.withCredentials!, this.options.headers || {});
            default:
                throw new Error(`Unknown transport: ${transport}.`);
        }
    }

    private startTransport(url: string, transferFormat: TransferFormat): Promise<void> {
        this.transport!.onreceive = this.onreceive;
        this.transport!.onclose = (e) => this.stopConnection(e);
        return this.transport!.connect(url, transferFormat);
    }

    private resolveTransportOrError(endpoint: IAvailableTransport, requestedTransport: HttpTransportType | undefined, requestedTransferFormat: TransferFormat): ITransport | Error {
        const transport = HttpTransportType[endpoint.transport];
        if (transport === null || transport === undefined) {
            this.logger.log(LogLevel.Debug, `Skipping transport '${endpoint.transport}' because it is not supported by this client.`);
            return new Error(`Skipping transport '${endpoint.transport}' because it is not supported by this client.`);
        } else {
            if (transportMatches(requestedTransport, transport)) {
                const transferFormats = endpoint.transferFormats.map((s) => TransferFormat[s]);
                if (transferFormats.indexOf(requestedTransferFormat) >= 0) {
                    if ((transport === HttpTransportType.WebSockets && !this.options.WebSocket) ||
                        (transport === HttpTransportType.ServerSentEvents && !this.options.EventSource)) {
                        this.logger.log(LogLevel.Debug, `Skipping transport '${HttpTransportType[transport]}' because it is not supported in your environment.'`);
                        return new Error(`'${HttpTransportType[transport]}' is not supported in your environment.`);
                    } else {
                        this.logger.log(LogLevel.Debug, `Selecting transport '${HttpTransportType[transport]}'.`);
                        try {
                            return this.constructTransport(transport);
                        } catch (ex) {
                            return ex;
                        }
                    }
                } else {
                    this.logger.log(LogLevel.Debug, `Skipping transport '${HttpTransportType[transport]}' because it does not support the requested transfer format '${TransferFormat[requestedTransferFormat]}'.`);
                    return new Error(`'${HttpTransportType[transport]}' does not support ${TransferFormat[requestedTransferFormat]}.`);
                }
            } else {
                this.logger.log(LogLevel.Debug, `Skipping transport '${HttpTransportType[transport]}' because it was disabled by the client.`);
                return new Error(`'${HttpTransportType[transport]}' is disabled by the client.`);
            }
        }
    }

    private isITransport(transport: any): transport is ITransport {
        return transport && typeof (transport) === "object" && "connect" in transport;
    }

    private stopConnection(error?: Error): void {
        this.logger.log(LogLevel.Debug, `HttpConnection.stopConnection(${error}) called while in state ${this.connectionState}.`);

        this.transport = undefined;

        // If we have a stopError, it takes precedence over the error from the transport
        error = this.stopError || error;
        this.stopError = undefined;

        if (this.connectionState === ConnectionState.Disconnected) {
            this.logger.log(LogLevel.Debug, `Call to HttpConnection.stopConnection(${error}) was ignored because the connection is already in the disconnected state.`);
            return;
        }

        if (this.connectionState === ConnectionState.Connecting) {
            this.logger.log(LogLevel.Warning, `Call to HttpConnection.stopConnection(${error}) was ignored because the connection is still in the connecting state.`);
            throw new Error(`HttpConnection.stopConnection(${error}) was called while the connection is still in the connecting state.`);
        }

        if (this.connectionState === ConnectionState.Disconnecting) {
            // A call to stop() induced this call to stopConnection and needs to be completed.
            // Any stop() awaiters will be scheduled to continue after the onclose callback fires.
            this.stopPromiseResolver();
        }

        if (error) {
            this.logger.log(LogLevel.Error, `Connection disconnected with error '${error}'.`);
        } else {
            this.logger.log(LogLevel.Information, "Connection disconnected.");
        }

        if (this.sendQueue) {
            this.sendQueue.stop().catch((e) => {
                this.logger.log(LogLevel.Error, `TransportSendQueue.stop() threw error '${e}'.`);
            });
            this.sendQueue = undefined;
        }

        this.connectionId = undefined;
        this.connectionState = ConnectionState.Disconnected;

        if (this.connectionStarted) {
            this.connectionStarted = false;
            try {
                if (this.onclose) {
                    this.onclose(error);
                }
            } catch (e) {
                this.logger.log(LogLevel.Error, `HttpConnection.onclose(${error}) threw error '${e}'.`);
            }
        }
    }

    private resolveUrl(url: string): string {
        // startsWith is not supported in IE
        if (url.lastIndexOf("https://", 0) === 0 || url.lastIndexOf("http://", 0) === 0) {
            return url;
        }

        if (!Platform.isBrowser || !window.document) {
            throw new Error(`Cannot resolve '${url}'.`);
        }

        // Setting the url to the href propery of an anchor tag handles normalization
        // for us. There are 3 main cases.
        // 1. Relative path normalization e.g "b" -> "http://localhost:5000/a/b"
        // 2. Absolute path normalization e.g "/a/b" -> "http://localhost:5000/a/b"
        // 3. Networkpath reference normalization e.g "//localhost:5000/a/b" -> "http://localhost:5000/a/b"
        const aTag = window.document.createElement("a");
        aTag.href = url;

        this.logger.log(LogLevel.Information, `Normalizing '${url}' to '${aTag.href}'.`);
        return aTag.href;
    }

    private resolveNegotiateUrl(url: string): string {
        const index = url.indexOf("?");
        let negotiateUrl = url.substring(0, index === -1 ? url.length : index);
        if (negotiateUrl[negotiateUrl.length - 1] !== "/") {
            negotiateUrl += "/";
        }
        negotiateUrl += "negotiate";
        negotiateUrl += index === -1 ? "" : url.substring(index);

        if (negotiateUrl.indexOf("negotiateVersion") === -1) {
            negotiateUrl += index === -1 ? "?" : "&";
            negotiateUrl += "negotiateVersion=" + this.negotiateVersion;
        }
        return negotiateUrl;
    }
}

function transportMatches(requestedTransport: HttpTransportType | undefined, actualTransport: HttpTransportType) {
    return !requestedTransport || ((actualTransport & requestedTransport) !== 0);
}

/** @private */
export class TransportSendQueue {
    private buffer: any[] = [];
    private sendBufferedData: PromiseSource;
    private executing: boolean = true;
    private transportResult?: PromiseSource;
    private sendLoopPromise: Promise<void>;

    constructor(private readonly transport: ITransport) {
        this.sendBufferedData = new PromiseSource();
        this.transportResult = new PromiseSource();

        this.sendLoopPromise = this.sendLoop();
    }

    public send(data: string | ArrayBuffer): Promise<void> {
        this.bufferData(data);
        if (!this.transportResult) {
            this.transportResult = new PromiseSource();
        }
        return this.transportResult.promise;
    }

    public stop(): Promise<void> {
        this.executing = false;
        this.sendBufferedData.resolve();
        return this.sendLoopPromise;
    }

    private bufferData(data: string | ArrayBuffer): void {
        if (this.buffer.length && typeof(this.buffer[0]) !== typeof(data)) {
            throw new Error(`Expected data to be of type ${typeof(this.buffer)} but was of type ${typeof(data)}`);
        }

        this.buffer.push(data);
        this.sendBufferedData.resolve();
    }

    private async sendLoop(): Promise<void> {
        while (true) {
            await this.sendBufferedData.promise;

            if (!this.executing) {
                if (this.transportResult) {
                    this.transportResult.reject("Connection stopped.");
                }

                break;
            }

            this.sendBufferedData = new PromiseSource();

            const transportResult = this.transportResult!;
            this.transportResult = undefined;

            const data = typeof(this.buffer[0]) === "string" ?
                this.buffer.join("") :
                TransportSendQueue.concatBuffers(this.buffer);

            this.buffer.length = 0;

            try {
                await this.transport.send(data);
                transportResult.resolve();
            } catch (error) {
                transportResult.reject(error);
            }
        }
    }

    private static concatBuffers(arrayBuffers: ArrayBuffer[]): ArrayBuffer {
        const totalLength = arrayBuffers.map((b) => b.byteLength).reduce((a, b) => a + b);
        const result = new Uint8Array(totalLength);
        let offset = 0;
        for (const item of arrayBuffers) {
            result.set(new Uint8Array(item), offset);
            offset += item.byteLength;
        }

        return result.buffer;
    }
}

class PromiseSource {
    private resolver?: () => void;
    private rejecter!: (reason?: any) => void;
    public promise: Promise<void>;

    constructor() {
        this.promise = new Promise((resolve, reject) => [this.resolver, this.rejecter] = [resolve, reject]);
    }

    public resolve(): void {
        this.resolver!();
    }

    public reject(reason?: any): void {
        this.rejecter!(reason);
    }
}
