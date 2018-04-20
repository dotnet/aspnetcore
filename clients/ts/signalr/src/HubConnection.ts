// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

import { HandshakeProtocol, HandshakeRequestMessage, HandshakeResponseMessage } from "./HandshakeProtocol";
import { IConnection } from "./IConnection";
import { CancelInvocationMessage, CompletionMessage, IHubProtocol, InvocationMessage, MessageType, StreamInvocationMessage, StreamItemMessage } from "./IHubProtocol";
import { ILogger, LogLevel } from "./ILogger";
import { IStreamResult } from "./Stream";
import { Arg, Subject } from "./Utils";

const DEFAULT_TIMEOUT_IN_MS: number = 30 * 1000;

export class HubConnection {
    private readonly connection: IConnection;
    private readonly logger: ILogger;
    private protocol: IHubProtocol;
    private handshakeProtocol: HandshakeProtocol;
    private callbacks: { [invocationId: string]: (invocationEvent: StreamItemMessage | CompletionMessage, error?: Error) => void };
    private methods: { [name: string]: Array<(...args: any[]) => void> };
    private id: number;
    private closedCallbacks: Array<(error?: Error) => void>;
    private timeoutHandle: NodeJS.Timer;
    private receivedHandshakeResponse: boolean;

    public serverTimeoutInMilliseconds: number;

    /** @internal */
    // Using a public static factory method means we can have a private constructor and an _internal_
    // create method that can be used by HubConnectionBuilder. An "internal" constructor would just
    // be stripped away and the '.d.ts' file would have no constructor, which is interpreted as a
    // public parameter-less constructor.
    public static create(connection: IConnection, logger: ILogger, protocol: IHubProtocol): HubConnection {
        return new HubConnection(connection, logger, protocol);
    }

    private constructor(connection: IConnection, logger: ILogger, protocol: IHubProtocol) {
        Arg.isRequired(connection, "connection");
        Arg.isRequired(logger, "logger");
        Arg.isRequired(protocol, "protocol");

        this.serverTimeoutInMilliseconds = DEFAULT_TIMEOUT_IN_MS;

        this.logger = logger;
        this.protocol = protocol;
        this.connection = connection;
        this.handshakeProtocol = new HandshakeProtocol();

        this.connection.onreceive = (data: any) => this.processIncomingData(data);
        this.connection.onclose = (error?: Error) => this.connectionClosed(error);

        this.callbacks = {};
        this.methods = {};
        this.closedCallbacks = [];
        this.id = 0;
    }

    public async start(): Promise<void> {
        const handshakeRequest: HandshakeRequestMessage = {
            protocol: this.protocol.name,
            version: this.protocol.version,
        };

        this.logger.log(LogLevel.Debug, "Starting HubConnection.");

        this.receivedHandshakeResponse = false;

        await this.connection.start(this.protocol.transferFormat);

        this.logger.log(LogLevel.Debug, "Sending handshake request.");

        await this.connection.send(this.handshakeProtocol.writeHandshakeRequest(handshakeRequest));

        this.logger.log(LogLevel.Information, `Using HubProtocol '${this.protocol.name}'.`);

        // defensively cleanup timeout in case we receive a message from the server before we finish start
        this.cleanupTimeout();
        this.configureTimeout();
    }

    public stop(): Promise<void> {
        this.logger.log(LogLevel.Debug, "Stopping HubConnection.");

        this.cleanupTimeout();
        return this.connection.stop();
    }

    public stream<T = any>(methodName: string, ...args: any[]): IStreamResult<T> {
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

    public invoke<T = any>(methodName: string, ...args: any[]): Promise<T> {
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

    public onclose(callback: (error?: Error) => void) {
        if (callback) {
            this.closedCallbacks.push(callback);
        }
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
        let remainingData: any;

        try {
            [remainingData, responseMessage] = this.handshakeProtocol.parseHandshakeResponse(data);
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
            this.logger.log(LogLevel.Debug, "Server handshake complete.");
        }

        return remainingData;
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
