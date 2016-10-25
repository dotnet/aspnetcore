class LongPollingTransport implements ITransport {

    private receiveCallback: (data: string) => void;
    private url: string;
    private queryString: string;
    private pollXhr: XMLHttpRequest;

    // TODO: make the callback a named type
    // TODO: string won't work for binary formats
    constructor(receiveCallback: (data: string) => void) {
         this.receiveCallback = receiveCallback;
         this.pollXhr = new XMLHttpRequest();
    }

    connect(url: string, queryString: string): Promise<void> {

        return Promise.resolve();
    }

    private poll(): void {
        this.pollXhr.open("GET", , true);
        this.pollXhr.send();
        this.pollXhr.onload = () => {
            if (this.pollXhr.status >= 200 && this.pollXhr.status < 300) {
                this.receiveCallback(this.pollXhr.response);
                this.poll();
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
        return new HttpClient().post(this.url + "/poll/send?" + this.queryString, data);
    }

    stop(): void {
        this.pollXhr.abort();
    }
}