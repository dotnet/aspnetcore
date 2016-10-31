
enum ConnectionState {
    Disconnected,
    Connecting,
    Connected
}

class Connection {
    private connectionState: ConnectionState;
    private url: string;
    private queryString: string;
    private connectionId: string;
    private transport: ITransport;
    private dataReceivedCallback: DataReceived;
    private errorHandler: ErrorHandler;

    constructor(url: string, queryString: string = "") {
        this.url = url;
        this.queryString = queryString;

        this.connectionState = ConnectionState.Disconnected;
    }

    start(): Promise<void> {
        if (this.connectionState != ConnectionState.Disconnected) {
            throw new Error("Cannot start a connection that is not in the 'Disconnected' state");
        }

        return new HttpClient().get(`${this.url}/getid?${this.queryString}`)
            .then(connectionId => {
                this.connectionId = connectionId;
                return this.tryStartTransport([
                    new WebSocketTransport(),
                    new ServerSentEventsTransport(null),
                    new LongPollingTransport(null)], 0);
            })
            .then(transport => {
                this.transport = transport;
                this.connectionState = ConnectionState.Connected;
            })
            .catch(e => {
                console.log("Failed to start the connection.")
                this.connectionState = ConnectionState.Disconnected;
                throw e;
            });
    }

    private tryStartTransport(transports: ITransport[], index: number): Promise<ITransport> {
        let thisConnection = this;
        transports[index].onDataReceived = data => thisConnection.dataReceivedCallback(data);
        transports[index].onError = e => thisConnection.errorHandler(e);

        return transports[index].connect(this.url, this.queryString)
            .then(() => {
                return transports[index];
            })
            .catch(e => {
                index++;
                if (index < transports.length) {
                    return this.tryStartTransport(transports, index);
                }
                else
                {
                    throw new Error('No transport could be started.')
                }
            })
    }

    send(data: any): Promise<void> {
        if (this.connectionState != ConnectionState.Connected) {
            throw new Error("Cannot send data if the connection is not in the 'Connected' State");
        }
        return this.transport.send(data);
    }

    set dataReceived(callback: DataReceived) {
        this.dataReceivedCallback = callback;
    }

    set onError(callback: ErrorHandler) {
        this.errorHandler = callback;
    }

    stop(): void {
        if (this.connectionState != ConnectionState.Connected) {
            throw new Error("Cannot stop the connection if is not in the 'Connected' State");
        }

        this.transport.stop();
        this.connectionState = ConnectionState.Disconnected;
    }
}