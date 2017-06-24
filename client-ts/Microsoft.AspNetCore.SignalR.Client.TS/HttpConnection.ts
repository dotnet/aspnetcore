import { DataReceived, ConnectionClosed } from "./Common"
import { IConnection } from "./IConnection"
import { ITransport, TransportType, WebSocketTransport, ServerSentEventsTransport, LongPollingTransport } from "./Transports"
import { IHttpClient, HttpClient } from "./HttpClient"
import { IHttpConnectionOptions } from "./IHttpConnectionOptions"

enum ConnectionState {
    Initial,
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
    private url: string;
    private connectionId: string;
    private httpClient: IHttpClient;
    private transport: ITransport;
    private options: IHttpConnectionOptions;
    private startPromise: Promise<void>;

    constructor(url: string, options: IHttpConnectionOptions = {}) {
        this.url = url;
        this.httpClient = options.httpClient || new HttpClient();
        this.connectionState = ConnectionState.Initial;
        this.options = options;
    }

    async start(): Promise<void> {
        if (this.connectionState != ConnectionState.Initial) {
            return Promise.reject(new Error("Cannot start a connection that is not in the 'Initial' state."));
        }

        this.connectionState = ConnectionState.Connecting;

        this.startPromise = this.startInternal();
        return this.startPromise;
    }

    private async startInternal(): Promise<void> {
        try {
            let negotiatePayload = await this.httpClient.options(this.url);
            let negotiateResponse: INegotiateResponse = JSON.parse(negotiatePayload);
            this.connectionId = negotiateResponse.connectionId;

            // the user tries to stop the the connection when it is being started
            if (this.connectionState == ConnectionState.Disconnected) {
                return;
            }

            this.url += (this.url.indexOf("?") == -1 ? "?" : "&") + `id=${this.connectionId}`;

            this.transport = this.createTransport(this.options.transport, negotiateResponse.availableTransports);
            this.transport.onDataReceived = this.onDataReceived;
            this.transport.onClosed = e => this.stopConnection(true, e);
            await this.transport.connect(this.url);
            // only change the state if we were connecting to not overwrite
            // the state if the connection is already marked as Disconnected
            this.changeState(ConnectionState.Connecting, ConnectionState.Connected);
        }
        catch (e) {
            console.log("Failed to start the connection. " + e);
            this.connectionState = ConnectionState.Disconnected;
            this.transport = null;
            throw e;
        };
    }

    private createTransport(transport: TransportType | ITransport, availableTransports: string[]): ITransport {
        if (!transport && availableTransports.length > 0) {
            transport = TransportType[availableTransports[0]];
        }
        if (transport === TransportType.WebSockets && availableTransports.indexOf(TransportType[transport]) >= 0) {
            return new WebSocketTransport();
        }
        if (transport === TransportType.ServerSentEvents && availableTransports.indexOf(TransportType[transport]) >= 0) {
            return new ServerSentEventsTransport(this.httpClient);
        }
        if (transport === TransportType.LongPolling && availableTransports.indexOf(TransportType[transport]) >= 0) {
            return new LongPollingTransport(this.httpClient);
        }

        if (this.isITransport(transport)) {
            return transport;
        }

        throw new Error("No available transports found.");
    }

    private isITransport(transport: any): transport is ITransport {
        return typeof(transport) === "object" && "connect" in transport;
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

    async stop(): Promise<void> {
        let previousState = this.connectionState;
        this.connectionState = ConnectionState.Disconnected;

        try {
            await this.startPromise;
        }
        catch (e) {
            // this exception is returned to the user as a rejected Promise from the start method
        }
        this.stopConnection(/*raiseClosed*/ previousState == ConnectionState.Connected);
    }

    private stopConnection(raiseClosed: Boolean, error?: any) {
        if (this.transport) {
            this.transport.stop();
            this.transport = null;
        }

        this.connectionState = ConnectionState.Disconnected;

        if (raiseClosed && this.onClosed) {
            this.onClosed(error);
        }
    }

    onDataReceived: DataReceived;
    onClosed: ConnectionClosed;
}