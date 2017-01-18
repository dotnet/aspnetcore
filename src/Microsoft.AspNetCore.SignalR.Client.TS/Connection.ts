import { DataReceived, ConnectionClosed } from "./Common"
import { ITransport, WebSocketTransport, ServerSentEventsTransport, LongPollingTransport } from "./Transports"
import { IHttpClient, HttpClient } from "./HttpClient"
import { ISignalROptions } from "./ISignalROptions"

enum ConnectionState {
    Disconnected,
    Connecting,
    Connected
}

export class Connection {
    private connectionState: ConnectionState;
    private url: string;
    private queryString: string;
    private connectionId: string;
    private httpClient: IHttpClient;
    private transport: ITransport;
    private dataReceivedCallback: DataReceived = (data: any) => { };
    private connectionClosedCallback: ConnectionClosed = (error?: any) => { };

    constructor(url: string, queryString: string = "", options: ISignalROptions = {}) {
        this.url = url;
        this.queryString = queryString;
        this.httpClient = options.httpClient || new HttpClient();
        this.connectionState = ConnectionState.Disconnected;
    }

    async start(transportName: string = 'webSockets'): Promise<void> {
        if (this.connectionState != ConnectionState.Disconnected) {
            throw new Error("Cannot start a connection that is not in the 'Disconnected' state");
        }

        this.transport = this.createTransport(transportName);
        this.transport.onDataReceived = this.dataReceivedCallback;
        this.transport.onError = e => this.stopConnection(e);

        try {
            this.connectionId = await this.httpClient.get(`${this.url}/getid?${this.queryString}`);
            this.queryString = `id=${this.connectionId}`;
            await this.transport.connect(this.url, this.queryString);
            this.connectionState = ConnectionState.Connected;
        }
        catch(e) {
            console.log("Failed to start the connection.")
            this.connectionState = ConnectionState.Disconnected;
            this.transport = null;
            throw e;
        };
    }

    private createTransport(transportName: string): ITransport {
        if (transportName === 'webSockets') {
            return new WebSocketTransport();
        }
        if (transportName === 'serverSentEvents') {
            return new ServerSentEventsTransport(this.httpClient);
        }
        if (transportName === 'longPolling') {
            return new LongPollingTransport(this.httpClient);
        }

        throw new Error("No valid transports requested.");
    }

    send(data: any): Promise<void> {
        if (this.connectionState != ConnectionState.Connected) {
            throw new Error("Cannot send data if the connection is not in the 'Connected' State");
        }
        return this.transport.send(data);
    }

    stop(): void {
        if (this.connectionState != ConnectionState.Connected) {
            throw new Error("Cannot stop the connection if it is not in the 'Connected' State");
        }

        this.stopConnection();
    }

    private stopConnection(error?: any) {
        this.transport.stop();
        this.transport = null;
        this.connectionState = ConnectionState.Disconnected;
        this.connectionClosedCallback(error);
    }

    set dataReceived(callback: DataReceived) {
        this.dataReceivedCallback = callback;
    }

    set connectionClosed(callback: ConnectionClosed) {
        this.connectionClosedCallback = callback;
    }
}