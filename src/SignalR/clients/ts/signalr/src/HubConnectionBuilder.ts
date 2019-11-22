// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

import { DefaultReconnectPolicy } from "./DefaultReconnectPolicy";
import { HttpConnection } from "./HttpConnection";
import { HubConnection } from "./HubConnection";
import { IHttpConnectionOptions } from "./IHttpConnectionOptions";
import { IHubProtocol } from "./IHubProtocol";
import { ILogger, LogLevel } from "./ILogger";
import { IRetryPolicy } from "./IRetryPolicy";
import { HttpTransportType } from "./ITransport";
import { JsonHubProtocol } from "./JsonHubProtocol";
import { NullLogger } from "./Loggers";
import { Arg, ConsoleLogger } from "./Utils";

// tslint:disable:object-literal-sort-keys
const LogLevelNameMapping = {
    trace: LogLevel.Trace,
    debug: LogLevel.Debug,
    info: LogLevel.Information,
    information: LogLevel.Information,
    warn: LogLevel.Warning,
    warning: LogLevel.Warning,
    error: LogLevel.Error,
    critical: LogLevel.Critical,
    none: LogLevel.None,
};

function parseLogLevel(name: string): LogLevel {
    // Case-insensitive matching via lower-casing
    // Yes, I know case-folding is a complicated problem in Unicode, but we only support
    // the ASCII strings defined in LogLevelNameMapping anyway, so it's fine -anurse.
    const mapping = LogLevelNameMapping[name.toLowerCase()];
    if (typeof mapping !== "undefined") {
        return mapping;
    } else {
        throw new Error(`Unknown log level: ${name}`);
    }
}

/** A builder for configuring {@link @microsoft/signalr.HubConnection} instances. */
export class HubConnectionBuilder {
    /** @internal */
    public protocol?: IHubProtocol;
    /** @internal */
    public httpConnectionOptions?: IHttpConnectionOptions;
    /** @internal */
    public url?: string;
    /** @internal */
    public logger?: ILogger;

    /** If defined, this indicates the client should automatically attempt to reconnect if the connection is lost. */
    /** @internal */
    public reconnectPolicy?: IRetryPolicy;

    /** Configures console logging for the {@link @microsoft/signalr.HubConnection}.
     *
     * @param {LogLevel} logLevel The minimum level of messages to log. Anything at this level, or a more severe level, will be logged.
     * @returns The {@link @microsoft/signalr.HubConnectionBuilder} instance, for chaining.
     */
    public configureLogging(logLevel: LogLevel): HubConnectionBuilder;

    /** Configures custom logging for the {@link @microsoft/signalr.HubConnection}.
     *
     * @param {ILogger} logger An object implementing the {@link @microsoft/signalr.ILogger} interface, which will be used to write all log messages.
     * @returns The {@link @microsoft/signalr.HubConnectionBuilder} instance, for chaining.
     */
    public configureLogging(logger: ILogger): HubConnectionBuilder;

    /** Configures custom logging for the {@link @microsoft/signalr.HubConnection}.
     *
     * @param {string} logLevel A string representing a LogLevel setting a minimum level of messages to log.
     *    See {@link https://docs.microsoft.com/en-us/aspnet/core/signalr/configuration#configure-logging|the documentation for client logging configuration} for more details.
     */
    public configureLogging(logLevel: string): HubConnectionBuilder;

    /** Configures custom logging for the {@link @microsoft/signalr.HubConnection}.
     *
     * @param {LogLevel | string | ILogger} logging A {@link @microsoft/signalr.LogLevel}, a string representing a LogLevel, or an object implementing the {@link @microsoft/signalr.ILogger} interface.
     *    See {@link https://docs.microsoft.com/en-us/aspnet/core/signalr/configuration#configure-logging|the documentation for client logging configuration} for more details.
     * @returns The {@link @microsoft/signalr.HubConnectionBuilder} instance, for chaining.
     */
    public configureLogging(logging: LogLevel | string | ILogger): HubConnectionBuilder;
    public configureLogging(logging: LogLevel | string | ILogger): HubConnectionBuilder {
        Arg.isRequired(logging, "logging");

        if (isLogger(logging)) {
            this.logger = logging;
        } else if (typeof logging === "string") {
            const logLevel = parseLogLevel(logging);
            this.logger = new ConsoleLogger(logLevel);
        } else {
            this.logger = new ConsoleLogger(logging);
        }

        return this;
    }

    /** Configures the {@link @microsoft/signalr.HubConnection} to use HTTP-based transports to connect to the specified URL.
     *
     * The transport will be selected automatically based on what the server and client support.
     *
     * @param {string} url The URL the connection will use.
     * @returns The {@link @microsoft/signalr.HubConnectionBuilder} instance, for chaining.
     */
    public withUrl(url: string): HubConnectionBuilder;

    /** Configures the {@link @microsoft/signalr.HubConnection} to use the specified HTTP-based transport to connect to the specified URL.
     *
     * @param {string} url The URL the connection will use.
     * @param {HttpTransportType} transportType The specific transport to use.
     * @returns The {@link @microsoft/signalr.HubConnectionBuilder} instance, for chaining.
     */
    public withUrl(url: string, transportType: HttpTransportType): HubConnectionBuilder;

    /** Configures the {@link @microsoft/signalr.HubConnection} to use HTTP-based transports to connect to the specified URL.
     *
     * @param {string} url The URL the connection will use.
     * @param {IHttpConnectionOptions} options An options object used to configure the connection.
     * @returns The {@link @microsoft/signalr.HubConnectionBuilder} instance, for chaining.
     */
    public withUrl(url: string, options: IHttpConnectionOptions): HubConnectionBuilder;
    public withUrl(url: string, transportTypeOrOptions?: IHttpConnectionOptions | HttpTransportType): HubConnectionBuilder {
        Arg.isRequired(url, "url");

        this.url = url;

        // Flow-typing knows where it's at. Since HttpTransportType is a number and IHttpConnectionOptions is guaranteed
        // to be an object, we know (as does TypeScript) this comparison is all we need to figure out which overload was called.
        if (typeof transportTypeOrOptions === "object") {
            this.httpConnectionOptions = { ...this.httpConnectionOptions, ...transportTypeOrOptions };
        } else {
            this.httpConnectionOptions = {
                ...this.httpConnectionOptions,
                transport: transportTypeOrOptions,
            };
        }

        return this;
    }

    /** Configures the {@link @microsoft/signalr.HubConnection} to use the specified Hub Protocol.
     *
     * @param {IHubProtocol} protocol The {@link @microsoft/signalr.IHubProtocol} implementation to use.
     */
    public withHubProtocol(protocol: IHubProtocol): HubConnectionBuilder {
        Arg.isRequired(protocol, "protocol");

        this.protocol = protocol;
        return this;
    }

    /** Configures the {@link @microsoft/signalr.HubConnection} to automatically attempt to reconnect if the connection is lost.
     * By default, the client will wait 0, 2, 10 and 30 seconds respectively before trying up to 4 reconnect attempts.
     */
    public withAutomaticReconnect(): HubConnectionBuilder;

    /** Configures the {@link @microsoft/signalr.HubConnection} to automatically attempt to reconnect if the connection is lost.
     *
     * @param {number[]} retryDelays An array containing the delays in milliseconds before trying each reconnect attempt.
     * The length of the array represents how many failed reconnect attempts it takes before the client will stop attempting to reconnect.
     */
    public withAutomaticReconnect(retryDelays: number[]): HubConnectionBuilder;

    /** Configures the {@link @microsoft/signalr.HubConnection} to automatically attempt to reconnect if the connection is lost.
     *
     * @param {IRetryPolicy} reconnectPolicy An {@link @microsoft/signalR.IRetryPolicy} that controls the timing and number of reconnect attempts.
     */
    public withAutomaticReconnect(reconnectPolicy: IRetryPolicy): HubConnectionBuilder;
    public withAutomaticReconnect(retryDelaysOrReconnectPolicy?: number[] | IRetryPolicy): HubConnectionBuilder {
        if (this.reconnectPolicy) {
            throw new Error("A reconnectPolicy has already been set.");
        }

        if (!retryDelaysOrReconnectPolicy) {
            this.reconnectPolicy = new DefaultReconnectPolicy();
        } else if (Array.isArray(retryDelaysOrReconnectPolicy)) {
            this.reconnectPolicy = new DefaultReconnectPolicy(retryDelaysOrReconnectPolicy);
        } else {
            this.reconnectPolicy = retryDelaysOrReconnectPolicy;
        }

        return this;
    }

    /** Creates a {@link @microsoft/signalr.HubConnection} from the configuration options specified in this builder.
     *
     * @returns {HubConnection} The configured {@link @microsoft/signalr.HubConnection}.
     */
    public build(): HubConnection {
        // If httpConnectionOptions has a logger, use it. Otherwise, override it with the one
        // provided to configureLogger
        const httpConnectionOptions = this.httpConnectionOptions || {};

        // If it's 'null', the user **explicitly** asked for null, don't mess with it.
        if (httpConnectionOptions.logger === undefined) {
            // If our logger is undefined or null, that's OK, the HttpConnection constructor will handle it.
            httpConnectionOptions.logger = this.logger;
        }

        // Now create the connection
        if (!this.url) {
            throw new Error("The 'HubConnectionBuilder.withUrl' method must be called before building the connection.");
        }
        const connection = new HttpConnection(this.url, httpConnectionOptions);

        return HubConnection.create(
            connection,
            this.logger || NullLogger.instance,
            this.protocol || new JsonHubProtocol(),
            this.reconnectPolicy);
    }
}

function isLogger(logger: any): logger is ILogger {
    return logger.log !== undefined;
}
