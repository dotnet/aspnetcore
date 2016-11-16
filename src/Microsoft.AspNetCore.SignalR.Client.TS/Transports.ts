import { HttpClient } from "./HttpClient"

export interface ITransport {
    connect(url: string, queryString: string): Promise<void>;
    send(data: any): Promise<void>;
    stop(): void;
    onDataReceived: DataReceived;
    onError: ErrorHandler;
}

export class WebSocketTransport implements ITransport {
    private webSocket: WebSocket;

    connect(url: string, queryString: string = ""): Promise<void> {
        return new Promise((resolve, reject) => {
            url = url.replace(/^http/, "ws");
            let connectUrl = url + "/ws?" + queryString;

            let webSocket = new WebSocket(connectUrl);
            let thisWebSocketTransport = this;

            webSocket.onopen = (event: Event) => {
                console.log(`WebSocket connected to ${connectUrl}`);
                thisWebSocketTransport.webSocket = webSocket;
                resolve();
            };

            webSocket.onerror = (event: Event) => {
                reject();
            };

            webSocket.onmessage = (message: MessageEvent) => {
                console.log(`(WebSockets transport) data received: ${message.data}`);
                if (thisWebSocketTransport.onDataReceived) {
                    thisWebSocketTransport.onDataReceived(message.data);
                }
            }

            webSocket.onclose = (event: CloseEvent) => {
                // webSocket will be null if the transport did not start successfully
                if (thisWebSocketTransport.webSocket && event.wasClean === false) {
                    if (thisWebSocketTransport.onError) {
                        thisWebSocketTransport.onError(event);
                    }
                }
            }
        });
    }

    send(data: any): Promise<void> {
        if (this.webSocket && this.webSocket.readyState === WebSocket.OPEN) {
            this.webSocket.send(data);
            return Promise.resolve();
        }

        return Promise.reject("WebSocket is not in OPEN state");
    }

    stop(): void {
        if (this.webSocket) {
            this.webSocket.close();
            this.webSocket = null;
        }
    }

    onDataReceived: DataReceived;
    onError: ErrorHandler;
}

export class ServerSentEventsTransport implements ITransport {
    private eventSource: EventSource;
    private url: string;
    private queryString: string;

    connect(url: string, queryString: string): Promise<void> {
        if (typeof (EventSource) === "undefined") {
            Promise.reject("EventSource not supported by the browser.")
        }

        this.queryString = queryString;
        this.url = url;
        let tmp = `${this.url}/sse?${this.queryString}`;

        return new Promise((resolve, reject) => {
            let eventSource = new EventSource(`${this.url}/sse?${this.queryString}`);

            try {
                let thisEventSourceTransport = this;
                eventSource.onmessage = (e: MessageEvent) => {
                    if (thisEventSourceTransport.onDataReceived) {
                        thisEventSourceTransport.onDataReceived(e.data);
                    }
                };

                eventSource.onerror = (e: Event) => {
                    reject();

                    // don't report an error if the transport did not start successfully
                    if (thisEventSourceTransport.eventSource && thisEventSourceTransport.onError) {
                        thisEventSourceTransport.onError(e);
                    }
                }

                eventSource.onopen = () => {
                    thisEventSourceTransport.eventSource = eventSource;
                    resolve();
                }
            }
            catch (e) {
                return Promise.reject(e);
            }
        });
    }

    send(data: any): Promise<void> {
        return new HttpClient().post(this.url + "/send?" + this.queryString, data);
    }

    stop(): void {
        if (this.eventSource) {
            this.eventSource.close();
            this.eventSource = null;
        }
    }

    onDataReceived: DataReceived;
    onError: ErrorHandler;
}

export class LongPollingTransport implements ITransport {
    private url: string;
    private queryString: string;
    private pollXhr: XMLHttpRequest;

    connect(url: string, queryString: string): Promise<void> {
        this.url = url;
        this.queryString = queryString;
        this.poll(url + "/poll?" + this.queryString)
        return Promise.resolve();
    }

    private poll(url: string): void {
        let thisLongPollingTransport = this;
        let pollXhr = new XMLHttpRequest();

        pollXhr.onload = () => {
            if (pollXhr.status == 200) {
                if (thisLongPollingTransport.onDataReceived) {
                    thisLongPollingTransport.onDataReceived(pollXhr.response);
                }
                thisLongPollingTransport.poll(url);
            }
            else if (this.pollXhr.status == 204) {
                // TODO: closed event?
            }
            else {
                if (thisLongPollingTransport.onError) {
                    thisLongPollingTransport.onError({
                        status: pollXhr.status,
                        statusText: pollXhr.statusText
                    });
                }
            }
        };

        pollXhr.onerror = () => {
            if (thisLongPollingTransport.onError) {
                thisLongPollingTransport.onError({
                    status: pollXhr.status,
                    statusText: pollXhr.statusText
                });
            }
        };

        pollXhr.ontimeout = () => {
            thisLongPollingTransport.poll(url);
        }

        this.pollXhr = pollXhr;
        this.pollXhr.open("GET", url, true);
        // TODO: consider making timeout configurable
        this.pollXhr.timeout = 110000;
        this.pollXhr.send();
    }

    send(data: any): Promise<void> {
        return new HttpClient().post(this.url + "/send?" + this.queryString, data);
    }

    stop(): void {
        if (this.pollXhr) {
            this.pollXhr.abort();
            this.pollXhr = null;
        }
    }

    onDataReceived: DataReceived;
    onError: ErrorHandler;
}
