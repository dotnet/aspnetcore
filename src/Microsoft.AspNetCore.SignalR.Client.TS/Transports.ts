import { DataReceived, ErrorHandler } from "./Common"
import { IHttpClient } from "./HttpClient"

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
        return new Promise<void>((resolve, reject) => {
            url = url.replace(/^http/, "ws");
            let connectUrl = url + "/ws?" + queryString;

            let webSocket = new WebSocket(connectUrl);

            webSocket.onopen = (event: Event) => {
                console.log(`WebSocket connected to ${connectUrl}`);
                this.webSocket = webSocket;
                resolve();
            };

            webSocket.onerror = (event: Event) => {
                reject();
            };

            webSocket.onmessage = (message: MessageEvent) => {
                console.log(`(WebSockets transport) data received: ${message.data}`);
                if (this.onDataReceived) {
                    this.onDataReceived(message.data);
                }
            }

            webSocket.onclose = (event: CloseEvent) => {
                // webSocket will be null if the transport did not start successfully
                if (this.webSocket && (event.wasClean === false || event.code !== 1000)) {
                    if (this.onError) {
                        this.onError(event);
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

        return Promise.reject("WebSocket is not in the OPEN state");
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
    private httpClient: IHttpClient;

    constructor(httpClient :IHttpClient) {
        this.httpClient = httpClient;
    }

    connect(url: string, queryString: string): Promise<void> {
        if (typeof (EventSource) === "undefined") {
            Promise.reject("EventSource not supported by the browser.")
        }

        this.queryString = queryString;
        this.url = url;
        let tmp = `${this.url}/sse?${this.queryString}`;

        return new Promise<void>((resolve, reject) => {
            let eventSource = new EventSource(`${this.url}/sse?${this.queryString}`);

            try {
                eventSource.onmessage = (e: MessageEvent) => {
                    if (this.onDataReceived) {
                        this.onDataReceived(e.data);
                    }
                };

                eventSource.onerror = (e: Event) => {
                    reject();

                    // don't report an error if the transport did not start successfully
                    if (this.eventSource && this.onError) {
                        this.onError(e);
                    }
                }

                eventSource.onopen = () => {
                    this.eventSource = eventSource;
                    resolve();
                }
            }
            catch (e) {
                return Promise.reject(e);
            }
        });
    }

    async send(data: any): Promise<void> {
        await this.httpClient.post(this.url + "/send?" + this.queryString, data);
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
    private httpClient: IHttpClient;
    private pollXhr: XMLHttpRequest;
    private shouldPoll: boolean;

    constructor(httpClient :IHttpClient) {
        this.httpClient = httpClient;
    }

    connect(url: string, queryString: string): Promise<void> {
        this.url = url;
        this.queryString = queryString;
        this.shouldPoll = true;
        this.poll(url + "/poll?" + this.queryString)
        return Promise.resolve();
    }

    private poll(url: string): void {
        if (!this.shouldPoll) {
            return;
        }

        let pollXhr = new XMLHttpRequest();

        pollXhr.onload = () => {
            if (pollXhr.status == 200) {
                if (this.onDataReceived) {
                    this.onDataReceived(pollXhr.response);
                }
                this.poll(url);
            }
            else if (this.pollXhr.status == 204) {
                // TODO: closed event?
            }
            else {
                if (this.onError) {
                    this.onError({
                        status: pollXhr.status,
                        statusText: pollXhr.statusText
                    });
                }
            }
        };

        pollXhr.onerror = () => {
            if (this.onError) {
                this.onError({
                    status: pollXhr.status,
                    statusText: pollXhr.statusText
                });
            }
        };

        pollXhr.ontimeout = () => {
            this.poll(url);
        }

        this.pollXhr = pollXhr;
        this.pollXhr.open("GET", url, true);
        // TODO: consider making timeout configurable
        this.pollXhr.timeout = 110000;
        this.pollXhr.send();
    }

    async send(data: any): Promise<void> {
        await this.httpClient.post(this.url + "/send?" + this.queryString, data);
    }

    stop(): void {
        this.shouldPoll = false;
        if (this.pollXhr) {
            this.pollXhr.abort();
            this.pollXhr = null;
        }
    }

    onDataReceived: DataReceived;
    onError: ErrorHandler;
}
