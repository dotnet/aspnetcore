import { IStreamResult } from "./Stream";
/** Describes the current state of the {@link HubConnection} to the server. */
export declare enum HubConnectionState {
    /** The hub connection is disconnected. */
    Disconnected = "Disconnected",
    /** The hub connection is connecting. */
    Connecting = "Connecting",
    /** The hub connection is connected. */
    Connected = "Connected",
    /** The hub connection is disconnecting. */
    Disconnecting = "Disconnecting",
    /** The hub connection is reconnecting. */
    Reconnecting = "Reconnecting"
}
/** Represents a connection to a SignalR Hub. */
export declare class HubConnection {
    private readonly _cachedPingMessage;
    private readonly connection;
    private readonly _logger;
    private readonly _reconnectPolicy?;
    private _protocol;
    private _handshakeProtocol;
    private _callbacks;
    private _methods;
    private _invocationId;
    private _closedCallbacks;
    private _reconnectingCallbacks;
    private _reconnectedCallbacks;
    private _receivedHandshakeResponse;
    private _handshakeResolver;
    private _handshakeRejecter;
    private _stopDuringStartError?;
    private _connectionState;
    private _connectionStarted;
    private _startPromise?;
    private _stopPromise?;
    private _nextKeepAlive;
    private _reconnectDelayHandle?;
    private _timeoutHandle?;
    private _pingServerHandle?;
    private _freezeEventListener;
    /** The server timeout in milliseconds.
     *
     * If this timeout elapses without receiving any messages from the server, the connection will be terminated with an error.
     * The default timeout value is 30,000 milliseconds (30 seconds).
     */
    serverTimeoutInMilliseconds: number;
    /** Default interval at which to ping the server.
     *
     * The default value is 15,000 milliseconds (15 seconds).
     * Allows the server to detect hard disconnects (like when a client unplugs their computer).
     * The ping will happen at most as often as the server pings.
     * If the server pings every 5 seconds, a value lower than 5 will ping every 5 seconds.
     */
    keepAliveIntervalInMilliseconds: number;
    private constructor();
    /** Indicates the state of the {@link HubConnection} to the server. */
    get state(): HubConnectionState;
    /** Represents the connection id of the {@link HubConnection} on the server. The connection id will be null when the connection is either
     *  in the disconnected state or if the negotiation step was skipped.
     */
    get connectionId(): string | null;
    /** Indicates the url of the {@link HubConnection} to the server. */
    get baseUrl(): string;
    /**
     * Sets a new url for the HubConnection. Note that the url can only be changed when the connection is in either the Disconnected or
     * Reconnecting states.
     * @param {string} url The url to connect to.
     */
    set baseUrl(url: string);
    /** Starts the connection.
     *
     * @returns {Promise<void>} A Promise that resolves when the connection has been successfully established, or rejects with an error.
     */
    start(): Promise<void>;
    private _startWithStateTransitions;
    private _startInternal;
    /** Stops the connection.
     *
     * @returns {Promise<void>} A Promise that resolves when the connection has been successfully terminated, or rejects with an error.
     */
    stop(): Promise<void>;
    private _stopInternal;
    /** Invokes a streaming hub method on the server using the specified name and arguments.
     *
     * @typeparam T The type of the items returned by the server.
     * @param {string} methodName The name of the server method to invoke.
     * @param {any[]} args The arguments used to invoke the server method.
     * @returns {IStreamResult<T>} An object that yields results from the server as they are received.
     */
    stream<T = any>(methodName: string, ...args: any[]): IStreamResult<T>;
    private _sendMessage;
    /**
     * Sends a js object to the server.
     * @param message The js object to serialize and send.
     */
    private _sendWithProtocol;
    /** Invokes a hub method on the server using the specified name and arguments. Does not wait for a response from the receiver.
     *
     * The Promise returned by this method resolves when the client has sent the invocation to the server. The server may still
     * be processing the invocation.
     *
     * @param {string} methodName The name of the server method to invoke.
     * @param {any[]} args The arguments used to invoke the server method.
     * @returns {Promise<void>} A Promise that resolves when the invocation has been successfully sent, or rejects with an error.
     */
    send(methodName: string, ...args: any[]): Promise<void>;
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
    invoke<T = any>(methodName: string, ...args: any[]): Promise<T>;
    /** Registers a handler that will be invoked when the hub method with the specified method name is invoked.
     *
     * @param {string} methodName The name of the hub method to define.
     * @param {Function} newMethod The handler that will be raised when the hub method is invoked.
     */
    on(methodName: string, newMethod: (...args: any[]) => any): void;
    /** Removes all handlers for the specified hub method.
     *
     * @param {string} methodName The name of the method to remove handlers for.
     */
    off(methodName: string): void;
    /** Removes the specified handler for the specified hub method.
     *
     * You must pass the exact same Function instance as was previously passed to {@link @microsoft/signalr.HubConnection.on}. Passing a different instance (even if the function
     * body is the same) will not remove the handler.
     *
     * @param {string} methodName The name of the method to remove handlers for.
     * @param {Function} method The handler to remove. This must be the same Function instance as the one passed to {@link @microsoft/signalr.HubConnection.on}.
     */
    off(methodName: string, method: (...args: any[]) => void): void;
    /** Registers a handler that will be invoked when the connection is closed.
     *
     * @param {Function} callback The handler that will be invoked when the connection is closed. Optionally receives a single argument containing the error that caused the connection to close (if any).
     */
    onclose(callback: (error?: Error) => void): void;
    /** Registers a handler that will be invoked when the connection starts reconnecting.
     *
     * @param {Function} callback The handler that will be invoked when the connection starts reconnecting. Optionally receives a single argument containing the error that caused the connection to start reconnecting (if any).
     */
    onreconnecting(callback: (error?: Error) => void): void;
    /** Registers a handler that will be invoked when the connection successfully reconnects.
     *
     * @param {Function} callback The handler that will be invoked when the connection successfully reconnects.
     */
    onreconnected(callback: (connectionId?: string) => void): void;
    private _processIncomingData;
    private _processHandshakeResponse;
    private _resetKeepAliveInterval;
    private _resetTimeoutPeriod;
    private serverTimeout;
    private _invokeClientMethod;
    private _connectionClosed;
    private _completeClose;
    private _reconnect;
    private _getNextRetryDelay;
    private _cancelCallbacksWithError;
    private _cleanupPingTimer;
    private _cleanupTimeout;
    private _createInvocation;
    private _launchStreams;
    private _replaceStreamingParams;
    private _isObservable;
    private _createStreamInvocation;
    private _createCancelInvocation;
    private _createStreamItemMessage;
    private _createCompletionMessage;
}
//# sourceMappingURL=HubConnection.d.ts.map