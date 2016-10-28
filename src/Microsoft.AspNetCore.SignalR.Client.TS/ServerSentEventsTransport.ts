// TODO: need EvenSource typings

class ServerSentEventsTransport implements ITransport {
    private receiveCallback: (data: string) => void;
    private eventSource: EventSource;
    private url: string;
    private queryString: string;

    constructor(receiveCallback: (data: string) => void) {
         this.receiveCallback = receiveCallback;
    }

    connect(url: string, queryString: string): Promise<void> {
        this.queryString = queryString || "";
        this.url = url || "";
        let tmp = `${this.url}/sse?${this.queryString}`;
        this.eventSource = new EventSource(`${this.url}/sse?${this.queryString}`);
        this.eventSource.onmessage = e => {
            this.receiveCallback(e.data);
        };

        //TODO: handle errors
        return Promise.resolve();
    }

    send(data: string): Promise<void> {
        return new HttpClient().post(this.url + "/send?" + this.queryString, data);
    }

    stop(): void {
        this.eventSource.close();
    }
}