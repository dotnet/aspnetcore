import { ConnectionClosed } from "./Common"
import { IConnection } from "./IConnection"
import { Connection } from "./Connection"
import { TransportType } from "./Transports"

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

export { Connection } from "./Connection"
export { TransportType } from "./Transports"

export class HubConnection {
    private connection: IConnection;
    private callbacks: Map<string, (invocationDescriptor: InvocationResultDescriptor) => void>;
    private methods: Map<string, (...args: any[]) => void>;
    private id: number;
    private connectionClosedCallback: ConnectionClosed;

    static create(url: string, queryString?: string): HubConnection {
        return new this(new Connection(url, queryString))
    }

    constructor(connection: IConnection);
    constructor(url: string, queryString?: string);
    constructor(connectionOrUrl: IConnection | string, queryString?: string) {
        this.connection = typeof connectionOrUrl === "string" ? new Connection(connectionOrUrl, queryString) : connectionOrUrl;
        this.connection.onDataReceived = data => {
            this.onDataReceived(data);
        };
        this.connection.onClosed = (error: Error) => {
            this.onConnectionClosed(error);
        }

        this.callbacks = new Map<string, (invocationDescriptor: InvocationResultDescriptor) => void>();
        this.methods = new Map<string, (...args: any[]) => void>();
        this.id = 0;
    }

    private onDataReceived(data: any) {
        // TODO: separate JSON parsing
        // Can happen if a poll request was cancelled
        if (!data) {
            return;
        }
        var descriptor = JSON.parse(data);
        if (descriptor.Method === undefined) {
            let invocationResult: InvocationResultDescriptor = descriptor;
            let callback = this.callbacks.get(invocationResult.Id);
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

    private onConnectionClosed(error: Error) {
        let errorInvocationResult = {
            Id: "-1",
            Error: error ? error.message : "Invocation cancelled due to connection being closed.",
            Result: null
        } as InvocationResultDescriptor;

        this.callbacks.forEach(callback => {
            callback(errorInvocationResult);
        });
        this.callbacks.clear();

        if (this.connectionClosedCallback) {
            this.connectionClosedCallback(error);
        }
    }

    start(transportType?: TransportType): Promise<void> {
        return this.connection.start(transportType);
    }

    stop(): void {
        return this.connection.stop();
    }

    invoke(methodName: string, ...args: any[]): Promise<any> {
        let id = this.id;
        this.id++;

        let invocationDescriptor: InvocationDescriptor = {
            "Id": id.toString(),
            "Method": methodName,
            "Arguments": args
        };

        let p = new Promise<any>((resolve, reject) => {
            this.callbacks.set(invocationDescriptor.Id, (invocationResult: InvocationResultDescriptor) => {
                if (invocationResult.Error != null) {
                    reject(new Error(invocationResult.Error));
                }
                else {
                    resolve(invocationResult.Result);
                }
            });

            //TODO: separate conversion to enable different data formats
            this.connection.send(JSON.stringify(invocationDescriptor))
                .catch(e => {
                    reject(e);
                    this.callbacks.delete(invocationDescriptor.Id);
                });
        });

        return p;
    }

    on(methodName: string, method: (...args: any[]) => void) {
        this.methods[methodName] = method;
    }

    set onClosed(callback: ConnectionClosed) {
        this.connectionClosedCallback = callback;
    }
}
