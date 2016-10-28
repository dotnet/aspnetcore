class LongPollingTransport implements ITransport {

    private receiveCallback: (data: string) => void;
    private url: string;
    private queryString: string;
    private pollXhr: XMLHttpRequest;

    // TODO: make the callback a named type
    // TODO: string won't work for binary formats
    constructor(receiveCallback: (data: string) => void) {
         this.receiveCallback = receiveCallback;
    }

    connect(url: string, queryString: string): Promise<void> {
        this.queryString = queryString || "";
        this.url = url || "";

        this.pollXhr = new XMLHttpRequest();
        // TODO: resolve promise on open sending? + reject on error
        this.poll(url + "/poll?" + this.queryString)
        return Promise.resolve();
    }

    private poll(url: string): void {
        //TODO: timeout
        this.pollXhr.open("GET", url, true);
        this.pollXhr.send();
        this.pollXhr.onload = () => {
            if (this.pollXhr.status >= 200 && this.pollXhr.status < 300) {
                this.receiveCallback(this.pollXhr.response);
                this.poll(url);
            }
            else {
                //TODO: handle error
                /*
                    {
                        status: xhr.status,
                        statusText: xhr.statusText
                    };
                }*/
            };

            this.pollXhr.onerror = () => {
                /*
                reject({
                    status: xhr.status,
                    statusText: xhr.statusText
                });*/
                //TODO: handle error
            };
        };
    }

    send(data: string): Promise<void> {
        return new HttpClient().post(this.url + "/send?" + this.queryString, data);
    }

    stop(): void {
        this.pollXhr.abort();
    }
}