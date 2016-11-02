class LongPollingTransport implements ITransport {
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