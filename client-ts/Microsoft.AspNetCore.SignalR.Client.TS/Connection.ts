import { DataReceived, ConnectionClosed } from "./Common"
import { IConnection } from "./IConnection"
import { ITransport, TransportType, WebSocketTransport, ServerSentEventsTransport, LongPollingTransport } from "./Transports"
import { IHttpClient, HttpClient } from "./HttpClient"
import { ISignalROptions } from "./ISignalROptions"

enum ConnectionState {
    Initial,
    Connecting,
    Connected,
    Disconnected
}

export class Connection implements IConnection {
    private connectionState: ConnectionState;
    private url: string;
    private queryString: string;
    private connectionId: string;
    private httpClient: IHttpClient;
    private transport: ITransport;
    private startPromise: Promise<void>;

    constructor(url: string, queryString: string = "", options: ISignalROptions = {}) {
        this.url = url;
        this.queryString = queryString;
        this.httpClient = options.httpClient || new HttpClient();
        this.connectionState = ConnectionState.Initial;
    }

    async start(transport: TransportType | ITransport = TransportType.WebSockets): Promise<void> {
        if (this.connectionState != ConnectionState.Initial) {
            return Promise.reject(new Error("Cannot start a connection that is not in the 'Initial' state."));
        }

        this.connectionState = ConnectionState.Connecting;

        this.startPromise = this.startInternal(transport);
        return this.startPromise;
    }

    private async startInternal(transportType: TransportType | ITransport): Promise<void> {
        try {
            this.connectionId = await this.httpClient.get(`${this.url}/negotiate?${this.queryString}`);

            // the user tries to stop the the connection when it is being started
            if (this.connectionState == ConnectionState.Disconnected) {
                return;
            }

            this.queryString = `id=${this.connectionId}`;

            this.transport = this.createTransport(transportType);
            this.transport.onDataReceived = this.onDataReceived;
            this.transport.onClosed = e => this.stopConnection(true, e);
            await this.transport.connect(this.url, this.queryString);
            // only change the state if we were connecting to not overwrite
            // the state if the connection is already marked as Disconnected
            this.changeState(ConnectionState.Connecting, ConnectionState.Connected);
        }
        catch (e) {
            console.log("Failed to start the connection. " + e)
            this.connectionState = ConnectionState.Disconnected;
            this.transport = null;
            throw e;
        };
    }

    private createTransport(transport: TransportType | ITransport): ITransport {
        if (transport === TransportType.WebSockets) {
            return new WebSocketTransport();
        }
        if (transport === TransportType.ServerSentEvents) {
            return new ServerSentEventsTransport(this.httpClient);
        }
        if (transport === TransportType.LongPolling) {
            return new LongPollingTransport(this.httpClient);
        }

        if (this.isITransport(transport)) {
            return transport;
        }

        throw new Error("No valid transports requested.");
    }

    private isITransport(transport: any): transport is ITransport {
        return "connect" in transport;
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