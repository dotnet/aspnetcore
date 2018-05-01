// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

import { HandshakeProtocol, HandshakeRequestMessage, HandshakeResponseMessage } from "./HandshakeProtocol";
import { IConnection } from "./IConnection";
import { CancelInvocationMessage, CompletionMessage, IHubProtocol, InvocationMessage, MessageType, StreamInvocationMessage, StreamItemMessage } from "./IHubProtocol";
import { ILogger, LogLevel } from "./ILogger";
import { IStreamResult } from "./Stream";
import { Arg, Subject } from "./Utils";

const DEFAULT_TIMEOUT_IN_MS: number = 30 * 1000;

/** Represents a connection to a SignalR Hub. */
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

    /** The server timeout in milliseconds.
     *
     * If this timeout elapses without receiving any messages from the server, the connection will be terminated with an error.
     * The default timeout value is 30,000 milliseconds (30 seconds).
     */
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

    /** Starts the connection.
     *
     * @returns {Promise<void>} A Promise that resolves when the connection has been successfully established, or rejects with an error.
     */
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

    /** Stops the connection.
     *
     * @returns {Promise<void>} A Promise that resolves when the connection has been successfully terminated, or rejects with an error.
     */
    public stop(): Promise<void> {
        this.logger.log(LogLevel.Debug, "Stopping HubConnection.");

        this.cleanupTimeout();
        return this.connection.stop();
    }

    /** Invokes a streaming hub method on the server using the specified name and arguments.
     *
     * @typeparam T The type of the items returned by the server.
     * @param {string} methodName The name of the server method to invoke.
     * @param {any[]} args The arguments used to invoke the server method.
     * @returns {IStreamResult<T>} An object that yields results from the server as they are received.
     */
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

    /** Invokes a hub method on the server using the specified name and arguments. Does not wait for a response from the receiver.
     *
     * The Promise returned by this method resolves when the client has sent the invocation to the server. The server may still
     * be processing the invocation.
     *
     * @param {string} methodName The name of the server method to invoke.
     * @param {any[]} args The arguments used to invoke the server method.
     * @returns {Promise<void>} A Promise that resolves when the invocation has been successfully sent, or rejects with an error.
     */
    public send(methodName: string, ...args: any[]): Promise<void> {
        const invocationDescriptor = this.createInvocation(methodName, args, true);

        const message = this.protocol.writeMessage(invocationDescriptor);

        return this.connection.send(message);
    }

    /** Invokes a hub method on the server using the specified name and arguments.
     *
     * The Promise returned by this method resolves when the server indicates it has finished invoking the method. When the promise
     * resolves, the server has finished invoking the method. If the server method returns a result, it is produced as the result of
     * resolving the Promise.
     *
     * @typeparam T The expected return type.
     * @param {string} methodName The name of the server method to invoke.
     * @param {any[]} args The arguments used to invoke the server method.
     * @returns {Promise<T>} A Promise that resolves with the result of the server method (if any), or rejects with an error.
     */
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

    /** Registers a handler that will be invoked when the hub method with the specified method name is invoked.
     *
     * @param {string} methodName The name of the hub method to define.
     * @param {Function} newMethod The handler that will be raised when the hub method is invoked.
     */
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

    /** Removes all handlers for the specified hub method.
     *
     * @param {string} methodName The name of the method to remove handlers for.
     */
    public off(methodName: string): void;

    /** Removes the specified handler for the specified hub method.
     *
     * You must pass the exact same Function instance as was previously passed to {@link on}. Passing a different instance (even if the function
     * body is the same) will not remove the handler.
     *
     * @param {string} methodName The name of the method to remove handlers for.
     * @param {Function} method The handler to remove. This must be the same Function instance as the one passed to {@link on}.
     */
    public off(methodName: string, method: (...args: any[]) => void): void;
    public off(methodName: string, method?: (...args: any[]) => void): void {
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

    /** Registers a handler that will be invoked when the connection is closed.
     *
     * @param {Function} callback The handler that will be invoked when the connection is closed. Optionally receives a single argument containing the error that caused the connection to close (if any).
     */
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
