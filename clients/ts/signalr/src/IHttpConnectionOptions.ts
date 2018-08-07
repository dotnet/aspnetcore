// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

import { HttpClient } from "./HttpClient";
import { ILogger, LogLevel } from "./ILogger";
import { HttpTransportType, ITransport } from "./ITransport";
import { EventSourceConstructor, WebSocketConstructor } from "./Polyfills";

/** Options provided to the 'withUrl' method on {@link @aspnet/signalr.HubConnectionBuilder} to configure options for the HTTP-based transports. */
export interface IHttpConnectionOptions {
    /** An {@link @aspnet/signalr.HttpClient} that will be used to make HTTP requests. */
    httpClient?: HttpClient;

    /** An {@link @aspnet/signalr.HttpTransportType} value specifying the transport to use for the connection. */
    transport?: HttpTransportType | ITransport;

    /** Configures the logger used for logging.
     *
     * Provide an {@link @aspnet/signalr.ILogger} instance, and log messages will be logged via that instance. Alternatively, provide a value from
     * the {@link @aspnet/signalr.LogLevel} enumeration and a default logger which logs to the Console will be configured to log messages of the specified
     * level (or higher).
     */
    logger?: ILogger | LogLevel;

    /** A function that provides an access token required for HTTP Bearer authentication.
     *
     * @returns {string | Promise<string>} A string containing the access token, or a Promise that resolves to a string containing the access token.
     */
    accessTokenFactory?(): string | Promise<string>;

    /** A boolean indicating if message content should be logged.
     *
     * Message content can contain sensitive user data, so this is disabled by default.
     */
    logMessageContent?: boolean;

    /** A boolean indicating if negotiation should be skipped.
     *
     * Negotiation can only be skipped when the {@link @aspnet/signalr.IHttpConnectionOptions.transport} property is set to 'HttpTransportType.WebSockets'.
     */
    skipNegotiation?: boolean;

    // Used for unit testing and code spelunkers
    /** A constructor that can be used to create a WebSocket.
     *
     * @internal
     */
    WebSocket?: WebSocketConstructor;

    // Used for unit testing and code spelunkers
    /** A constructor that can be used to create an EventSource.
     *
     * @internal
     */
    EventSource?: EventSourceConstructor;
}
