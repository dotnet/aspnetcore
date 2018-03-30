// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

import { ConnectionClosed } from "./Common";
import { HttpConnection, IHttpConnectionOptions } from "./HttpConnection";
import { IConnection } from "./IConnection";
import { CancelInvocationMessage, CompletionMessage, HandshakeRequestMessage, HandshakeResponseMessage, HubMessage, IHubProtocol, InvocationMessage, MessageType, StreamInvocationMessage, StreamItemMessage } from "./IHubProtocol";
import { ILogger, LogLevel } from "./ILogger";
import { JsonHubProtocol } from "./JsonHubProtocol";
import { ConsoleLogger, LoggerFactory, NullLogger } from "./Loggers";
import { Observable, Subject } from "./Observable";
import { TextMessageFormat } from "./TextMessageFormat";
import { TransferFormat, TransportType } from "./Transports";

export { JsonHubProtocol };

export interface IHubConnectionOptions extends IHttpConnectionOptions {
    protocol?: IHubProtocol;
    timeoutInMilliseconds?: number;
}

const DEFAULT_TIMEOUT_IN_MS: number = 30 * 1000;

export class HubConnection {
    private readonly connection: IConnection;
    private readonly logger: ILogger;
    private protocol: IHubProtocol;
    private callbacks: { [invocationId: string]: (invocationEvent: StreamItemMessage | CompletionMessage, error?: Error) => void };
    private methods: { [name: string]: Array<(...args: any[]) => void> };
    private id: number;
    private closedCallbacks: ConnectionClosed[];
    private timeoutHandle: NodeJS.Timer;
    private timeoutInMilliseconds: number;
    private receivedHandshakeResponse: boolean;

    constructor(url: string, options?: IHubConnectionOptions);
    constructor(connection: IConnection, options?: IHubConnectionOptions);
    constructor(urlOrConnection: string | IConnection, options: IHubConnectionOptions = {}) {
        options = options || {};

        this.timeoutInMilliseconds = options.timeoutInMilliseconds || DEFAULT_TIMEOUT_IN_MS;

        this.protocol = options.protocol || new JsonHubProtocol();

        if (typeof urlOrConnection === "string") {
            this.connection = new HttpConnection(urlOrConnection, options);
        } else {
            this.connection = urlOrConnection;
        }

        this.logger = LoggerFactory.createLogger(options.logger);

        this.connection.onreceive = (data: any) => this.processIncomingData(data);
        this.connection.onclose = (error?: Error) => this.connectionClosed(error);

        this.callbacks = {};
        this.methods = {};
        this.closedCallbacks = [];
        this.id = 0;
    }

    private processIncomingData(data: any) {
        this.cleanupTimeout();

        if (!this.receivedHandshakeResponse) {
            data = this.processHandshakeResponse(data);
            this.receivedHandshakeResponse = true;
        }

        // Data may have all been read when processing handshake response
        if (data) {
            // Parse the messages
            const messages = this.protocol.parseMessages(data, this.logger);

            for (const message of messages) {
                switch (message.type) {
                    case MessageType.Invocation:
                        this.invokeClientMethod(message);
                        break;
                    case MessageType.StreamItem:
                    case MessageType.Completion:
                        const callback = this.callbacks[message.invocationId];
                        if (callback != null) {
                            if (message.type === MessageType.Completion) {
                                delete this.callbacks[message.invocationId];
                            }
                            callback(message);
                        }
                        break;
                    case MessageType.Ping:
                        // Don't care about pings
                        break;
                    case MessageType.Close:
                        this.logger.log(LogLevel.Information, "Close message received from server.");
                        this.connection.stop(message.error ? new Error("Server returned an error on close: " + message.error) : null);
                        break;
                    default:
                        this.logger.log(LogLevel.Warning, "Invalid message type: " + message.type);
                        break;
                }
            }
        }

        this.configureTimeout();
    }

    private processHandshakeResponse(data: any): any {
        let responseMessage: HandshakeResponseMessage;
        let messageData: string;
        let remainingData: any;
        try {
            if (data instanceof ArrayBuffer) {
                // Format is binary but still need to read JSON text from handshake response
                const binaryData = new Uint8Array(data);
                const separatorIndex = binaryData.indexOf(TextMessageFormat.RecordSeparatorCode);
                if (separatorIndex === -1) {
                    throw new Error("Message is incomplete.");
                }

                // content before separator is handshake response
                // optional content after is additional messages
                const responseLength = separatorIndex + 1;
                messageData = String.fromCharCode.apply(null, binaryData.slice(0, responseLength));
                remainingData = (binaryData.byteLength > responseLength) ? binaryData.slice(responseLength).buffer : null;
            } else {
                const textData: string = data;
                const separatorIndex = textData.indexOf(TextMessageFormat.RecordSeparator);
                if (separatorIndex === -1) {
                    throw new Error("Message is incomplete.");
                }

                // content before separator is handshake response
                // optional content after is additional messages
                const responseLength = separatorIndex + 1;
                messageData = textData.substring(0, responseLength);
                remainingData = (textData.length > responseLength) ? textData.substring(responseLength) : null;
            }

            // At this point we should have just the single handshake message
            const messages = TextMessageFormat.parse(messageData);
            responseMessage = JSON.parse(messages[0]);
        } catch (e) {
            const message = "Error parsing handshake response: " + e;
            this.logger.log(LogLevel.Error, message);

            const error = new Error(message);
            this.connection.stop(error);
            throw error;
        }
        if (responseMessage.error) {
            const message = "Server returned handshake error: " + responseMessage.error;
            this.logger.log(LogLevel.Error, message);
            this.connection.stop(new Error(message));
        } else {
            this.logger.log(LogLevel.Trace, "Server handshake complete.");
        }

        // multiple messages could have arrived with handshake
        // return additional data to be parsed as usual, or null if all parsed
        return remainingData;
    }

    private configureTimeout() {
        if (!this.connection.features || !this.connection.features.inherentKeepAlive) {
            // Set the timeout timer
            this.timeoutHandle = setTimeout(() => this.serverTimeout(), this.timeoutInMilliseconds);
        }
    }

    private serverTimeout() {
        // The server hasn't talked to us in a while. It doesn't like us anymore ... :(
        // Terminate the connection
        this.connection.stop(new Error("Server timeout elapsed without receiving a message from the server."));
    }

    private invokeClientMethod(invocationMessage: InvocationMessage) {
        const methods = this.methods[invocationMessage.target.toLowerCase()];
        if (methods) {
            methods.forEach((m) => m.apply(this, invocationMessage.arguments));
            if (invocationMessage.invocationId) {
                // This is not supported in v1. So we return an error to avoid blocking the server waiting for the response.
                const message = "Server requested a response, which is not supported in this version of the client.";
                this.logger.log(LogLevel.Error, message);
                this.connection.stop(new Error(message));
            }
        } else {
            this.logger.log(LogLevel.Warning, `No client method with the name '${invocationMessage.target}' found.`);
        }
    }

    private connectionClosed(error?: Error) {
        const callbacks = this.callbacks;
        this.callbacks = {};

        Object.keys(callbacks)
            .forEach((key) => {
                const callback = callbacks[key];
                callback(undefined, error ? error : new Error("Invocation canceled due to connection being closed."));
            });

        this.cleanupTimeout();

        this.closedCallbacks.forEach((c) => c.apply(this, [error]));
    }

    public async start(): Promise<void> {
        this.logger.log(LogLevel.Trace, "Starting HubConnection.");

        this.receivedHandshakeResponse = false;

        await this.connection.start(this.protocol.transferFormat);

        this.logger.log(LogLevel.Trace, "Sending handshake request.");
        // Handshake request is always JSON
        await this.connection.send(
            TextMessageFormat.write(
                JSON.stringify({ protocol: this.protocol.name, version: this.protocol.version } as HandshakeRequestMessage)));

        this.logger.log(LogLevel.Information, `Using HubProtocol '${this.protocol.name}'.`);

        // defensively cleanup timeout in case we receive a message from the server before we finish start
        this.cleanupTimeout();
        this.configureTimeout();
    }

    public stop(): Promise<void> {
        this.logger.log(LogLevel.Trace, "Stopping HubConnection.");

        this.cleanupTimeout();
        return this.connection.stop();
    }

    public stream<T>(methodName: string, ...args: any[]): Observable<T> {
        const invocationDescriptor = this.createStreamInvocation(methodName, args);

        const subject = new Subject<T>(() => {
            const cancelInvocation: CancelInvocationMessage = this.createCancelInvocation(invocationDescriptor.invocationId);
            const cancelMessage: any = this.protocol.writeMessage(cancelInvocation);

            delete this.callbacks[invocationDescriptor.invocationId];

            return this.connection.send(cancelMessage);
        });

        this.callbacks[invocationDescriptor.invocationId] = (invocationEvent: CompletionMessage | StreamItemMessage, error?: Error) => {
            if (error) {
                subject.error(error);
                return;
            }

            if (invocationEvent.type === MessageType.Completion) {
                if (invocationEvent.error) {
                    subject.error(new Error(invocationEvent.error));
                } else {
                    subject.complete();
                }
            } else {
                subject.next((invocationEvent.item) as T);
            }
        };

        const message = this.protocol.writeMessage(invocationDescriptor);

        this.connection.send(message)
            .catch((e) => {
                subject.error(e);
                delete this.callbacks[invocationDescriptor.invocationId];
            });

        return subject;
    }

    public send(methodName: string, ...args: any[]): Promise<void> {
        const invocationDescriptor = this.createInvocation(methodName, args, true);

        const message = this.protocol.writeMessage(invocationDescriptor);

        return this.connection.send(message);
    }

    public invoke(methodName: string, ...args: any[]): Promise<any> {
        const invocationDescriptor = this.createInvocation(methodName, args, false);

        const p = new Promise<any>((resolve, reject) => {
            this.callbacks[invocationDescriptor.invocationId] = (invocationEvent: StreamItemMessage | CompletionMessage, error?: Error) => {
                if (error) {
                    reject(error);
                    return;
                }
                if (invocationEvent.type === MessageType.Completion) {
                    const completionMessage = invocationEvent as CompletionMessage;
                    if (completionMessage.error) {
                        reject(new Error(completionMessage.error));
                    } else {
                        resolve(completionMessage.result);
                    }
                } else {
                    reject(new Error(`Unexpected message type: ${invocationEvent.type}`));
                }
            };

            const message = this.protocol.writeMessage(invocationDescriptor);

            this.connection.send(message)
                .catch((e) => {
                    reject(e);
                    delete this.callbacks[invocationDescriptor.invocationId];
                });
        });

        return p;
    }

    public on(methodName: string, newMethod: (...args: any[]) => void) {
        if (!methodName || !newMethod) {
            return;
        }

        methodName = methodName.toLowerCase();
        if (!this.methods[methodName]) {
            this.methods[methodName] = [];
        }

        // Preventing adding the same handler multiple times.
        if (this.methods[methodName].indexOf(newMethod) !== -1) {
            return;
         }

        this.methods[methodName].push(newMethod);
    }

    public off(methodName: string, method?: (...args: any[]) => void) {
        if (!methodName) {
            return;
        }

        methodName = methodName.toLowerCase();
        const handlers = this.methods[methodName];
        if (!handlers) {
            return;
        }
        if (method) {
            const removeIdx = handlers.indexOf(method);
            if (removeIdx !== -1) {
                handlers.splice(removeIdx, 1);
                if (handlers.length === 0) {
                    delete this.methods[methodName];
                }
            }
        } else {
            delete this.methods[methodName];
        }

    }

    public onclose(callback: ConnectionClosed) {
        if (callback) {
            this.closedCallbacks.push(callback);
        }
    }

    private cleanupTimeout(): void {
        if (this.timeoutHandle) {
            clearTimeout(this.timeoutHandle);
        }
    }

    private createInvocation(methodName: string, args: any[], nonblocking: boolean): InvocationMessage {
        if (nonblocking) {
            return {
                arguments: args,
                target: methodName,
                type: MessageType.Invocation,
            };
        } else {
            const id = this.id;
            this.id++;

            return {
                arguments: args,
                invocationId: id.toString(),
                target: methodName,
                type: MessageType.Invocation,
            };
        }
    }

    private createStreamInvocation(methodName: string, args: any[]): StreamInvocationMessage {
        const id = this.id;
        this.id++;

        return {
            arguments: args,
            invocationId: id.toString(),
            target: methodName,
            type: MessageType.StreamInvocation,
        };
    }

    private createCancelInvocation(id: string): CancelInvocationMessage {
        return {
            invocationId: id,
            type: MessageType.CancelInvocation,
        };
    }
}
