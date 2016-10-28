interface InvocationDescriptor {
    readonly Id: string;
    readonly Method: string;
    readonly Arguments: Array<any>;
}

interface InvocationResultDescriptor {
    readonly Id: string;
    readonly Error: string;
    readonly Result: any;
}

class RpcConnection {
    // TODO: add connection state
    private url: string;
    private queryString: string;
    private callbacks: Map<string, (any) => void>;
    private methods: Map<string, (...args:any[]) => void>;
    private transport: ITransport;
    private id: number;

    constructor(url: string, queryString?: string) {
        this.url = url;
        this.queryString = queryString || "";
        this.callbacks = new Map<string, (any) => void>();
        this.methods = new Map<string, (...args:any[]) => void>();
        this.id = 0;
    }

    private messageReceived(data: string) {
        //TODO: separate JSON parsing
        var descriptor = JSON.parse(data);
        if (descriptor.Method === undefined) {
            let invocationResult: InvocationResultDescriptor = descriptor;
            let callback = this.callbacks[invocationResult.Id];
            if (callback != null) {
                callback(invocationResult);
                this.callbacks.delete(invocationResult.Id);
            }
        }
        else {
            let invocation: InvocationDescriptor = descriptor;
            let method = this.methods[invocation.Method];
            if (method != null) {
                // TODO: bind? args?
                method.apply(this, invocation.Arguments);
            }
        }
    }

    start(): Promise<void> {
        return new Promise((resolve, reject) => {
            new HttpClient().get(this.url + "/getid?" + this.queryString)
            .then(id => {
                // this.transport = new WebSocketTransport(data => this.messageReceived(data));
                this.transport = new LongPollingTransport(data => this.messageReceived(data));
                return this.transport.connect(this.url, `id=${id}&${this.queryString}`);
            })
            .then(() => {
                resolve();
            })
            .catch(() => {
                reject();
            });
        });
    }

    stop(): void {
        this.transport.stop();
    }

    invoke(methodName: string, ...args: any[]): Promise<void> {
        let invocationDescriptor: InvocationDescriptor = {
            "Id": this.id.toString(),
            "Method": methodName,
            "Arguments": args
        };

        let p = new Promise<any>((resolve, reject) => {
            this.callbacks[this.id] = (invocationResult: InvocationResultDescriptor) => {
                if (invocationResult.Error != null) {
                    reject(invocationResult.Error);
                }
                else {
                    resolve(invocationResult.Result);
                }
            };

            this.transport.send(JSON.stringify(invocationDescriptor))
                .catch(e => reject(e));
        });

        this.id++;

        //TODO: separate conversion to enable different data formats
        return p;
    }

    on(methodName: string, method: (...args: any[]) => void) {
        this.methods[methodName] = method;
    }
}