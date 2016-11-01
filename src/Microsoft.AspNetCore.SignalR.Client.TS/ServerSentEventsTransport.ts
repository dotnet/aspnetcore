// TODO: need EvenSource typings

class ServerSentEventsTransport implements ITransport {
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
        try {
            this.eventSource = new EventSource(`${this.url}/sse?${this.queryString}`);

            this.eventSource.onmessage = (e: MessageEvent) => {
                this.onDataReceived(e.data);
            };
            this.eventSource.onerror = (e: Event) => {
                // todo: handle errors
            }
        }
        catch (e) {
            return Promise.reject(e);
        }

        return Promise.resolve();
    }

    send(data: any): Promise<void> {
        return new HttpClient().post(this.url + "/send?" + this.queryString, data);
    }

    stop(): void {
        this.eventSource.close();
    }

    onDataReceived: DataReceived;
    onError: ErrorHandler;
}