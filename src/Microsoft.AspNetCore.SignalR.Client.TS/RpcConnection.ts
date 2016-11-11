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
    private connection: Connection;
    private callbacks: Map<string, (any) => void>;
    private methods: Map<string, (...args:any[]) => void>;
    private id: number;

    constructor(url: string, queryString?: string) {
        this.connection = new Connection(url, queryString);

        let thisRpcConnection = this;
        this.connection.dataReceived = data => {
            thisRpcConnection.dataReceived(data);
        };

        this.callbacks = new Map<string, (any) => void>();
        this.methods = new Map<string, (...args:any[]) => void>();
        this.id = 0;
    }

    private dataReceived(data: any) {
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

    start(transportName? :string): Promise<void> {
        return this.connection.start(transportName);
    }

    stop(): void {
        return this.connection.stop();
    }

    invoke(methodName: string, ...args: any[]): Promise<void> {

        let id = this.id;
        this.id++;

        let invocationDescriptor: InvocationDescriptor = {
            "Id": id.toString(),
            "Method": methodName,
            "Arguments": args
        };

        let p = new Promise<any>((resolve, reject) => {
            this.callbacks[id] = (invocationResult: InvocationResultDescriptor) => {
                if (invocationResult.Error != null) {
                    reject(invocationResult.Error);
                }
                else {
                    resolve(invocationResult.Result);
                }
            };

            //TODO: separate conversion to enable different data formats
            this.connection.send(JSON.stringify(invocationDescriptor))
                .catch(e => {
                    // TODO: remove callback
                    reject(e);
                });
        });

        return p;
    }

    on(methodName: string, method: (...args: any[]) => void) {
        this.methods[methodName] = method;
    }

    set connectionClosed(callback: ConnectionClosed) {
        this.connection.connectionClosed = callback;
    }
}