import { ConnectionClosed } from "./Common"
import { IConnection } from "./IConnection"
import { Connection } from "./Connection"
import { TransportType } from "./Transports"
import { Subject, Observable } from "./Observable"

const enum MessageType {
    Invocation = 1,
    Result,
    Completion
}

interface HubMessage {
    readonly type: MessageType;
    readonly invocationId: string;
}

interface InvocationMessage extends HubMessage {
    readonly target: string;
    readonly arguments: Array<any>;
    readonly nonblocking?: boolean;
}

interface ResultMessage extends HubMessage {
    readonly item?: any;
}

interface CompletionMessage extends HubMessage {
    readonly error?: string;
    readonly result?: any;
}

export { Connection } from "./Connection"
export { TransportType } from "./Transports"

export class HubConnection {
    private connection: IConnection;
    private callbacks: Map<string, (invocationUpdate: CompletionMessage | ResultMessage) => void>;
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

        this.callbacks = new Map<string, (invocationEvent: CompletionMessage | ResultMessage) => void>();
        this.methods = new Map<string, (...args: any[]) => void>();
        this.id = 0;
    }

    private onDataReceived(data: any) {
        // TODO: separate JSON parsing
        // Can happen if a poll request was cancelled
        if (!data) {
            return;
        }

        var message = JSON.parse(data);
        switch (message.type) {
            case MessageType.Invocation:
                this.InvokeClientMethod(<InvocationMessage>message);
                break;
            case MessageType.Result:
            case MessageType.Completion:
                let callback = this.callbacks.get(message.invocationId);
                if (callback != null) {
                    callback(message);

                    if (message.type == MessageType.Completion) {
                        this.callbacks.delete(message.invocationId);
                    }
                }
                break;
            default:
                console.log("Invalid message type: " + data);
                break;
        }
    }

    private InvokeClientMethod(invocationMessage: InvocationMessage) {
        let method = this.methods.get(invocationMessage.target);
        if (method) {
            method.apply(this, invocationMessage.arguments);
            if (!invocationMessage.nonblocking) {
                // TODO: send result back to the server?
            }
        }
        else {
            console.log(`No client method with the name '${invocationMessage.target}' found.`);
        }
    }

    private onConnectionClosed(error: Error) {
        let errorCompletionMessage = <CompletionMessage>{
            type: MessageType.Completion,
            invocationId: "-1",
            error: error ? error.message : "Invocation cancelled due to connection being closed.",
        };

        this.callbacks.forEach(callback => {
            callback(errorCompletionMessage);
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

    stream<T>(methodName: string, ...args: any[]): Observable<T> {
        let invocationDescriptor = this.createInvocation(methodName, args);

        let subject = new Subject<T>();

        this.callbacks.set(invocationDescriptor.invocationId, (invocationEvent: CompletionMessage | ResultMessage) => {
            if (invocationEvent.type === MessageType.Completion) {
                let completionMessage = <CompletionMessage>invocationEvent;
                if (completionMessage.error) {
                    subject.error(new Error(completionMessage.error));
                }
                else if(completionMessage.result) {
                    subject.error(new Error("Server provided a result in a completion response to a streamed invocation."));
                }
                else {
                    // TODO: Log a warning if there's a payload?
                    subject.complete();
                }
            }
            else {
                subject.next(<T>(<ResultMessage>invocationEvent).item);
            }
        });

        //TODO: separate conversion to enable different data formats
        this.connection.send(JSON.stringify(invocationDescriptor))
            .catch(e => {
                subject.error(e);
                this.callbacks.delete(invocationDescriptor.invocationId);
            });

        return subject;
    }

    invoke(methodName: string, ...args: any[]): Promise<any> {
        let invocationDescriptor = this.createInvocation(methodName, args);

        let p = new Promise<any>((resolve, reject) => {
            this.callbacks.set(invocationDescriptor.invocationId, (invocationEvent: CompletionMessage | ResultMessage) => {
                if (invocationEvent.type === MessageType.Completion) {
                    let completionMessage = <CompletionMessage>invocationEvent;
                    if (completionMessage.error) {
                        reject(new Error(completionMessage.error));
                    }
                    else {
                        resolve(completionMessage.result);
                    }
                }
                else {
                    reject(new Error("Streaming methods must be invoked using HubConnection.stream"))
                }
            });

            //TODO: separate conversion to enable different data formats
            this.connection.send(JSON.stringify(invocationDescriptor))
                .catch(e => {
                    reject(e);
                    this.callbacks.delete(invocationDescriptor.invocationId);
                });
        });

        return p;
    }

    on(methodName: string, method: (...args: any[]) => void) {
        this.methods.set(methodName, method);
    }

    set onClosed(callback: ConnectionClosed) {
        this.connectionClosedCallback = callback;
    }

    private createInvocation(methodName: string, args: any[]): InvocationMessage {
        let id = this.id;
        this.id++;

        return <InvocationMessage>{
            type: MessageType.Invocation,
            invocationId: id.toString(),
            target: methodName,
            arguments: args,
            nonblocking: false
        };
    }
}
