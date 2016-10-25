class WebSocketTransport implements ITransport {
    private webSocket: WebSocket;
    // TODO: make the callback a named type
    // TODO: string won't work for binary formats
    private receiveCallback: (data: string) => void;

    constructor(receiveCallback: (data: string) => void) {
         this.receiveCallback = receiveCallback;
    }

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
                this.receiveCallback(message.data);
            }
        });
    }

    send(data: string): Promise<void> {
        this.webSocket.send(data);
        return Promise.resolve();
    }

    stop(): void {
        this.webSocket.close();
    }
}