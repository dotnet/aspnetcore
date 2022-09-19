// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import { AccessTokenHttpClient } from "./AccessTokenHttpClient";
import { DefaultHttpClient } from "./DefaultHttpClient";
import { AggregateErrors, DisabledTransportError, FailedToNegotiateWithServerError, FailedToStartTransportError, HttpError, UnsupportedTransportError, AbortError } from "./Errors";
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
    transferFormats: (keyof typeof TransferFormat)[];
}

const MAX_REDIRECTS = 100;

/** @private */
export class HttpConnection implements IConnection {
    private _connectionState: ConnectionState;
    // connectionStarted is tracked independently from connectionState, so we can check if the
    // connection ever did successfully transition from connecting to connected before disconnecting.
    private _connectionStarted: boolean;
    private readonly _httpClient: AccessTokenHttpClient;
    private readonly _logger: ILogger;
    private readonly _options: IHttpConnectionOptions;
    // Needs to not start with _ to be available for tests
    // eslint-disable-next-line @typescript-eslint/naming-convention
    private transport?: ITransport;
    private _startInternalPromise?: Promise<void>;
    private _stopPromise?: Promise<void>;
    private _stopPromiseResolver: (value?: PromiseLike<void>) => void = () => {};
    private _stopError?: Error;
    private _accessTokenFactory?: () => string | Promise<string>;
    private _sendQueue?: TransportSendQueue;

    public readonly features: any = {};
    public baseUrl: string;
    public connectionId?: string;
    public onreceive: ((data: string | ArrayBuffer) => void) | null;
    public onclose: ((e?: Error) => void) | null;

    private readonly _negotiateVersion: number = 1;

    constructor(url: string, options: IHttpConnectionOptions = {}) {
        Arg.isRequired(url, "url");

        this._logger = createLogger(options.logger);
        this.baseUrl = this._resolveUrl(url);

        options = options || {};
        options.logMessageContent = options.logMessageContent === undefined ? false : options.logMessageContent;
        if (typeof options.withCredentials === "boolean" || options.withCredentials === undefined) {
            options.withCredentials = options.withCredentials === undefined ? true : options.withCredentials;
        } else {
            throw new Error("withCredentials option was not a 'boolean' or 'undefined' value");
        }
        options.timeout = options.timeout === undefined ? 100 * 1000 : options.timeout;

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

        this._httpClient = new AccessTokenHttpClient(options.httpClient || new DefaultHttpClient(this._logger), options.accessTokenFactory);
        this._connectionState = ConnectionState.Disconnected;
        this._connectionStarted = false;
        this._options = options;

        this.onreceive = null;
        this.onclose = null;
    }

    public start(): Promise<void>;
    public start(transferFormat: TransferFormat): Promise<void>;
    public async start(transferFormat?: TransferFormat): Promise<void> {
        transferFormat = transferFormat || TransferFormat.Binary;

        Arg.isIn(transferFormat, TransferFormat, "transferFormat");

        this._logger.log(LogLevel.Debug, `Starting connection with transfer format '${TransferFormat[transferFormat]}'.`);

        if (this._connectionState !== ConnectionState.Disconnected) {
            return Promise.reject(new Error("Cannot start an HttpConnection that is not in the 'Disconnected' state."));
        }

        this._connectionState = ConnectionState.Connecting;

        this._startInternalPromise = this._startInternal(transferFormat);
        await this._startInternalPromise;

        // The TypeScript compiler thinks that connectionState must be Connecting here. The TypeScript compiler is wrong.
        if (this._connectionState as any === ConnectionState.Disconnecting) {
            // stop() was called and transitioned the client into the Disconnecting state.
            const message = "Failed to start the HttpConnection before stop() was called.";
            this._logger.log(LogLevel.Error, message);

            // We cannot await stopPromise inside startInternal since stopInternal awaits the startInternalPromise.
            await this._stopPromise;

            return Promise.reject(new AbortError(message));
        } else if (this._connectionState as any !== ConnectionState.Connected) {
            // stop() was called and transitioned the client into the Disconnecting state.
            const message = "HttpConnection.startInternal completed gracefully but didn't enter the connection into the connected state!";
            this._logger.log(LogLevel.Error, message);
            return Promise.reject(new AbortError(message));
        }

        this._connectionStarted = true;
    }

    public send(data: string | ArrayBuffer): Promise<void> {
        if (this._connectionState !== ConnectionState.Connected) {
            return Promise.reject(new Error("Cannot send data if the connection is not in the 'Connected' State."));
        }

        if (!this._sendQueue) {
            this._sendQueue = new TransportSendQueue(this.transport!);
        }

        // Transport will not be null if state is connected
        return this._sendQueue.send(data);
    }

    public async stop(error?: Error): Promise<void> {
        if (this._connectionState === ConnectionState.Disconnected) {
            this._logger.log(LogLevel.Debug, `Call to HttpConnection.stop(${error}) ignored because the connection is already in the disconnected state.`);
            return Promise.resolve();
        }

        if (this._connectionState === ConnectionState.Disconnecting) {
            this._logger.log(LogLevel.Debug, `Call to HttpConnection.stop(${error}) ignored because the connection is already in the disconnecting state.`);
            return this._stopPromise;
        }

        this._connectionState = ConnectionState.Disconnecting;

        this._stopPromise = new Promise((resolve) => {
            // Don't complete stop() until stopConnection() completes.
            this._stopPromiseResolver = resolve;
        });

        // stopInternal should never throw so just observe it.
        await this._stopInternal(error);
        await this._stopPromise;
    }

    private async _stopInternal(error?: Error): Promise<void> {
        // Set error as soon as possible otherwise there is a race between
        // the transport closing and providing an error and the error from a close message
        // We would prefer the close message error.
        this._stopError = error;

        try {
            await this._startInternalPromise;
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
                this._logger.log(LogLevel.Error, `HttpConnection.transport.stop() threw error '${e}'.`);
                this._stopConnection();
            }

            this.transport = undefined;
        } else {
            this._logger.log(LogLevel.Debug, "HttpConnection.transport is undefined in HttpConnection.stop() because start() failed.");
        }
    }

    private async _startInternal(transferFormat: TransferFormat): Promise<void> {
        // Store the original base url and the access token factory since they may change
        // as part of negotiating
        let url = this.baseUrl;
        this._accessTokenFactory = this._options.accessTokenFactory;
        this._httpClient._accessTokenFactory = this._accessTokenFactory;

        try {
            if (this._options.skipNegotiation) {
                if (this._options.transport === HttpTransportType.WebSockets) {
                    // No need to add a connection ID in this case
                    this.transport = this._constructTransport(HttpTransportType.WebSockets);
                    // We should just call connect directly in this case.
                    // No fallback or negotiate in this case.
                    await this._startTransport(url, transferFormat);
                } else {
                    throw new Error("Negotiation can only be skipped when using the WebSocket transport directly.");
                }
            } else {
                let negotiateResponse: INegotiateResponse | null = null;
                let redirects = 0;

                do {
                    negotiateResponse = await this._getNegotiationResponse(url);
                    // the user tries to stop the connection when it is being started
                    if (this._connectionState === ConnectionState.Disconnecting || this._connectionState === ConnectionState.Disconnected) {
                        throw new AbortError("The connection was stopped during negotiation.");
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
                        this._accessTokenFactory = () => accessToken;
                        // set the factory to undefined so the AccessTokenHttpClient won't retry with the same token, since we know it won't change until a connection restart
                        this._httpClient._accessToken = accessToken;
                        this._httpClient._accessTokenFactory = undefined;
                    }

                    redirects++;
                }
                while (negotiateResponse.url && redirects < MAX_REDIRECTS);

                if (redirects === MAX_REDIRECTS && negotiateResponse.url) {
                    throw new Error("Negotiate redirection limit exceeded.");
                }

                await this._createTransport(url, this._options.transport, negotiateResponse, transferFormat);
            }

            if (this.transport instanceof LongPollingTransport) {
                this.features.inherentKeepAlive = true;
            }

            if (this._connectionState === ConnectionState.Connecting) {
                // Ensure the connection transitions to the connected state prior to completing this.startInternalPromise.
                // start() will handle the case when stop was called and startInternal exits still in the disconnecting state.
                this._logger.log(LogLevel.Debug, "The HttpConnection connected successfully.");
                this._connectionState = ConnectionState.Connected;
            }

            // stop() is waiting on us via this.startInternalPromise so keep this.transport around so it can clean up.
            // This is the only case startInternal can exit in neither the connected nor disconnected state because stopConnection()
            // will transition to the disconnected state. start() will wait for the transition using the stopPromise.
        } catch (e) {
            this._logger.log(LogLevel.Error, "Failed to start the connection: " + e);
            this._connectionState = ConnectionState.Disconnected;
            this.transport = undefined;

            // if start fails, any active calls to stop assume that start will complete the stop promise
            this._stopPromiseResolver();
            return Promise.reject(e);
        }
    }

    private async _getNegotiationResponse(url: string): Promise<INegotiateResponse> {
        const headers: {[k: string]: string} = {};
        const [name, value] = getUserAgentHeader();
        headers[name] = value;

        const negotiateUrl = this._resolveNegotiateUrl(url);
        this._logger.log(LogLevel.Debug, `Sending negotiation request: ${negotiateUrl}.`);
        try {
            const response = await this._httpClient.post(negotiateUrl, {
                content: "",
                headers: { ...headers, ...this._options.headers },
                timeout: this._options.timeout,
                withCredentials: this._options.withCredentials,
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
            let errorMessage = "Failed to complete negotiation with the server: " + e;
            if (e instanceof HttpError) {
                if (e.statusCode === 404) {
                    errorMessage = errorMessage + " Either this is not a SignalR endpoint or there is a proxy blocking the connection.";
                }
            }
            this._logger.log(LogLevel.Error, errorMessage);

            return Promise.reject(new FailedToNegotiateWithServerError(errorMessage));
        }
    }

    private _createConnectUrl(url: string, connectionToken: string | null | undefined) {
        if (!connectionToken) {
            return url;
        }

        return url + (url.indexOf("?") === -1 ? "?" : "&") + `id=${connectionToken}`;
    }

    private async _createTransport(url: string, requestedTransport: HttpTransportType | ITransport | undefined, negotiateResponse: INegotiateResponse, requestedTransferFormat: TransferFormat): Promise<void> {
        let connectUrl = this._createConnectUrl(url, negotiateResponse.connectionToken);
        if (this._isITransport(requestedTransport)) {
            this._logger.log(LogLevel.Debug, "Connection was provided an instance of ITransport, using that directly.");
            this.transport = requestedTransport;
            await this._startTransport(connectUrl, requestedTransferFormat);

            this.connectionId = negotiateResponse.connectionId;
            return;
        }

        const transportExceptions: any[] = [];
        const transports = negotiateResponse.availableTransports || [];
        let negotiate: INegotiateResponse | undefined = negotiateResponse;
        for (const endpoint of transports) {
            const transportOrError = this._resolveTransportOrError(endpoint, requestedTransport, requestedTransferFormat);
            if (transportOrError instanceof Error) {
                // Store the error and continue, we don't want to cause a re-negotiate in these cases
                transportExceptions.push(`${endpoint.transport} failed:`);
                transportExceptions.push(transportOrError);
            } else if (this._isITransport(transportOrError)) {
                this.transport = transportOrError;
                if (!negotiate) {
                    try {
                        negotiate = await this._getNegotiationResponse(url);
                    } catch (ex) {
                        return Promise.reject(ex);
                    }
                    connectUrl = this._createConnectUrl(url, negotiate.connectionToken);
                }
                try {
                    await this._startTransport(connectUrl, requestedTransferFormat);
                    this.connectionId = negotiate.connectionId;
                    return;
                } catch (ex) {
                    this._logger.log(LogLevel.Error, `Failed to start the transport '${endpoint.transport}': ${ex}`);
                    negotiate = undefined;
                    transportExceptions.push(new FailedToStartTransportError(`${endpoint.transport} failed: ${ex}`, HttpTransportType[endpoint.transport]));

                    if (this._connectionState !== ConnectionState.Connecting) {
                        const message = "Failed to select transport before stop() was called.";
                        this._logger.log(LogLevel.Debug, message);
                        return Promise.reject(new AbortError(message));
                    }
                }
            }
        }

        if (transportExceptions.length > 0) {
            return Promise.reject(new AggregateErrors(`Unable to connect to the server with any of the available transports. ${transportExceptions.join(" ")}`, transportExceptions));
        }
        return Promise.reject(new Error("None of the transports supported by the client are supported by the server."));
    }

    private _constructTransport(transport: HttpTransportType): ITransport {
        switch (transport) {
            case HttpTransportType.WebSockets:
                if (!this._options.WebSocket) {
                    throw new Error("'WebSocket' is not supported in your environment.");
                }
                return new WebSocketTransport(this._httpClient, this._accessTokenFactory, this._logger, this._options.logMessageContent!, this._options.WebSocket, this._options.headers || {});
            case HttpTransportType.ServerSentEvents:
                if (!this._options.EventSource) {
                    throw new Error("'EventSource' is not supported in your environment.");
                }
                return new ServerSentEventsTransport(this._httpClient, this._httpClient._accessToken, this._logger, this._options);
            case HttpTransportType.LongPolling:
                return new LongPollingTransport(this._httpClient, this._logger, this._options);
            default:
                throw new Error(`Unknown transport: ${transport}.`);
        }
    }

    private _startTransport(url: string, transferFormat: TransferFormat): Promise<void> {
        this.transport!.onreceive = this.onreceive;
        this.transport!.onclose = (e) => this._stopConnection(e);
        return this.transport!.connect(url, transferFormat);
    }

    private _resolveTransportOrError(endpoint: IAvailableTransport, requestedTransport: HttpTransportType | undefined, requestedTransferFormat: TransferFormat): ITransport | Error {
        const transport = HttpTransportType[endpoint.transport];
        if (transport === null || transport === undefined) {
            this._logger.log(LogLevel.Debug, `Skipping transport '${endpoint.transport}' because it is not supported by this client.`);
            return new Error(`Skipping transport '${endpoint.transport}' because it is not supported by this client.`);
        } else {
            if (transportMatches(requestedTransport, transport)) {
                const transferFormats = endpoint.transferFormats.map((s) => TransferFormat[s]);
                if (transferFormats.indexOf(requestedTransferFormat) >= 0) {
                    if ((transport === HttpTransportType.WebSockets && !this._options.WebSocket) ||
                        (transport === HttpTransportType.ServerSentEvents && !this._options.EventSource)) {
                        this._logger.log(LogLevel.Debug, `Skipping transport '${HttpTransportType[transport]}' because it is not supported in your environment.'`);
                        return new UnsupportedTransportError(`'${HttpTransportType[transport]}' is not supported in your environment.`, transport);
                    } else {
                        this._logger.log(LogLevel.Debug, `Selecting transport '${HttpTransportType[transport]}'.`);
                        try {
                            return this._constructTransport(transport);
                        } catch (ex) {
                            return ex;
                        }
                    }
                } else {
                    this._logger.log(LogLevel.Debug, `Skipping transport '${HttpTransportType[transport]}' because it does not support the requested transfer format '${TransferFormat[requestedTransferFormat]}'.`);
                    return new Error(`'${HttpTransportType[transport]}' does not support ${TransferFormat[requestedTransferFormat]}.`);
                }
            } else {
                this._logger.log(LogLevel.Debug, `Skipping transport '${HttpTransportType[transport]}' because it was disabled by the client.`);
                return new DisabledTransportError(`'${HttpTransportType[transport]}' is disabled by the client.`, transport);
            }
        }
    }

    private _isITransport(transport: any): transport is ITransport {
        return transport && typeof (transport) === "object" && "connect" in transport;
    }

    private _stopConnection(error?: Error): void {
        this._logger.log(LogLevel.Debug, `HttpConnection.stopConnection(${error}) called while in state ${this._connectionState}.`);

        this.transport = undefined;

        // If we have a stopError, it takes precedence over the error from the transport
        error = this._stopError || error;
        this._stopError = undefined;

        if (this._connectionState === ConnectionState.Disconnected) {
            this._logger.log(LogLevel.Debug, `Call to HttpConnection.stopConnection(${error}) was ignored because the connection is already in the disconnected state.`);
            return;
        }

        if (this._connectionState === ConnectionState.Connecting) {
            this._logger.log(LogLevel.Warning, `Call to HttpConnection.stopConnection(${error}) was ignored because the connection is still in the connecting state.`);
            throw new Error(`HttpConnection.stopConnection(${error}) was called while the connection is still in the connecting state.`);
        }

        if (this._connectionState === ConnectionState.Disconnecting) {
            // A call to stop() induced this call to stopConnection and needs to be completed.
            // Any stop() awaiters will be scheduled to continue after the onclose callback fires.
            this._stopPromiseResolver();
        }

        if (error) {
            this._logger.log(LogLevel.Error, `Connection disconnected with error '${error}'.`);
        } else {
            this._logger.log(LogLevel.Information, "Connection disconnected.");
        }

        if (this._sendQueue) {
            this._sendQueue.stop().catch((e) => {
                this._logger.log(LogLevel.Error, `TransportSendQueue.stop() threw error '${e}'.`);
            });
            this._sendQueue = undefined;
        }

        this.connectionId = undefined;
        this._connectionState = ConnectionState.Disconnected;

        if (this._connectionStarted) {
            this._connectionStarted = false;
            try {
                if (this.onclose) {
                    this.onclose(error);
                }
            } catch (e) {
                this._logger.log(LogLevel.Error, `HttpConnection.onclose(${error}) threw error '${e}'.`);
            }
        }
    }

    private _resolveUrl(url: string): string {
        // startsWith is not supported in IE
        if (url.lastIndexOf("https://", 0) === 0 || url.lastIndexOf("http://", 0) === 0) {
            return url;
        }

        if (!Platform.isBrowser) {
            throw new Error(`Cannot resolve '${url}'.`);
        }

        // Setting the url to the href propery of an anchor tag handles normalization
        // for us. There are 3 main cases.
        // 1. Relative path normalization e.g "b" -> "http://localhost:5000/a/b"
        // 2. Absolute path normalization e.g "/a/b" -> "http://localhost:5000/a/b"
        // 3. Networkpath reference normalization e.g "//localhost:5000/a/b" -> "http://localhost:5000/a/b"
        const aTag = window.document.createElement("a");
        aTag.href = url;

        this._logger.log(LogLevel.Information, `Normalizing '${url}' to '${aTag.href}'.`);
        return aTag.href;
    }

    private _resolveNegotiateUrl(url: string): string {
        const index = url.indexOf("?");
        let negotiateUrl = url.substring(0, index === -1 ? url.length : index);
        if (negotiateUrl[negotiateUrl.length - 1] !== "/") {
            negotiateUrl += "/";
        }
        negotiateUrl += "negotiate";
        negotiateUrl += index === -1 ? "" : url.substring(index);

        if (negotiateUrl.indexOf("negotiateVersion") === -1) {
            negotiateUrl += index === -1 ? "?" : "&";
            negotiateUrl += "negotiateVersion=" + this._negotiateVersion;
        }
        return negotiateUrl;
    }
}

function transportMatches(requestedTransport: HttpTransportType | undefined, actualTransport: HttpTransportType) {
    return !requestedTransport || ((actualTransport & requestedTransport) !== 0);
}

/** @private */
export class TransportSendQueue {
    private _buffer: any[] = [];
    private _sendBufferedData: PromiseSource;
    private _executing: boolean = true;
    private _transportResult?: PromiseSource;
    private _sendLoopPromise: Promise<void>;

    constructor(private readonly _transport: ITransport) {
        this._sendBufferedData = new PromiseSource();
        this._transportResult = new PromiseSource();

        this._sendLoopPromise = this._sendLoop();
    }

    public send(data: string | ArrayBuffer): Promise<void> {
        this._bufferData(data);
        if (!this._transportResult) {
            this._transportResult = new PromiseSource();
        }
        return this._transportResult.promise;
    }

    public stop(): Promise<void> {
        this._executing = false;
        this._sendBufferedData.resolve();
        return this._sendLoopPromise;
    }

    private _bufferData(data: string | ArrayBuffer): void {
        if (this._buffer.length && typeof(this._buffer[0]) !== typeof(data)) {
            throw new Error(`Expected data to be of type ${typeof(this._buffer)} but was of type ${typeof(data)}`);
        }

        this._buffer.push(data);
        this._sendBufferedData.resolve();
    }

    private async _sendLoop(): Promise<void> {
        while (true) {
            await this._sendBufferedData.promise;

            if (!this._executing) {
                if (this._transportResult) {
                    this._transportResult.reject("Connection stopped.");
                }

                break;
            }

            this._sendBufferedData = new PromiseSource();

            const transportResult = this._transportResult!;
            this._transportResult = undefined;

            const data = typeof(this._buffer[0]) === "string" ?
                this._buffer.join("") :
                TransportSendQueue._concatBuffers(this._buffer);

            this._buffer.length = 0;

            try {
                await this._transport.send(data);
                transportResult.resolve();
            } catch (error) {
                transportResult.reject(error);
            }
        }
    }

    private static _concatBuffers(arrayBuffers: ArrayBuffer[]): ArrayBuffer {
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
    private _resolver?: () => void;
    private _rejecter!: (reason?: any) => void;
    public promise: Promise<void>;

    constructor() {
        this.promise = new Promise((resolve, reject) => [this._resolver, this._rejecter] = [resolve, reject]);
    }

    public resolve(): void {
        this._resolver!();
    }

    public reject(reason?: any): void {
        this._rejecter!(reason);
    }
}
