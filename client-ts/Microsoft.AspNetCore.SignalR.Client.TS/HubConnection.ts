// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

import { ConnectionClosed } from "./Common"
import { IConnection } from "./IConnection"
import { HttpConnection } from "./HttpConnection"
import { TransportType, TransferMode } from "./Transports"
import { Subject, Observable } from "./Observable"
import { IHubProtocol, ProtocolType, MessageType, HubMessage, CompletionMessage, ResultMessage, InvocationMessage, StreamInvocationMessage, NegotiationMessage, HubInvocationMessage } from "./IHubProtocol";
import { JsonHubProtocol } from "./JsonHubProtocol";
import { TextMessageFormat } from "./Formatters"
import { Base64EncodedHubProtocol } from "./Base64EncodedHubProtocol"
import { ILogger, LogLevel } from "./ILogger"
import { ConsoleLogger, NullLogger, LoggerFactory } from "./Loggers"
import { IHubConnectionOptions } from "./IHubConnectionOptions"
import { setTimeout, clearTimeout } from "timers";

export { TransportType } from "./Transports"
export { HttpConnection } from "./HttpConnection"
export { JsonHubProtocol } from "./JsonHubProtocol"
export { LogLevel, ILogger } from "./ILogger"
export { ConsoleLogger, NullLogger } from "./Loggers"

const DEFAULT_SERVER_TIMEOUT_IN_MS: number = 30 * 1000;

export class HubConnection {
    private readonly connection: IConnection;
    private readonly logger: ILogger;
    private protocol: IHubProtocol;
    private callbacks: Map<string, (invocationEvent: HubMessage, error?: Error) => void>;
    private methods: Map<string, ((...args: any[]) => void)[]>;
    private id: number;
    private closedCallbacks: ConnectionClosed[];
    private timeoutHandle: NodeJS.Timer;
    private serverTimeoutInMilliseconds: number;

    constructor(urlOrConnection: string | IConnection, options: IHubConnectionOptions = {}) {
        options = options || {};

        this.serverTimeoutInMilliseconds = options.serverTimeoutInMilliseconds || DEFAULT_SERVER_TIMEOUT_IN_MS;

        if (typeof urlOrConnection === "string") {
            this.connection = new HttpConnection(urlOrConnection, options);
        }
        else {
            this.connection = urlOrConnection;
        }

        this.logger = LoggerFactory.createLogger(options.logging);

        this.protocol = options.protocol || new JsonHubProtocol();
        this.connection.onreceive = (data: any) => this.processIncomingData(data);
        this.connection.onclose = (error?: Error) => this.connectionClosed(error);

        this.callbacks = new Map<string, (invocationEvent: HubMessage, error?: Error) => void>();
        this.methods = new Map<string, ((...args: any[]) => void)[]>();
        this.closedCallbacks = [];
        this.id = 0;
    }

    private processIncomingData(data: any) {
        if (this.timeoutHandle !== undefined) {
            clearTimeout(this.timeoutHandle);
        }

        // Parse the messages
        let messages = this.protocol.parseMessages(data);

        for (var i = 0; i < messages.length; ++i) {
            var message = messages[i];

            switch (message.type) {
                case MessageType.Invocation:
                    this.invokeClientMethod(<InvocationMessage>message);
                    break;
                case MessageType.StreamItem:
                case MessageType.Completion:
                    let callback = this.callbacks.get((<HubInvocationMessage>message).invocationId);
                    if (callback != null) {
                        if (message.type === MessageType.Completion) {
                            this.callbacks.delete((<HubInvocationMessage>message).invocationId);
                        }
                        callback(message);
                    }
                    break;
                case MessageType.Ping:
                    // Don't care about pings
                    break;
                default:
                    this.logger.log(LogLevel.Warning, "Invalid message type: " + data);
                    break;
            }
        }

        this.configureTimeout();
    }

    private configureTimeout() {
        if (!this.connection.features || !this.connection.features.inherentKeepAlive) {
            // Set the timeout timer
            this.timeoutHandle = setTimeout(() => this.serverTimeout(), this.serverTimeoutInMilliseconds);
        }
    }

    private serverTimeout() {
        // The server hasn't talked to us in a while. It doesn't like us anymore ... :(
        // Terminate the connection
        this.connection.stop(new Error("Server timeout elapsed without receiving a message from the server."));
    }

    private invokeClientMethod(invocationMessage: InvocationMessage) {
        let methods = this.methods.get(invocationMessage.target.toLowerCase());
        if (methods) {
            methods.forEach(m => m.apply(this, invocationMessage.arguments));
            if (!invocationMessage.nonblocking) {
                // TODO: send result back to the server?
            }
        }
        else {
            this.logger.log(LogLevel.Warning, `No client method with the name '${invocationMessage.target}' found.`);
        }
    }

    private connectionClosed(error?: Error) {
        this.callbacks.forEach(callback => {
            callback(undefined, error ? error : new Error("Invocation canceled due to connection being closed."));
        });
        this.callbacks.clear();

        this.closedCallbacks.forEach(c => c.apply(this, [error]));
    }

    async start(): Promise<void> {
        let requestedTransferMode =
            (this.protocol.type === ProtocolType.Binary)
                ? TransferMode.Binary
                : TransferMode.Text;

        this.connection.features.transferMode = requestedTransferMode
        await this.connection.start();
        var actualTransferMode = this.connection.features.transferMode;

        await this.connection.send(
            TextMessageFormat.write(
                JSON.stringify(<NegotiationMessage>{ protocol: this.protocol.name })));

        this.logger.log(LogLevel.Information, `Using HubProtocol '${this.protocol.name}'.`);

        if (requestedTransferMode === TransferMode.Binary && actualTransferMode === TransferMode.Text) {
            this.protocol = new Base64EncodedHubProtocol(this.protocol);
        }

        this.configureTimeout();
    }

    stop(): void {
        return this.connection.stop();
    }

    stream<T>(methodName: string, ...args: any[]): Observable<T> {
        let invocationDescriptor = this.createStreamInvocation(methodName, args);

        let subject = new Subject<T>();

        this.callbacks.set(invocationDescriptor.invocationId, (invocationEvent: HubMessage, error?: Error) => {
            if (error) {
                subject.error(error);
                return;
            }

            if (invocationEvent.type === MessageType.Completion) {
                let completionMessage = <CompletionMessage>invocationEvent;
                if (completionMessage.error) {
                    subject.error(new Error(completionMessage.error));
                }
                else {
                    subject.complete();
                }
            }
            else {
                subject.next(<T>(<ResultMessage>invocationEvent).item);
            }
        });

        let message = this.protocol.writeMessage(invocationDescriptor);

        this.connection.send(message)
            .catch(e => {
                subject.error(e);
                this.callbacks.delete(invocationDescriptor.invocationId);
            });

        return subject;
    }

    send(methodName: string, ...args: any[]): Promise<void> {
        let invocationDescriptor = this.createInvocation(methodName, args, true);

        let message = this.protocol.writeMessage(invocationDescriptor);

        return this.connection.send(message);
    }

    invoke(methodName: string, ...args: any[]): Promise<any> {
        let invocationDescriptor = this.createInvocation(methodName, args, false);

        let p = new Promise<any>((resolve, reject) => {
            this.callbacks.set(invocationDescriptor.invocationId, (invocationEvent: HubMessage, error?: Error) => {
                if (error) {
                    reject(error);
                    return;
                }
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
                    reject(new Error(`Unexpected message type: ${invocationEvent.type}`));
                }
            });

            let message = this.protocol.writeMessage(invocationDescriptor);

            this.connection.send(message)
                .catch(e => {
                    reject(e);
                    this.callbacks.delete(invocationDescriptor.invocationId);
                });
        });

        return p;
    }

    on(methodName: string, method: (...args: any[]) => void) {
        if (!methodName || !method) {
            return;
        }

        methodName = methodName.toLowerCase();
        if (!this.methods.has(methodName)) {
            this.methods.set(methodName, []);
        }

        this.methods.get(methodName).push(method);
    }

    off(methodName: string, method: (...args: any[]) => void) {
        if (!methodName || !method) {
            return;
        }

        methodName = methodName.toLowerCase();
        let handlers = this.methods.get(methodName);
        if (!handlers) {
            return;
        }
        var removeIdx = handlers.indexOf(method);
        if (removeIdx != -1) {
            handlers.splice(removeIdx, 1);
        }
    }

    onclose(callback: ConnectionClosed) {
        if (callback) {
            this.closedCallbacks.push(callback);
        }
    }

    private createInvocation(methodName: string, args: any[], nonblocking: boolean): InvocationMessage {
        let id = this.id;
        this.id++;

        return <InvocationMessage>{
            type: MessageType.Invocation,
            invocationId: id.toString(),
            target: methodName,
            arguments: args,
            nonblocking: nonblocking
        };
    }

    private createStreamInvocation(methodName: string, args: any[]): StreamInvocationMessage {
        let id = this.id;
        this.id++;

        return <StreamInvocationMessage>{
            type: MessageType.StreamInvocation,
            invocationId: id.toString(),
            target: methodName,
            arguments: args,
        };
    }
}
