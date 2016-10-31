class WebSocketTransport implements ITransport {
    private webSocket: WebSocket;

    connect(url: string, queryString: string): Promise<void> {
        return new Promise((resolve, reject) => {
            url = url.replace(/^http/, "ws");
            let connectUrl = url + "/ws?" + queryString;
            this.webSocket = new WebSocket(connectUrl);
            this.webSocket.onopen = (event: Event) => {
                console.log(`WebSocket connected to ${connectUrl}`);
                resolve();
            };

            this.webSocket.onerror = (event: Event) => {
                // TODO: handle when connection was opened successfully
                reject();
            };

            this.webSocket.onmessage = (message: MessageEvent) => {
                console.log(`(WebSockets transport) data received: ${message.data}`);
                if (this.onDataReceived) {
                    this.onDataReceived(message.data);
                }
            }
        });
    }

    send(data: any): Promise<void> {
        this.webSocket.send(data);
        return Promise.resolve();
    }

    stop(): void {
        this.webSocket.close();
    }

    onDataReceived: DataReceived;
    onError: ErrorHandler;
}