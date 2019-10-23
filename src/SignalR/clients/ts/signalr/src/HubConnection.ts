// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

import { HandshakeProtocol, HandshakeRequestMessage, HandshakeResponseMessage } from "./HandshakeProtocol";
import { IConnection } from "./IConnection";
import { CancelInvocationMessage, CompletionMessage, IHubProtocol, InvocationMessage, MessageType, StreamInvocationMessage, StreamItemMessage } from "./IHubProtocol";
import { ILogger, LogLevel } from "./ILogger";
import { IRetryPolicy } from "./IRetryPolicy";
import { IStreamResult } from "./Stream";
import { Subject } from "./Subject";
import { Arg } from "./Utils";

const DEFAULT_TIMEOUT_IN_MS: number = 30 * 1000;
const DEFAULT_PING_INTERVAL_IN_MS: number = 15 * 1000;

/** Describes the current state of the {@link HubConnection} to the server. */
export enum HubConnectionState {
    /** The hub connection is disconnected. */
    Disconnected = "Disconnected",
    /** The hub connection is connecting. */
    Connecting = "Connecting",
    /** The hub connection is connected. */
    Connected = "Connected",
    /** The hub connection is disconnecting. */
    Disconnecting = "Disconnecting",
    /** The hub connection is reconnecting. */
    Reconnecting = "Reconnecting",
}

/** Represents a connection to a SignalR Hub. */
export class HubConnection {
    private readonly cachedPingMessage: string | ArrayBuffer;
    private readonly connection: IConnection;
    private readonly logger: ILogger;
    private readonly reconnectPolicy?: IRetryPolicy;
    private protocol: IHubProtocol;
    private handshakeProtocol: HandshakeProtocol;
    private callbacks: { [invocationId: string]: (invocationEvent: StreamItemMessage | CompletionMessage | null, error?: Error) => void };
    private methods: { [name: string]: Array<(...args: any[]) => void> };
    private invocationId: number;

    private closedCallbacks: Array<(error?: Error) => void>;
    private reconnectingCallbacks: Array<(error?: Error) => void>;
    private reconnectedCallbacks: Array<(connectionId?: string) => void>;

    private receivedHandshakeResponse: boolean;
    private handshakeResolver!: (value?: PromiseLike<{}>) => void;
    private handshakeRejecter!: (reason?: any) => void;
    private stopDuringStartError?: Error;

    private connectionState: HubConnectionState;
    // connectionStarted is tracked independently from connectionState, so we can check if the
    // connection ever did successfully transition from connecting to connected before disconnecting.
    private connectionStarted: boolean;
    private startPromise?: Promise<void>;
    private stopPromise?: Promise<void>;

    // The type of these a) doesn't matter and b) varies when building in browser and node contexts
    // Since we're building the WebPack bundle directly from the TypeScript, this matters (previously
    // we built the bundle from the compiled JavaScript).
    private reconnectDelayHandle?: any;
    private timeoutHandle?: any;
    private pingServerHandle?: any;

    /** The server timeout in milliseconds.
     *
     * If this timeout elapses without receiving any messages from the server, the connection will be terminated with an error.
     * The default timeout value is 30,000 milliseconds (30 seconds).
     */
    public serverTimeoutInMilliseconds: number;

    /** Default interval at which to ping the server.
     *
     * The default value is 15,000 milliseconds (15 seconds).
     * Allows the server to detect hard disconnects (like when a client unplugs their computer).
     */
    public keepAliveIntervalInMilliseconds: number;

    /** @internal */
    // Using a public static factory method means we can have a private constructor and an _internal_
    // create method that can be used by HubConnectionBuilder. An "internal" constructor would just
    // be stripped away and the '.d.ts' file would have no constructor, which is interpreted as a
    // public parameter-less constructor.
    public static create(connection: IConnection, logger: ILogger, protocol: IHubProtocol, reconnectPolicy?: IRetryPolicy): HubConnection {
        return new HubConnection(connection, logger, protocol, reconnectPolicy);
    }

    private constructor(connection: IConnection, logger: ILogger, protocol: IHubProtocol, reconnectPolicy?: IRetryPolicy) {
        Arg.isRequired(connection, "connection");
        Arg.isRequired(logger, "logger");
        Arg.isRequired(protocol, "protocol");

        this.serverTimeoutInMilliseconds = DEFAULT_TIMEOUT_IN_MS;
        this.keepAliveIntervalInMilliseconds = DEFAULT_PING_INTERVAL_IN_MS;

        this.logger = logger;
        this.protocol = protocol;
        this.connection = connection;
        this.reconnectPolicy = reconnectPolicy;
        this.handshakeProtocol = new HandshakeProtocol();

        this.connection.onreceive = (data: any) => this.processIncomingData(data);
        this.connection.onclose = (error?: Error) => this.connectionClosed(error);

        this.callbacks = {};
        this.methods = {};
        this.closedCallbacks = [];
        this.reconnectingCallbacks = [];
        this.reconnectedCallbacks = [];
        this.invocationId = 0;
        this.receivedHandshakeResponse = false;
        this.connectionState = HubConnectionState.Disconnected;
        this.connectionStarted = false;

        this.cachedPingMessage = this.protocol.writeMessage({ type: MessageType.Ping });
    }

    /** Indicates the state of the {@link HubConnection} to the server. */
    get state(): HubConnectionState {
        return this.connectionState;
    }

    /** Represents the connection id of the {@link HubConnection} on the server. The connection id will be null when the connection is either
     *  in the disconnected state or if the negotiation step was skipped.
     */
    get connectionId(): string | null {
        return this.connection ? (this.connection.connectionId || null) : null;
    }

    /** Indicates the url of the {@link HubConnection} to the server. */
    get baseUrl(): string {
        return this.connection.baseUrl || "";
    }

    /**
     * Sets a new url for the HubConnection. Note that the url can only be changed when the connection is in either the Disconnected or
     * Reconnecting states.
     * @param {string} url The url to connect to.
     */
    set baseUrl(url: string) {
        if (this.connectionState !== HubConnectionState.Disconnected && this.connectionState !== HubConnectionState.Reconnecting) {
            throw new Error("The HubConnection must be in the Disconnected or Reconnecting state to change the url.");
        }

        if (!url) {
            throw new Error("The HubConnection url must be a valid url.");
        }

        this.connection.baseUrl = url;
    }

    /** Starts the connection.
     *
     * @returns {Promise<void>} A Promise that resolves when the connection has been successfully established, or rejects with an error.
     */
    public start(): Promise<void> {
        this.startPromise = this.startWithStateTransitions();
        return this.startPromise;
    }

    private async startWithStateTransitions(): Promise<void> {
        if (this.connectionState !== HubConnectionState.Disconnected) {
            return Promise.reject(new Error("Cannot start a HubConnection that is not in the 'Disconnected' state."));
        }

        this.connectionState = HubConnectionState.Connecting;
        this.logger.log(LogLevel.Debug, "Starting HubConnection.");

        try {
            await this.startInternal();

            this.connectionState = HubConnectionState.Connected;
            this.connectionStarted = true;
            this.logger.log(LogLevel.Debug, "HubConnection connected successfully.");
        } catch (e) {
            this.connectionState = HubConnectionState.Disconnected;
            this.logger.log(LogLevel.Debug, `HubConnection failed to start successfully because of error '${e}'.`);
            return Promise.reject(e);
        }
    }

    private async startInternal() {
        this.stopDuringStartError = undefined;
        this.receivedHandshakeResponse = false;
        // Set up the promise before any connection is (re)started otherwise it could race with received messages
        const handshakePromise = new Promise((resolve, reject) => {
            this.handshakeResolver = resolve;
            this.handshakeRejecter = reject;
        });

        await this.connection.start(this.protocol.transferFormat);

        try {
            const handshakeRequest: HandshakeRequestMessage = {
                protocol: this.protocol.name,
                version: this.protocol.version,
            };

            this.logger.log(LogLevel.Debug, "Sending handshake request.");

            await this.sendMessage(this.handshakeProtocol.writeHandshakeRequest(handshakeRequest));

            this.logger.log(LogLevel.Information, `Using HubProtocol '${this.protocol.name}'.`);

            // defensively cleanup timeout in case we receive a message from the server before we finish start
            this.cleanupTimeout();
            this.resetTimeoutPeriod();
            this.resetKeepAliveInterval();

            await handshakePromise;

            // It's important to check the stopDuringStartError instead of just relying on the handshakePromise
            // being rejected on close, because this continuation can run after both the handshake completed successfully
            // and the connection was closed.
            if (this.stopDuringStartError) {
                // It's important to throw instead of returning a rejected promise, because we don't want to allow any state
                // transitions to occur between now and the calling code observing the exceptions. Returning a rejected promise
                // will cause the calling continuation to get scheduled to run later.
                throw this.stopDuringStartError;
            }
        } catch (e) {
            this.logger.log(LogLevel.Debug, `Hub handshake failed with error '${e}' during start(). Stopping HubConnection.`);

            this.cleanupTimeout();
            this.cleanupPingTimer();

            // HttpConnection.stop() should not complete until after the onclose callback is invoked.
            // This will transition the HubConnection to the disconnected state before HttpConnection.stop() completes.
            await this.connection.stop(e);
            throw e;
        }
    }

    /** Stops the connection.
     *
     * @returns {Promise<void>} A Promise that resolves when the connection has been successfully terminated, or rejects with an error.
     */
    public async stop(): Promise<void> {
        // Capture the start promise before the connection might be restarted in an onclose callback.
        const startPromise = this.startPromise;

        this.stopPromise = this.stopInternal();
        await this.stopPromise;

        try {
            // Awaiting undefined continues immediately
            await startPromise;
        } catch (e) {
            // This exception is returned to the user as a rejected Promise from the start method.
        }
    }

    private stopInternal(error?: Error): Promise<void> {
        if (this.connectionState === HubConnectionState.Disconnected) {
            this.logger.log(LogLevel.Debug, `Call to HubConnection.stop(${error}) ignored because it is already in the disconnected state.`);
            return Promise.resolve();
        }

        if (this.connectionState === HubConnectionState.Disconnecting) {
            this.logger.log(LogLevel.Debug, `Call to HttpConnection.stop(${error}) ignored because the connection is already in the disconnecting state.`);
            return this.stopPromise!;
        }

        this.connectionState = HubConnectionState.Disconnecting;

        this.logger.log(LogLevel.Debug, "Stopping HubConnection.");

        if (this.reconnectDelayHandle) {
            // We're in a reconnect delay which means the underlying connection is currently already stopped.
            // Just clear the handle to stop the reconnect loop (which no one is waiting on thankfully) and
            // fire the onclose callbacks.
            this.logger.log(LogLevel.Debug, "Connection stopped during reconnect delay. Done reconnecting.");

            clearTimeout(this.reconnectDelayHandle);
            this.reconnectDelayHandle = undefined;

            this.completeClose();
            return Promise.resolve();
        }

        this.cleanupTimeout();
        this.cleanupPingTimer();
        this.stopDuringStartError = error || new Error("The connection was stopped before the hub handshake could complete.");

        // HttpConnection.stop() should not complete until after either HttpConnection.start() fails
        // or the onclose callback is invoked. The onclose callback will transition the HubConnection
        // to the disconnected state if need be before HttpConnection.stop() completes.
        return this.connection.stop(error);
    }

    /** Invokes a streaming hub method on the server using the specified name and arguments.
     *
     * @typeparam T The type of the items returned by the server.
     * @param {string} methodName The name of the server method to invoke.
     * @param {any[]} args The arguments used to invoke the server method.
     * @returns {IStreamResult<T>} An object that yields results from the server as they are received.
     */
    public stream<T = any>(methodName: string, ...args: any[]): IStreamResult<T> {
        const [streams, streamIds] = this.replaceStreamingParams(args);
        const invocationDescriptor = this.createStreamInvocation(methodName, args, streamIds);

        let promiseQueue: Promise<void>;
        const subject = new Subject<T>();
        subject.cancelCallback = () => {
            const cancelInvocation: CancelInvocationMessage = this.createCancelInvocation(invocationDescriptor.invocationId);

            delete this.callbacks[invocationDescriptor.invocationId];

            return promiseQueue.then(() => {
                return this.sendWithProtocol(cancelInvocation);
            });
        };

        this.callbacks[invocationDescriptor.invocationId] = (invocationEvent: CompletionMessage | StreamItemMessage | null, error?: Error) => {
            if (error) {
                subject.error(error);
                return;
            } else if (invocationEvent) {
                // invocationEvent will not be null when an error is not passed to the callback
                if (invocationEvent.type === MessageType.Completion) {
                    if (invocationEvent.error) {
                        subject.error(new Error(invocationEvent.error));
                    } else {
                        subject.complete();
                    }
                } else {
                    subject.next((invocationEvent.item) as T);
                }
            }
        };

        promiseQueue = this.sendWithProtocol(invocationDescriptor)
            .catch((e) => {
                subject.error(e);
                delete this.callbacks[invocationDescriptor.invocationId];
            });

        this.launchStreams(streams, promiseQueue);

        return subject;
    }

    private sendMessage(message: any) {
        this.resetKeepAliveInterval();
        return this.connection.send(message);
    }

    /**
     * Sends a js object to the server.
     * @param message The js object to serialize and send.
     */
    private sendWithProtocol(message: any) {
        return this.sendMessage(this.protocol.writeMessage(message));
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
        const [streams, streamIds] = this.replaceStreamingParams(args);
        const sendPromise = this.sendWithProtocol(this.createInvocation(methodName, args, true, streamIds));

        this.launchStreams(streams, sendPromise);

        return sendPromise;
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
        const [streams, streamIds] = this.replaceStreamingParams(args);
        const invocationDescriptor = this.createInvocation(methodName, args, false, streamIds);

        const p = new Promise<any>((resolve, reject) => {
            // invocationId will always have a value for a non-blocking invocation
            this.callbacks[invocationDescriptor.invocationId!] = (invocationEvent: StreamItemMessage | CompletionMessage | null, error?: Error) => {
                if (error) {
                    reject(error);
                    return;
                } else if (invocationEvent) {
                    // invocationEvent will not be null when an error is not passed to the callback
                    if (invocationEvent.type === MessageType.Completion) {
                        if (invocationEvent.error) {
                            reject(new Error(invocationEvent.error));
                        } else {
                            resolve(invocationEvent.result);
                        }
                    } else {
                        reject(new Error(`Unexpected message type: ${invocationEvent.type}`));
                    }
                }
            };

            const promiseQueue = this.sendWithProtocol(invocationDescriptor)
                .catch((e) => {
                    reject(e);
                    // invocationId will always have a value for a non-blocking invocation
                    delete this.callbacks[invocationDescriptor.invocationId!];
                });

            this.launchStreams(streams, promiseQueue);
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
     * You must pass the exact same Function instance as was previously passed to {@link @microsoft/signalr.HubConnection.on}. Passing a different instance (even if the function
     * body is the same) will not remove the handler.
     *
     * @param {string} methodName The name of the method to remove handlers for.
     * @param {Function} method The handler to remove. This must be the same Function instance as the one passed to {@link @microsoft/signalr.HubConnection.on}.
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

    /** Registers a handler that will be invoked when the connection starts reconnecting.
     *
     * @param {Function} callback The handler that will be invoked when the connection starts reconnecting. Optionally receives a single argument containing the error that caused the connection to start reconnecting (if any).
     */
    public onreconnecting(callback: (error?: Error) => void) {
        if (callback) {
            this.reconnectingCallbacks.push(callback);
        }
    }

    /** Registers a handler that will be invoked when the connection successfully reconnects.
     *
     * @param {Function} callback The handler that will be invoked when the connection successfully reconnects.
     */
    public onreconnected(callback: (connectionId?: string) => void) {
        if (callback) {
            this.reconnectedCallbacks.push(callback);
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
                        if (callback) {
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

                        const error = message.error ? new Error("Server returned an error on close: " + message.error) : undefined;

                        if (message.allowReconnect === true) {
                            // It feels wrong not to await connection.stop() here, but processIncomingData is called as part of an onreceive callback which is not async,
                            // this is already the behavior for serverTimeout(), and HttpConnection.Stop() should catch and log all possible exceptions.

                            // tslint:disable-next-line:no-floating-promises
                            this.connection.stop(error);
                        } else {
                            // We cannot await stopInternal() here, but subsequent calls to stop() will await this if stopInternal() is still ongoing.
                            this.stopPromise = this.stopInternal(error);
                        }

                        break;
                    default:
                        this.logger.log(LogLevel.Warning, `Invalid message type: ${message.type}.`);
                        break;
                }
            }
        }

        this.resetTimeoutPeriod();
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
            this.handshakeRejecter(error);
            throw error;
        }
        if (responseMessage.error) {
            const message = "Server returned handshake error: " + responseMessage.error;
            this.logger.log(LogLevel.Error, message);

            const error = new Error(message);
            this.handshakeRejecter(error);
            throw error;
        } else {
            this.logger.log(LogLevel.Debug, "Server handshake complete.");
        }

        this.handshakeResolver();
        return remainingData;
    }

    private resetKeepAliveInterval() {
        this.cleanupPingTimer();
        this.pingServerHandle = setTimeout(async () => {
            if (this.connectionState === HubConnectionState.Connected) {
                try {
                    await this.sendMessage(this.cachedPingMessage);
                } catch {
                    // We don't care about the error. It should be seen elsewhere in the client.
                    // The connection is probably in a bad or closed state now, cleanup the timer so it stops triggering
                    this.cleanupPingTimer();
                }
            }
        }, this.keepAliveIntervalInMilliseconds);
    }

    private resetTimeoutPeriod() {
        if (!this.connection.features || !this.connection.features.inherentKeepAlive) {
            // Set the timeout timer
            this.timeoutHandle = setTimeout(() => this.serverTimeout(), this.serverTimeoutInMilliseconds);
        }
    }

    private serverTimeout() {
        // The server hasn't talked to us in a while. It doesn't like us anymore ... :(
        // Terminate the connection, but we don't need to wait on the promise. This could trigger reconnecting.
        // tslint:disable-next-line:no-floating-promises
        this.connection.stop(new Error("Server timeout elapsed without receiving a message from the server."));
    }

    private invokeClientMethod(invocationMessage: InvocationMessage) {
        const methods = this.methods[invocationMessage.target.toLowerCase()];
        if (methods) {
            try {
                methods.forEach((m) => m.apply(this, invocationMessage.arguments));
            } catch (e) {
                this.logger.log(LogLevel.Error, `A callback for the method ${invocationMessage.target.toLowerCase()} threw error '${e}'.`);
            }

            if (invocationMessage.invocationId) {
                // This is not supported in v1. So we return an error to avoid blocking the server waiting for the response.
                const message = "Server requested a response, which is not supported in this version of the client.";
                this.logger.log(LogLevel.Error, message);

                // We don't want to wait on the stop itself.
                this.stopPromise = this.stopInternal(new Error(message));
            }
        } else {
            this.logger.log(LogLevel.Warning, `No client method with the name '${invocationMessage.target}' found.`);
        }
    }

    private connectionClosed(error?: Error) {
        this.logger.log(LogLevel.Debug, `HubConnection.connectionClosed(${error}) called while in state ${this.connectionState}.`);

        // Triggering this.handshakeRejecter is insufficient because it could already be resolved without the continuation having run yet.
        this.stopDuringStartError = this.stopDuringStartError || error || new Error("The underlying connection was closed before the hub handshake could complete.");

        // If the handshake is in progress, start will be waiting for the handshake promise, so we complete it.
        // If it has already completed, this should just noop.
        if (this.handshakeResolver) {
            this.handshakeResolver();
        }

        this.cancelCallbacksWithError(error || new Error("Invocation canceled due to the underlying connection being closed."));

        this.cleanupTimeout();
        this.cleanupPingTimer();

        if (this.connectionState === HubConnectionState.Disconnecting) {
            this.completeClose(error);
        } else if (this.connectionState === HubConnectionState.Connected && this.reconnectPolicy) {
            // tslint:disable-next-line:no-floating-promises
            this.reconnect(error);
        } else if (this.connectionState === HubConnectionState.Connected) {
            this.completeClose(error);
        }

        // If none of the above if conditions were true were called the HubConnection must be in either:
        // 1. The Connecting state in which case the handshakeResolver will complete it and stopDuringStartError will fail it.
        // 2. The Reconnecting state in which case the handshakeResolver will complete it and stopDuringStartError will fail the current reconnect attempt
        //    and potentially continue the reconnect() loop.
        // 3. The Disconnected state in which case we're already done.
    }

    private completeClose(error?: Error) {
        if (this.connectionStarted) {
            this.connectionState = HubConnectionState.Disconnected;
            this.connectionStarted = false;

            try {
                this.closedCallbacks.forEach((c) => c.apply(this, [error]));
            } catch (e) {
                this.logger.log(LogLevel.Error, `An onclose callback called with error '${error}' threw error '${e}'.`);
            }
        }
    }

    private async reconnect(error?: Error) {
        const reconnectStartTime = Date.now();
        let previousReconnectAttempts = 0;
        let retryError = error !== undefined ? error : new Error("Attempting to reconnect due to a unknown error.");

        let nextRetryDelay = this.getNextRetryDelay(previousReconnectAttempts++, 0, retryError);

        if (nextRetryDelay === null) {
            this.logger.log(LogLevel.Debug, "Connection not reconnecting because the IRetryPolicy returned null on the first reconnect attempt.");
            this.completeClose(error);
            return;
        }

        this.connectionState = HubConnectionState.Reconnecting;

        if (error) {
            this.logger.log(LogLevel.Information, `Connection reconnecting because of error '${error}'.`);
        } else {
            this.logger.log(LogLevel.Information, "Connection reconnecting.");
        }

        if (this.onreconnecting) {
            try {
                this.reconnectingCallbacks.forEach((c) => c.apply(this, [error]));
            } catch (e) {
                this.logger.log(LogLevel.Error, `An onreconnecting callback called with error '${error}' threw error '${e}'.`);
            }

            // Exit early if an onreconnecting callback called connection.stop().
            if (this.connectionState !== HubConnectionState.Reconnecting) {
                this.logger.log(LogLevel.Debug, "Connection left the reconnecting state in onreconnecting callback. Done reconnecting.");
                return;
            }
        }

        while (nextRetryDelay !== null) {
            this.logger.log(LogLevel.Information, `Reconnect attempt number ${previousReconnectAttempts} will start in ${nextRetryDelay} ms.`);

            await new Promise((resolve) => {
                this.reconnectDelayHandle = setTimeout(resolve, nextRetryDelay!);
            });
            this.reconnectDelayHandle = undefined;

            if (this.connectionState !== HubConnectionState.Reconnecting) {
                this.logger.log(LogLevel.Debug, "Connection left the reconnecting state during reconnect delay. Done reconnecting.");
                return;
            }

            try {
                await this.startInternal();

                this.connectionState = HubConnectionState.Connected;
                this.logger.log(LogLevel.Information, "HubConnection reconnected successfully.");

                if (this.onreconnected) {
                    try {
                        this.reconnectedCallbacks.forEach((c) => c.apply(this, [this.connection.connectionId]));
                    } catch (e) {
                        this.logger.log(LogLevel.Error, `An onreconnected callback called with connectionId '${this.connection.connectionId}; threw error '${e}'.`);
                    }
                }

                return;
            } catch (e) {
                this.logger.log(LogLevel.Information, `Reconnect attempt failed because of error '${e}'.`);

                if (this.connectionState !== HubConnectionState.Reconnecting) {
                    this.logger.log(LogLevel.Debug, "Connection left the reconnecting state during reconnect attempt. Done reconnecting.");
                    return;
                }

                retryError = e instanceof Error ? e : new Error(e.toString());
                nextRetryDelay = this.getNextRetryDelay(previousReconnectAttempts++, Date.now() - reconnectStartTime, retryError);
            }
        }

        this.logger.log(LogLevel.Information, `Reconnect retries have been exhausted after ${Date.now() - reconnectStartTime} ms and ${previousReconnectAttempts} failed attempts. Connection disconnecting.`);

        this.completeClose();
    }

    private getNextRetryDelay(previousRetryCount: number, elapsedMilliseconds: number, retryReason: Error) {
        try {
            return this.reconnectPolicy!.nextRetryDelayInMilliseconds({
                elapsedMilliseconds,
                previousRetryCount,
                retryReason,
            });
        } catch (e) {
            this.logger.log(LogLevel.Error, `IRetryPolicy.nextRetryDelayInMilliseconds(${previousRetryCount}, ${elapsedMilliseconds}) threw error '${e}'.`);
            return null;
        }
    }

    private cancelCallbacksWithError(error: Error) {
        const callbacks = this.callbacks;
        this.callbacks = {};

        Object.keys(callbacks)
            .forEach((key) => {
                const callback = callbacks[key];
                callback(null, error);
            });
    }

    private cleanupPingTimer(): void {
        if (this.pingServerHandle) {
            clearTimeout(this.pingServerHandle);
        }
    }

    private cleanupTimeout(): void {
        if (this.timeoutHandle) {
            clearTimeout(this.timeoutHandle);
        }
    }

    private createInvocation(methodName: string, args: any[], nonblocking: boolean, streamIds: string[]): InvocationMessage {
        if (nonblocking) {
            return {
                arguments: args,
                streamIds,
                target: methodName,
                type: MessageType.Invocation,
            };
        } else {
            const invocationId = this.invocationId;
            this.invocationId++;

            return {
                arguments: args,
                invocationId: invocationId.toString(),
                streamIds,
                target: methodName,
                type: MessageType.Invocation,
            };
        }
    }

    private launchStreams(streams: Array<IStreamResult<any>>, promiseQueue: Promise<void>): void {
        if (streams.length === 0) {
            return;
        }

        // Synchronize stream data so they arrive in-order on the server
        if (!promiseQueue) {
            promiseQueue = Promise.resolve();
        }

        // We want to iterate over the keys, since the keys are the stream ids
        // tslint:disable-next-line:forin
        for (const streamId in streams) {
            streams[streamId].subscribe({
                complete: () => {
                    promiseQueue = promiseQueue.then(() => this.sendWithProtocol(this.createCompletionMessage(streamId)));
                },
                error: (err) => {
                    let message: string;
                    if (err instanceof Error) {
                        message = err.message;
                    } else if (err && err.toString) {
                        message = err.toString();
                    } else {
                        message = "Unknown error";
                    }

                    promiseQueue = promiseQueue.then(() => this.sendWithProtocol(this.createCompletionMessage(streamId, message)));
                },
                next: (item) => {
                    promiseQueue = promiseQueue.then(() => this.sendWithProtocol(this.createStreamItemMessage(streamId, item)));
                },
            });
        }
    }

    private replaceStreamingParams(args: any[]): [Array<IStreamResult<any>>, string[]] {
        const streams: Array<IStreamResult<any>> = [];
        const streamIds: string[] = [];
        for (let i = 0; i < args.length; i++) {
            const argument = args[i];
            if (this.isObservable(argument)) {
                const streamId = this.invocationId;
                this.invocationId++;
                // Store the stream for later use
                streams[streamId] = argument;
                streamIds.push(streamId.toString());

                // remove stream from args
                args.splice(i, 1);
            }
        }

        return [streams, streamIds];
    }

    private isObservable(arg: any): arg is IStreamResult<any> {
        // This allows other stream implementations to just work (like rxjs)
        return arg && arg.subscribe && typeof arg.subscribe === "function";
    }

    private createStreamInvocation(methodName: string, args: any[], streamIds: string[]): StreamInvocationMessage {
        const invocationId = this.invocationId;
        this.invocationId++;

        return {
            arguments: args,
            invocationId: invocationId.toString(),
            streamIds,
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

    private createStreamItemMessage(id: string, item: any): StreamItemMessage {
        return {
            invocationId: id,
            item,
            type: MessageType.StreamItem,
        };
    }

    private createCompletionMessage(id: string, error?: any, result?: any): CompletionMessage {
        if (error) {
            return {
                error,
                invocationId: id,
                type: MessageType.Completion,
            };
        }

        return {
            invocationId: id,
            result,
            type: MessageType.Completion,
        };
    }
}
