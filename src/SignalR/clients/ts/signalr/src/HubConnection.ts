// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import { HandshakeProtocol, HandshakeRequestMessage, HandshakeResponseMessage } from "./HandshakeProtocol";
import { IConnection } from "./IConnection";
import { AbortError } from "./Errors";
import { CancelInvocationMessage, CloseMessage, CompletionMessage, IHubProtocol, InvocationMessage, MessageType, StreamInvocationMessage, StreamItemMessage } from "./IHubProtocol";
import { ILogger, LogLevel } from "./ILogger";
import { IRetryPolicy } from "./IRetryPolicy";
import { IStreamResult } from "./Stream";
import { Subject } from "./Subject";
import { Arg, getErrorString, Platform } from "./Utils";
import { MessageBuffer } from "./MessageBuffer";

const DEFAULT_TIMEOUT_IN_MS: number = 30 * 1000;
const DEFAULT_PING_INTERVAL_IN_MS: number = 15 * 1000;
const DEFAULT_STATEFUL_RECONNECT_BUFFER_SIZE = 100_000;

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
    private readonly _cachedPingMessage: string | ArrayBuffer;
    // Needs to not start with _ for tests
    // eslint-disable-next-line @typescript-eslint/naming-convention
    private readonly connection: IConnection;
    private readonly _logger: ILogger;
    private readonly _reconnectPolicy?: IRetryPolicy;
    private readonly _statefulReconnectBufferSize: number;
    private _protocol: IHubProtocol;
    private _handshakeProtocol: HandshakeProtocol;
    private _callbacks: { [invocationId: string]: (invocationEvent: StreamItemMessage | CompletionMessage | null, error?: Error) => void };
    private _methods: { [name: string]: (((...args: any[]) => void) | ((...args: any[]) => any))[] };
    private _invocationId: number;
    private _messageBuffer?: MessageBuffer;

    private _closedCallbacks: ((error?: Error) => void)[];
    private _reconnectingCallbacks: ((error?: Error) => void)[];
    private _reconnectedCallbacks: ((connectionId?: string) => void)[];

    private _receivedHandshakeResponse: boolean;
    private _handshakeResolver!: (value?: PromiseLike<{}>) => void;
    private _handshakeRejecter!: (reason?: any) => void;
    private _stopDuringStartError?: Error;

    private _connectionState: HubConnectionState;
    // connectionStarted is tracked independently from connectionState, so we can check if the
    // connection ever did successfully transition from connecting to connected before disconnecting.
    private _connectionStarted: boolean;
    private _startPromise?: Promise<void>;
    private _stopPromise?: Promise<void>;
    private _nextKeepAlive: number = 0;

    // The type of these a) doesn't matter and b) varies when building in browser and node contexts
    // Since we're building the WebPack bundle directly from the TypeScript, this matters (previously
    // we built the bundle from the compiled JavaScript).
    private _reconnectDelayHandle?: any;
    private _timeoutHandle?: any;
    private _pingServerHandle?: any;

    private _freezeEventListener = () =>
    {
        this._logger.log(LogLevel.Warning, "The page is being frozen, this will likely lead to the connection being closed and messages being lost. For more information see the docs at https://learn.microsoft.com/aspnet/core/signalr/javascript-client#bsleep");
    };

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
     * The ping will happen at most as often as the server pings.
     * If the server pings every 5 seconds, a value lower than 5 will ping every 5 seconds.
     */
    public keepAliveIntervalInMilliseconds: number;

    /** @internal */
    // Using a public static factory method means we can have a private constructor and an _internal_
    // create method that can be used by HubConnectionBuilder. An "internal" constructor would just
    // be stripped away and the '.d.ts' file would have no constructor, which is interpreted as a
    // public parameter-less constructor.
    public static create(
        connection: IConnection,
        logger: ILogger,
        protocol: IHubProtocol,
        reconnectPolicy?: IRetryPolicy,
        serverTimeoutInMilliseconds?: number,
        keepAliveIntervalInMilliseconds?: number,
        statefulReconnectBufferSize?: number): HubConnection {
        return new HubConnection(connection, logger, protocol, reconnectPolicy,
            serverTimeoutInMilliseconds, keepAliveIntervalInMilliseconds, statefulReconnectBufferSize);
    }

    private constructor(
        connection: IConnection,
        logger: ILogger,
        protocol: IHubProtocol,
        reconnectPolicy?: IRetryPolicy,
        serverTimeoutInMilliseconds?: number,
        keepAliveIntervalInMilliseconds?: number,
        statefulReconnectBufferSize?: number) {
        Arg.isRequired(connection, "connection");
        Arg.isRequired(logger, "logger");
        Arg.isRequired(protocol, "protocol");

        this.serverTimeoutInMilliseconds = serverTimeoutInMilliseconds ?? DEFAULT_TIMEOUT_IN_MS;
        this.keepAliveIntervalInMilliseconds = keepAliveIntervalInMilliseconds ?? DEFAULT_PING_INTERVAL_IN_MS;

        this._statefulReconnectBufferSize = statefulReconnectBufferSize ?? DEFAULT_STATEFUL_RECONNECT_BUFFER_SIZE;

        this._logger = logger;
        this._protocol = protocol;
        this.connection = connection;
        this._reconnectPolicy = reconnectPolicy;
        this._handshakeProtocol = new HandshakeProtocol();

        this.connection.onreceive = (data: any) => this._processIncomingData(data);
        this.connection.onclose = (error?: Error) => this._connectionClosed(error);

        this._callbacks = {};
        this._methods = {};
        this._closedCallbacks = [];
        this._reconnectingCallbacks = [];
        this._reconnectedCallbacks = [];
        this._invocationId = 0;
        this._receivedHandshakeResponse = false;
        this._connectionState = HubConnectionState.Disconnected;
        this._connectionStarted = false;

        this._cachedPingMessage = this._protocol.writeMessage({ type: MessageType.Ping });
    }

    /** Indicates the state of the {@link HubConnection} to the server. */
    get state(): HubConnectionState {
        return this._connectionState;
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
        if (this._connectionState !== HubConnectionState.Disconnected && this._connectionState !== HubConnectionState.Reconnecting) {
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
        this._startPromise = this._startWithStateTransitions();
        return this._startPromise;
    }

    private async _startWithStateTransitions(): Promise<void> {
        if (this._connectionState !== HubConnectionState.Disconnected) {
            return Promise.reject(new Error("Cannot start a HubConnection that is not in the 'Disconnected' state."));
        }

        this._connectionState = HubConnectionState.Connecting;
        this._logger.log(LogLevel.Debug, "Starting HubConnection.");

        try {
            await this._startInternal();

            if (Platform.isBrowser) {
                // Log when the browser freezes the tab so users know why their connection unexpectedly stopped working
                window.document.addEventListener("freeze", this._freezeEventListener);
            }

            this._connectionState = HubConnectionState.Connected;
            this._connectionStarted = true;
            this._logger.log(LogLevel.Debug, "HubConnection connected successfully.");
        } catch (e) {
            this._connectionState = HubConnectionState.Disconnected;
            this._logger.log(LogLevel.Debug, `HubConnection failed to start successfully because of error '${e}'.`);
            return Promise.reject(e);
        }
    }

    private async _startInternal() {
        this._stopDuringStartError = undefined;
        this._receivedHandshakeResponse = false;
        // Set up the promise before any connection is (re)started otherwise it could race with received messages
        const handshakePromise = new Promise((resolve, reject) => {
            this._handshakeResolver = resolve;
            this._handshakeRejecter = reject;
        });

        await this.connection.start(this._protocol.transferFormat);

        try {
            let version = this._protocol.version;
            if (!this.connection.features.reconnect) {
                // Stateful Reconnect starts with HubProtocol version 2, newer clients connecting to older servers will fail to connect due to
                // the handshake only supporting version 1, so we will try to send version 1 during the handshake to keep old servers working.
                version = 1;
            }

            const handshakeRequest: HandshakeRequestMessage = {
                protocol: this._protocol.name,
                version,
            };

            this._logger.log(LogLevel.Debug, "Sending handshake request.");

            await this._sendMessage(this._handshakeProtocol.writeHandshakeRequest(handshakeRequest));

            this._logger.log(LogLevel.Information, `Using HubProtocol '${this._protocol.name}'.`);

            // defensively cleanup timeout in case we receive a message from the server before we finish start
            this._cleanupTimeout();
            this._resetTimeoutPeriod();
            this._resetKeepAliveInterval();

            await handshakePromise;

            // It's important to check the stopDuringStartError instead of just relying on the handshakePromise
            // being rejected on close, because this continuation can run after both the handshake completed successfully
            // and the connection was closed.
            if (this._stopDuringStartError) {
                // It's important to throw instead of returning a rejected promise, because we don't want to allow any state
                // transitions to occur between now and the calling code observing the exceptions. Returning a rejected promise
                // will cause the calling continuation to get scheduled to run later.
                // eslint-disable-next-line @typescript-eslint/no-throw-literal
                throw this._stopDuringStartError;
            }

            const useStatefulReconnect = this.connection.features.reconnect || false;
            if (useStatefulReconnect) {
                this._messageBuffer = new MessageBuffer(this._protocol, this.connection, this._statefulReconnectBufferSize);
                this.connection.features.disconnected = this._messageBuffer._disconnected.bind(this._messageBuffer);
                this.connection.features.resend = () => {
                    if (this._messageBuffer) {
                        return this._messageBuffer._resend();
                    }
                }
            }

            if (!this.connection.features.inherentKeepAlive) {
                await this._sendMessage(this._cachedPingMessage);
            }
        } catch (e) {
            this._logger.log(LogLevel.Debug, `Hub handshake failed with error '${e}' during start(). Stopping HubConnection.`);

            this._cleanupTimeout();
            this._cleanupPingTimer();

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
        const startPromise = this._startPromise;
        this.connection.features.reconnect = false;

        this._stopPromise = this._stopInternal();
        await this._stopPromise;

        try {
            // Awaiting undefined continues immediately
            await startPromise;
        } catch (e) {
            // This exception is returned to the user as a rejected Promise from the start method.
        }
    }

    private _stopInternal(error?: Error): Promise<void> {
        if (this._connectionState === HubConnectionState.Disconnected) {
            this._logger.log(LogLevel.Debug, `Call to HubConnection.stop(${error}) ignored because it is already in the disconnected state.`);
            return Promise.resolve();
        }

        if (this._connectionState === HubConnectionState.Disconnecting) {
            this._logger.log(LogLevel.Debug, `Call to HttpConnection.stop(${error}) ignored because the connection is already in the disconnecting state.`);
            return this._stopPromise!;
        }

        const state = this._connectionState;
        this._connectionState = HubConnectionState.Disconnecting;

        this._logger.log(LogLevel.Debug, "Stopping HubConnection.");

        if (this._reconnectDelayHandle) {
            // We're in a reconnect delay which means the underlying connection is currently already stopped.
            // Just clear the handle to stop the reconnect loop (which no one is waiting on thankfully) and
            // fire the onclose callbacks.
            this._logger.log(LogLevel.Debug, "Connection stopped during reconnect delay. Done reconnecting.");

            clearTimeout(this._reconnectDelayHandle);
            this._reconnectDelayHandle = undefined;

            this._completeClose();
            return Promise.resolve();
        }

        if (state === HubConnectionState.Connected) {
            // eslint-disable-next-line @typescript-eslint/no-floating-promises
            this._sendCloseMessage();
        }

        this._cleanupTimeout();
        this._cleanupPingTimer();
        this._stopDuringStartError = error || new AbortError("The connection was stopped before the hub handshake could complete.");

        // HttpConnection.stop() should not complete until after either HttpConnection.start() fails
        // or the onclose callback is invoked. The onclose callback will transition the HubConnection
        // to the disconnected state if need be before HttpConnection.stop() completes.
        return this.connection.stop(error);
    }

    private async _sendCloseMessage() {
        try {
            await this._sendWithProtocol(this._createCloseMessage());
        } catch {
            // Ignore, this is a best effort attempt to let the server know the client closed gracefully.
        }
    }

    /** Invokes a streaming hub method on the server using the specified name and arguments.
     *
     * @typeparam T The type of the items returned by the server.
     * @param {string} methodName The name of the server method to invoke.
     * @param {any[]} args The arguments used to invoke the server method.
     * @returns {IStreamResult<T>} An object that yields results from the server as they are received.
     */
    public stream<T = any>(methodName: string, ...args: any[]): IStreamResult<T> {
        const [streams, streamIds] = this._replaceStreamingParams(args);
        const invocationDescriptor = this._createStreamInvocation(methodName, args, streamIds);

        // eslint-disable-next-line prefer-const
        let promiseQueue: Promise<void>;

        const subject = new Subject<T>();
        subject.cancelCallback = () => {
            const cancelInvocation: CancelInvocationMessage = this._createCancelInvocation(invocationDescriptor.invocationId);

            delete this._callbacks[invocationDescriptor.invocationId];

            return promiseQueue.then(() => {
                return this._sendWithProtocol(cancelInvocation);
            });
        };

        this._callbacks[invocationDescriptor.invocationId] = (invocationEvent: CompletionMessage | StreamItemMessage | null, error?: Error) => {
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

        promiseQueue = this._sendWithProtocol(invocationDescriptor)
            .catch((e) => {
                subject.error(e);
                delete this._callbacks[invocationDescriptor.invocationId];
            });

        this._launchStreams(streams, promiseQueue);

        return subject;
    }

    private _sendMessage(message: any) {
        this._resetKeepAliveInterval();
        return this.connection.send(message);
    }

    /**
     * Sends a js object to the server.
     * @param message The js object to serialize and send.
     */
    private _sendWithProtocol(message: any) {
        if (this._messageBuffer) {
            return this._messageBuffer._send(message);
        } else {
            return this._sendMessage(this._protocol.writeMessage(message));
        }
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
        const [streams, streamIds] = this._replaceStreamingParams(args);
        const sendPromise = this._sendWithProtocol(this._createInvocation(methodName, args, true, streamIds));

        this._launchStreams(streams, sendPromise);

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
        const [streams, streamIds] = this._replaceStreamingParams(args);
        const invocationDescriptor = this._createInvocation(methodName, args, false, streamIds);

        const p = new Promise<any>((resolve, reject) => {
            // invocationId will always have a value for a non-blocking invocation
            this._callbacks[invocationDescriptor.invocationId!] = (invocationEvent: StreamItemMessage | CompletionMessage | null, error?: Error) => {
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

            const promiseQueue = this._sendWithProtocol(invocationDescriptor)
                .catch((e) => {
                    reject(e);
                    // invocationId will always have a value for a non-blocking invocation
                    delete this._callbacks[invocationDescriptor.invocationId!];
                });

            this._launchStreams(streams, promiseQueue);
        });

        return p;
    }

    /** Registers a handler that will be invoked when the hub method with the specified method name is invoked.
     *
     * @param {string} methodName The name of the hub method to define.
     * @param {Function} newMethod The handler that will be raised when the hub method is invoked.
     */
    public on(methodName: string, newMethod: (...args: any[]) => any): void
    public on(methodName: string, newMethod: (...args: any[]) => void): void {
        if (!methodName || !newMethod) {
            return;
        }

        methodName = methodName.toLowerCase();
        if (!this._methods[methodName]) {
            this._methods[methodName] = [];
        }

        // Preventing adding the same handler multiple times.
        if (this._methods[methodName].indexOf(newMethod) !== -1) {
            return;
        }

        this._methods[methodName].push(newMethod);
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
        const handlers = this._methods[methodName];
        if (!handlers) {
            return;
        }
        if (method) {
            const removeIdx = handlers.indexOf(method);
            if (removeIdx !== -1) {
                handlers.splice(removeIdx, 1);
                if (handlers.length === 0) {
                    delete this._methods[methodName];
                }
            }
        } else {
            delete this._methods[methodName];
        }

    }

    /** Registers a handler that will be invoked when the connection is closed.
     *
     * @param {Function} callback The handler that will be invoked when the connection is closed. Optionally receives a single argument containing the error that caused the connection to close (if any).
     */
    public onclose(callback: (error?: Error) => void): void {
        if (callback) {
            this._closedCallbacks.push(callback);
        }
    }

    /** Registers a handler that will be invoked when the connection starts reconnecting.
     *
     * @param {Function} callback The handler that will be invoked when the connection starts reconnecting. Optionally receives a single argument containing the error that caused the connection to start reconnecting (if any).
     */
    public onreconnecting(callback: (error?: Error) => void): void {
        if (callback) {
            this._reconnectingCallbacks.push(callback);
        }
    }

    /** Registers a handler that will be invoked when the connection successfully reconnects.
     *
     * @param {Function} callback The handler that will be invoked when the connection successfully reconnects.
     */
    public onreconnected(callback: (connectionId?: string) => void): void {
        if (callback) {
            this._reconnectedCallbacks.push(callback);
        }
    }

    private _processIncomingData(data: any) {
        this._cleanupTimeout();

        if (!this._receivedHandshakeResponse) {
            data = this._processHandshakeResponse(data);
            this._receivedHandshakeResponse = true;
        }

        // Data may have all been read when processing handshake response
        if (data) {
            // Parse the messages
            const messages = this._protocol.parseMessages(data, this._logger);

            for (const message of messages) {
                if (this._messageBuffer && !this._messageBuffer._shouldProcessMessage(message)) {
                    // Don't process the message, we are either waiting for a SequenceMessage or received a duplicate message
                    continue;
                }

                switch (message.type) {
                    case MessageType.Invocation:
                        // eslint-disable-next-line @typescript-eslint/no-floating-promises
                        this._invokeClientMethod(message);
                        break;
                    case MessageType.StreamItem:
                    case MessageType.Completion: {
                        const callback = this._callbacks[message.invocationId];
                        if (callback) {
                            if (message.type === MessageType.Completion) {
                                delete this._callbacks[message.invocationId];
                            }
                            try {
                                callback(message);
                            } catch (e) {
                                this._logger.log(LogLevel.Error, `Stream callback threw error: ${getErrorString(e)}`);
                            }
                        }
                        break;
                    }
                    case MessageType.Ping:
                        // Don't care about pings
                        break;
                    case MessageType.Close: {
                        this._logger.log(LogLevel.Information, "Close message received from server.");

                        const error = message.error ? new Error("Server returned an error on close: " + message.error) : undefined;

                        if (message.allowReconnect === true) {
                            // It feels wrong not to await connection.stop() here, but processIncomingData is called as part of an onreceive callback which is not async,
                            // this is already the behavior for serverTimeout(), and HttpConnection.Stop() should catch and log all possible exceptions.

                            // eslint-disable-next-line @typescript-eslint/no-floating-promises
                            this.connection.stop(error);
                        } else {
                            // We cannot await stopInternal() here, but subsequent calls to stop() will await this if stopInternal() is still ongoing.
                            this._stopPromise = this._stopInternal(error);
                        }

                        break;
                    }
                    case MessageType.Ack:
                        if (this._messageBuffer) {
                            this._messageBuffer._ack(message);
                        }
                        break;
                    case MessageType.Sequence:
                        if (this._messageBuffer) {
                            this._messageBuffer._resetSequence(message);
                        }
                        break;
                    default:
                        this._logger.log(LogLevel.Warning, `Invalid message type: ${message.type}.`);
                        break;
                }
            }
        }

        this._resetTimeoutPeriod();
    }

    private _processHandshakeResponse(data: any): any {
        let responseMessage: HandshakeResponseMessage;
        let remainingData: any;

        try {
            [remainingData, responseMessage] = this._handshakeProtocol.parseHandshakeResponse(data);
        } catch (e) {
            const message = "Error parsing handshake response: " + e;
            this._logger.log(LogLevel.Error, message);

            const error = new Error(message);
            this._handshakeRejecter(error);
            throw error;
        }
        if (responseMessage.error) {
            const message = "Server returned handshake error: " + responseMessage.error;
            this._logger.log(LogLevel.Error, message);

            const error = new Error(message);
            this._handshakeRejecter(error);
            throw error;
        } else {
            this._logger.log(LogLevel.Debug, "Server handshake complete.");
        }

        this._handshakeResolver();
        return remainingData;
    }

    private _resetKeepAliveInterval() {
        if (this.connection.features.inherentKeepAlive) {
            return;
        }

        // Set the time we want the next keep alive to be sent
        // Timer will be setup on next message receive
        this._nextKeepAlive = new Date().getTime() + this.keepAliveIntervalInMilliseconds;

        this._cleanupPingTimer();
    }

    private _resetTimeoutPeriod() {
        if (!this.connection.features || !this.connection.features.inherentKeepAlive) {
            // Set the timeout timer
            this._timeoutHandle = setTimeout(() => this.serverTimeout(), this.serverTimeoutInMilliseconds);

            // Set keepAlive timer if there isn't one
            if (this._pingServerHandle === undefined)
            {
                let nextPing = this._nextKeepAlive - new Date().getTime();
                if (nextPing < 0) {
                    nextPing = 0;
                }

                // The timer needs to be set from a networking callback to avoid Chrome timer throttling from causing timers to run once a minute
                this._pingServerHandle = setTimeout(async () => {
                    if (this._connectionState === HubConnectionState.Connected) {
                        try {
                            await this._sendMessage(this._cachedPingMessage);
                        } catch {
                            // We don't care about the error. It should be seen elsewhere in the client.
                            // The connection is probably in a bad or closed state now, cleanup the timer so it stops triggering
                            this._cleanupPingTimer();
                        }
                    }
                }, nextPing);
            }
        }
    }

    // eslint-disable-next-line @typescript-eslint/naming-convention
    private serverTimeout() {
        // The server hasn't talked to us in a while. It doesn't like us anymore ... :(
        // Terminate the connection, but we don't need to wait on the promise. This could trigger reconnecting.
        // eslint-disable-next-line @typescript-eslint/no-floating-promises
        this.connection.stop(new Error("Server timeout elapsed without receiving a message from the server."));
    }

    private async _invokeClientMethod(invocationMessage: InvocationMessage) {
        const methodName = invocationMessage.target.toLowerCase();
        const methods = this._methods[methodName];
        if (!methods) {
            this._logger.log(LogLevel.Warning, `No client method with the name '${methodName}' found.`);

            // No handlers provided by client but the server is expecting a response still, so we send an error
            if (invocationMessage.invocationId) {
                this._logger.log(LogLevel.Warning, `No result given for '${methodName}' method and invocation ID '${invocationMessage.invocationId}'.`);
                await this._sendWithProtocol(this._createCompletionMessage(invocationMessage.invocationId, "Client didn't provide a result.", null));
            }
            return;
        }

        // Avoid issues with handlers removing themselves thus modifying the list while iterating through it
        const methodsCopy = methods.slice();

        // Server expects a response
        const expectsResponse = invocationMessage.invocationId ? true : false;
        // We preserve the last result or exception but still call all handlers
        let res;
        let exception;
        let completionMessage;
        for (const m of methodsCopy) {
            try {
                const prevRes = res;
                res = await m.apply(this, invocationMessage.arguments);
                if (expectsResponse && res && prevRes) {
                    this._logger.log(LogLevel.Error, `Multiple results provided for '${methodName}'. Sending error to server.`);
                    completionMessage = this._createCompletionMessage(invocationMessage.invocationId!, `Client provided multiple results.`, null);
                }
                // Ignore exception if we got a result after, the exception will be logged
                exception = undefined;
            } catch (e) {
                exception = e;
                this._logger.log(LogLevel.Error, `A callback for the method '${methodName}' threw error '${e}'.`);
            }
        }
        if (completionMessage) {
            await this._sendWithProtocol(completionMessage);
        } else if (expectsResponse) {
            // If there is an exception that means either no result was given or a handler after a result threw
            if (exception) {
                completionMessage = this._createCompletionMessage(invocationMessage.invocationId!, `${exception}`, null);
            } else if (res !== undefined) {
                completionMessage = this._createCompletionMessage(invocationMessage.invocationId!, null, res);
            } else {
                this._logger.log(LogLevel.Warning, `No result given for '${methodName}' method and invocation ID '${invocationMessage.invocationId}'.`);
                // Client didn't provide a result or throw from a handler, server expects a response so we send an error
                completionMessage = this._createCompletionMessage(invocationMessage.invocationId!, "Client didn't provide a result.", null);
            }
            await this._sendWithProtocol(completionMessage);
        } else {
            if (res) {
                this._logger.log(LogLevel.Error, `Result given for '${methodName}' method but server is not expecting a result.`);
            }
        }
    }

    private _connectionClosed(error?: Error) {
        this._logger.log(LogLevel.Debug, `HubConnection.connectionClosed(${error}) called while in state ${this._connectionState}.`);

        // Triggering this.handshakeRejecter is insufficient because it could already be resolved without the continuation having run yet.
        this._stopDuringStartError = this._stopDuringStartError || error || new AbortError("The underlying connection was closed before the hub handshake could complete.");

        // If the handshake is in progress, start will be waiting for the handshake promise, so we complete it.
        // If it has already completed, this should just noop.
        if (this._handshakeResolver) {
            this._handshakeResolver();
        }

        this._cancelCallbacksWithError(error || new Error("Invocation canceled due to the underlying connection being closed."));

        this._cleanupTimeout();
        this._cleanupPingTimer();

        if (this._connectionState === HubConnectionState.Disconnecting) {
            this._completeClose(error);
        } else if (this._connectionState === HubConnectionState.Connected && this._reconnectPolicy) {
            // eslint-disable-next-line @typescript-eslint/no-floating-promises
            this._reconnect(error);
        } else if (this._connectionState === HubConnectionState.Connected) {
            this._completeClose(error);
        }

        // If none of the above if conditions were true were called the HubConnection must be in either:
        // 1. The Connecting state in which case the handshakeResolver will complete it and stopDuringStartError will fail it.
        // 2. The Reconnecting state in which case the handshakeResolver will complete it and stopDuringStartError will fail the current reconnect attempt
        //    and potentially continue the reconnect() loop.
        // 3. The Disconnected state in which case we're already done.
    }

    private _completeClose(error?: Error) {
        if (this._connectionStarted) {
            this._connectionState = HubConnectionState.Disconnected;
            this._connectionStarted = false;
            if (this._messageBuffer) {
                this._messageBuffer._dispose(error ?? new Error("Connection closed."));
                this._messageBuffer = undefined;
            }

            if (Platform.isBrowser) {
                window.document.removeEventListener("freeze", this._freezeEventListener);
            }

            try {
                this._closedCallbacks.forEach((c) => c.apply(this, [error]));
            } catch (e) {
                this._logger.log(LogLevel.Error, `An onclose callback called with error '${error}' threw error '${e}'.`);
            }
        }
    }

    private async _reconnect(error?: Error) {
        const reconnectStartTime = Date.now();
        let previousReconnectAttempts = 0;
        let retryError = error !== undefined ? error : new Error("Attempting to reconnect due to a unknown error.");

        let nextRetryDelay = this._getNextRetryDelay(previousReconnectAttempts++, 0, retryError);

        if (nextRetryDelay === null) {
            this._logger.log(LogLevel.Debug, "Connection not reconnecting because the IRetryPolicy returned null on the first reconnect attempt.");
            this._completeClose(error);
            return;
        }

        this._connectionState = HubConnectionState.Reconnecting;

        if (error) {
            this._logger.log(LogLevel.Information, `Connection reconnecting because of error '${error}'.`);
        } else {
            this._logger.log(LogLevel.Information, "Connection reconnecting.");
        }

        if (this._reconnectingCallbacks.length !== 0) {
            try {
                this._reconnectingCallbacks.forEach((c) => c.apply(this, [error]));
            } catch (e) {
                this._logger.log(LogLevel.Error, `An onreconnecting callback called with error '${error}' threw error '${e}'.`);
            }

            // Exit early if an onreconnecting callback called connection.stop().
            if (this._connectionState !== HubConnectionState.Reconnecting) {
                this._logger.log(LogLevel.Debug, "Connection left the reconnecting state in onreconnecting callback. Done reconnecting.");
                return;
            }
        }

        while (nextRetryDelay !== null) {
            this._logger.log(LogLevel.Information, `Reconnect attempt number ${previousReconnectAttempts} will start in ${nextRetryDelay} ms.`);

            await new Promise((resolve) => {
                this._reconnectDelayHandle = setTimeout(resolve, nextRetryDelay!);
            });
            this._reconnectDelayHandle = undefined;

            if (this._connectionState !== HubConnectionState.Reconnecting) {
                this._logger.log(LogLevel.Debug, "Connection left the reconnecting state during reconnect delay. Done reconnecting.");
                return;
            }

            try {
                await this._startInternal();

                this._connectionState = HubConnectionState.Connected;
                this._logger.log(LogLevel.Information, "HubConnection reconnected successfully.");

                if (this._reconnectedCallbacks.length !== 0) {
                    try {
                        this._reconnectedCallbacks.forEach((c) => c.apply(this, [this.connection.connectionId]));
                    } catch (e) {
                        this._logger.log(LogLevel.Error, `An onreconnected callback called with connectionId '${this.connection.connectionId}; threw error '${e}'.`);
                    }
                }

                return;
            } catch (e) {
                this._logger.log(LogLevel.Information, `Reconnect attempt failed because of error '${e}'.`);

                if (this._connectionState !== HubConnectionState.Reconnecting) {
                    this._logger.log(LogLevel.Debug, `Connection moved to the '${this._connectionState}' from the reconnecting state during reconnect attempt. Done reconnecting.`);
                    // The TypeScript compiler thinks that connectionState must be Connected here. The TypeScript compiler is wrong.
                    if (this._connectionState as any === HubConnectionState.Disconnecting) {
                        this._completeClose();
                    }
                    return;
                }

                retryError = e instanceof Error ? e : new Error((e as any).toString());
                nextRetryDelay = this._getNextRetryDelay(previousReconnectAttempts++, Date.now() - reconnectStartTime, retryError);
            }
        }

        this._logger.log(LogLevel.Information, `Reconnect retries have been exhausted after ${Date.now() - reconnectStartTime} ms and ${previousReconnectAttempts} failed attempts. Connection disconnecting.`);

        this._completeClose();
    }

    private _getNextRetryDelay(previousRetryCount: number, elapsedMilliseconds: number, retryReason: Error) {
        try {
            return this._reconnectPolicy!.nextRetryDelayInMilliseconds({
                elapsedMilliseconds,
                previousRetryCount,
                retryReason,
            });
        } catch (e) {
            this._logger.log(LogLevel.Error, `IRetryPolicy.nextRetryDelayInMilliseconds(${previousRetryCount}, ${elapsedMilliseconds}) threw error '${e}'.`);
            return null;
        }
    }

    private _cancelCallbacksWithError(error: Error) {
        const callbacks = this._callbacks;
        this._callbacks = {};

        Object.keys(callbacks)
            .forEach((key) => {
                const callback = callbacks[key];
                try {
                    callback(null, error);
                } catch (e) {
                    this._logger.log(LogLevel.Error, `Stream 'error' callback called with '${error}' threw error: ${getErrorString(e)}`);
                }
            });
    }

    private _cleanupPingTimer(): void {
        if (this._pingServerHandle) {
            clearTimeout(this._pingServerHandle);
            this._pingServerHandle = undefined;
        }
    }

    private _cleanupTimeout(): void {
        if (this._timeoutHandle) {
            clearTimeout(this._timeoutHandle);
        }
    }

    private _createInvocation(methodName: string, args: any[], nonblocking: boolean, streamIds: string[]): InvocationMessage {
        if (nonblocking) {
            if (streamIds.length !== 0) {
                return {
                    arguments: args,
                    streamIds,
                    target: methodName,
                    type: MessageType.Invocation,
                };
            } else {
                return {
                    arguments: args,
                    target: methodName,
                    type: MessageType.Invocation,
                };
            }
        } else {
            const invocationId = this._invocationId;
            this._invocationId++;

            if (streamIds.length !== 0) {
                return {
                    arguments: args,
                    invocationId: invocationId.toString(),
                    streamIds,
                    target: methodName,
                    type: MessageType.Invocation,
                };
            } else {
                return {
                    arguments: args,
                    invocationId: invocationId.toString(),
                    target: methodName,
                    type: MessageType.Invocation,
                };
            }
        }
    }

    private _launchStreams(streams: IStreamResult<any>[], promiseQueue: Promise<void>): void {
        if (streams.length === 0) {
            return;
        }

        // Synchronize stream data so they arrive in-order on the server
        if (!promiseQueue) {
            promiseQueue = Promise.resolve();
        }

        // We want to iterate over the keys, since the keys are the stream ids
        // eslint-disable-next-line guard-for-in
        for (const streamId in streams) {
            streams[streamId].subscribe({
                complete: () => {
                    promiseQueue = promiseQueue.then(() => this._sendWithProtocol(this._createCompletionMessage(streamId)));
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

                    promiseQueue = promiseQueue.then(() => this._sendWithProtocol(this._createCompletionMessage(streamId, message)));
                },
                next: (item) => {
                    promiseQueue = promiseQueue.then(() => this._sendWithProtocol(this._createStreamItemMessage(streamId, item)));
                },
            });
        }
    }

    private _replaceStreamingParams(args: any[]): [IStreamResult<any>[], string[]] {
        const streams: IStreamResult<any>[] = [];
        const streamIds: string[] = [];
        for (let i = 0; i < args.length; i++) {
            const argument = args[i];
            if (this._isObservable(argument)) {
                const streamId = this._invocationId;
                this._invocationId++;
                // Store the stream for later use
                streams[streamId] = argument;
                streamIds.push(streamId.toString());

                // remove stream from args
                args.splice(i, 1);
            }
        }

        return [streams, streamIds];
    }

    private _isObservable(arg: any): arg is IStreamResult<any> {
        // This allows other stream implementations to just work (like rxjs)
        return arg && arg.subscribe && typeof arg.subscribe === "function";
    }

    private _createStreamInvocation(methodName: string, args: any[], streamIds: string[]): StreamInvocationMessage {
        const invocationId = this._invocationId;
        this._invocationId++;

        if (streamIds.length !== 0) {
            return {
                arguments: args,
                invocationId: invocationId.toString(),
                streamIds,
                target: methodName,
                type: MessageType.StreamInvocation,
            };
        } else {
            return {
                arguments: args,
                invocationId: invocationId.toString(),
                target: methodName,
                type: MessageType.StreamInvocation,
            };
        }
    }

    private _createCancelInvocation(id: string): CancelInvocationMessage {
        return {
            invocationId: id,
            type: MessageType.CancelInvocation,
        };
    }

    private _createStreamItemMessage(id: string, item: any): StreamItemMessage {
        return {
            invocationId: id,
            item,
            type: MessageType.StreamItem,
        };
    }

    private _createCompletionMessage(id: string, error?: any, result?: any): CompletionMessage {
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

    private _createCloseMessage(): CloseMessage {
        return { type: MessageType.Close };
    }
}
