// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

import { ConnectionClosed } from "./Common"
import { IConnection } from "./IConnection"
import { TransportType, TransferMode } from "./Transports"
import { Subject, Observable } from "./Observable"
import { IHubProtocol, ProtocolType, MessageType, HubMessage, CompletionMessage, ResultMessage, InvocationMessage, NegotiationMessage } from "./IHubProtocol";
import { JsonHubProtocol } from "./JsonHubProtocol";
import { TextMessageFormat } from "./Formatters"
import { Base64EncodedHubProtocol } from "./Base64EncodedHubProtocol"
import { ILogger, LogLevel } from "./ILogger"
import { ConsoleLogger, NullLogger, LoggerFactory } from "./Loggers"
import { IHubConnectionOptions } from "./IHubConnectionOptions"

export { TransportType } from "./Transports"
export { HttpConnection } from "./HttpConnection"
export { JsonHubProtocol } from "./JsonHubProtocol"
export { LogLevel, ILogger } from "./ILogger"
export { ConsoleLogger, NullLogger } from "./Loggers"

export class HubConnection {
    private readonly connection: IConnection;
    private readonly logger: ILogger;
    private protocol: IHubProtocol;
    private callbacks: Map<string, (invocationUpdate: CompletionMessage | ResultMessage) => void>;
    private methods: Map<string, (...args: any[]) => void>;
    private id: number;
    private connectionClosedCallback: ConnectionClosed;

    constructor(connection: IConnection, options: IHubConnectionOptions = {}) {
        this.connection = connection;
        options = options || {};
        this.logger = LoggerFactory.createLogger(options.logging);

        this.protocol = options.protocol || new JsonHubProtocol();
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
        // Parse the messages
        let messages = this.protocol.parseMessages(data);

        for (var i = 0; i < messages.length; ++i) {
            var message = messages[i];

            switch (message.type) {
                case MessageType.Invocation:
                    this.invokeClientMethod(<InvocationMessage>message);
                    break;
                case MessageType.Result:
                case MessageType.Completion:
                    let callback = this.callbacks.get(message.invocationId);
                    if (callback != null) {
                        if (message.type == MessageType.Completion) {
                            this.callbacks.delete(message.invocationId);
                        }
                        callback(message);
                    }
                    break;
                default:
                    this.logger.log(LogLevel.Warning, "Invalid message type: " + data);
                    break;
            }
        }
    }

    private invokeClientMethod(invocationMessage: InvocationMessage) {
        let method = this.methods.get(invocationMessage.target);
        if (method) {
            method.apply(this, invocationMessage.arguments);
            if (!invocationMessage.nonblocking) {
                // TODO: send result back to the server?
            }
        }
        else {
            this.logger.log(LogLevel.Warning, `No client method with the name '${invocationMessage.target}' found.`);
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
                JSON.stringify(<NegotiationMessage>{ protocol: this.protocol.name})));

        if (requestedTransferMode === TransferMode.Binary && actualTransferMode === TransferMode.Text) {
            this.protocol = new Base64EncodedHubProtocol(this.protocol);
        }
    }

    stop(): void {
        return this.connection.stop();
    }

    stream<T>(methodName: string, ...args: any[]): Observable<T> {
        let invocationDescriptor = this.createInvocation(methodName, args, false);

        let subject = new Subject<T>();

        this.callbacks.set(invocationDescriptor.invocationId, (invocationEvent: CompletionMessage | ResultMessage) => {
            if (invocationEvent.type === MessageType.Completion) {
                let completionMessage = <CompletionMessage>invocationEvent;
                if (completionMessage.error) {
                    subject.error(new Error(completionMessage.error));
                }
                else if (completionMessage.result) {
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
        this.methods.set(methodName, method);
    }

    set onClosed(callback: ConnectionClosed) {
        this.connectionClosedCallback = callback;
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
}
