// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

import { HttpConnection } from "./HttpConnection";
import { HubConnection } from "./HubConnection";
import { IHttpConnectionOptions } from "./IHttpConnectionOptions";
import { IHubProtocol } from "./IHubProtocol";
import { ILogger, LogLevel } from "./ILogger";
import { HttpTransportType } from "./ITransport";
import { JsonHubProtocol } from "./JsonHubProtocol";
import { NullLogger } from "./Loggers";
import { Arg, ConsoleLogger } from "./Utils";

/** A builder for configuring {@link @aspnet/signalr.HubConnection} instances. */
export class HubConnectionBuilder {
    /** @internal */
    public protocol?: IHubProtocol;
    /** @internal */
    public httpConnectionOptions?: IHttpConnectionOptions;
    /** @internal */
    public url?: string;
    /** @internal */
    public logger?: ILogger;

    /** Configures console logging for the {@link @aspnet/signalr.HubConnection}.
     *
     * @param {LogLevel} logLevel The minimum level of messages to log. Anything at this level, or a more severe level, will be logged.
     * @returns The {@link @aspnet/signalr.HubConnectionBuilder} instance, for chaining.
     */
    public configureLogging(logLevel: LogLevel): HubConnectionBuilder;

    /** Configures custom logging for the {@link @aspnet/signalr.HubConnection}.
     *
     * @param {ILogger} logger An object implementing the {@link @aspnet/signalr.ILogger} interface, which will be used to write all log messages.
     * @returns The {@link @aspnet/signalr.HubConnectionBuilder} instance, for chaining.
     */
    public configureLogging(logger: ILogger): HubConnectionBuilder;
    /** Configures custom logging for the {@link @aspnet/signalr.HubConnection}.
     *
     * @param {LogLevel | ILogger} logging An object implementing the {@link @aspnet/signalr.ILogger} interface or {@link @aspnet/signalr.LogLevel}.
     * @returns The {@link @aspnet/signalr.HubConnectionBuilder} instance, for chaining.
     */
    public configureLogging(logging: LogLevel | ILogger): HubConnectionBuilder;
    public configureLogging(logging: LogLevel | ILogger): HubConnectionBuilder {
        Arg.isRequired(logging, "logging");

        if (isLogger(logging)) {
            this.logger = logging;
        } else {
            this.logger = new ConsoleLogger(logging);
        }

        return this;
    }

    /** Configures the {@link @aspnet/signalr.HubConnection} to use HTTP-based transports to connect to the specified URL.
     *
     * The transport will be selected automatically based on what the server and client support.
     *
     * @param {string} url The URL the connection will use.
     * @returns The {@link @aspnet/signalr.HubConnectionBuilder} instance, for chaining.
     */
    public withUrl(url: string): HubConnectionBuilder;

    /** Configures the {@link @aspnet/signalr.HubConnection} to use the specified HTTP-based transport to connect to the specified URL.
     *
     * @param {string} url The URL the connection will use.
     * @param {HttpTransportType} transportType The specific transport to use.
     * @returns The {@link @aspnet/signalr.HubConnectionBuilder} instance, for chaining.
     */
    public withUrl(url: string, transportType: HttpTransportType): HubConnectionBuilder;

    /** Configures the {@link @aspnet/signalr.HubConnection} to use HTTP-based transports to connect to the specified URL.
     *
     * @param {string} url The URL the connection will use.
     * @param {IHttpConnectionOptions} options An options object used to configure the connection.
     * @returns The {@link @aspnet/signalr.HubConnectionBuilder} instance, for chaining.
     */
    public withUrl(url: string, options: IHttpConnectionOptions): HubConnectionBuilder;
    public withUrl(url: string, transportTypeOrOptions?: IHttpConnectionOptions | HttpTransportType): HubConnectionBuilder {
        Arg.isRequired(url, "url");

        this.url = url;

        // Flow-typing knows where it's at. Since HttpTransportType is a number and IHttpConnectionOptions is guaranteed
        // to be an object, we know (as does TypeScript) this comparison is all we need to figure out which overload was called.
        if (typeof transportTypeOrOptions === "object") {
            this.httpConnectionOptions = transportTypeOrOptions;
        } else {
            this.httpConnectionOptions = {
                transport: transportTypeOrOptions,
            };
        }

        return this;
    }

    /** Configures the {@link @aspnet/signalr.HubConnection} to use the specified Hub Protocol.
     *
     * @param {IHubProtocol} protocol The {@link @aspnet/signalr.IHubProtocol} implementation to use.
     */
    public withHubProtocol(protocol: IHubProtocol): HubConnectionBuilder {
        Arg.isRequired(protocol, "protocol");

        this.protocol = protocol;
        return this;
    }

    /** Creates a {@link @aspnet/signalr.HubConnection} from the configuration options specified in this builder.
     *
     * @returns {HubConnection} The configured {@link @aspnet/signalr.HubConnection}.
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
            this.protocol || new JsonHubProtocol());
    }
}

function isLogger(logger: any): logger is ILogger {
    return logger.log !== undefined;
}
