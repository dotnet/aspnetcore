import { HubConnection } from "./HubConnection";
import { IHttpConnectionOptions } from "./IHttpConnectionOptions";
import { IHubProtocol } from "./IHubProtocol";
import { ILogger, LogLevel } from "./ILogger";
import { IRetryPolicy } from "./IRetryPolicy";
import { HttpTransportType } from "./ITransport";
/** A builder for configuring {@link @microsoft/signalr.HubConnection} instances. */
export declare class HubConnectionBuilder {
    /** Configures console logging for the {@link @microsoft/signalr.HubConnection}.
     *
     * @param {LogLevel} logLevel The minimum level of messages to log. Anything at this level, or a more severe level, will be logged.
     * @returns The {@link @microsoft/signalr.HubConnectionBuilder} instance, for chaining.
     */
    configureLogging(logLevel: LogLevel): HubConnectionBuilder;
    /** Configures custom logging for the {@link @microsoft/signalr.HubConnection}.
     *
     * @param {ILogger} logger An object implementing the {@link @microsoft/signalr.ILogger} interface, which will be used to write all log messages.
     * @returns The {@link @microsoft/signalr.HubConnectionBuilder} instance, for chaining.
     */
    configureLogging(logger: ILogger): HubConnectionBuilder;
    /** Configures custom logging for the {@link @microsoft/signalr.HubConnection}.
     *
     * @param {string} logLevel A string representing a LogLevel setting a minimum level of messages to log.
     *    See {@link https://docs.microsoft.com/aspnet/core/signalr/configuration#configure-logging|the documentation for client logging configuration} for more details.
     */
    configureLogging(logLevel: string): HubConnectionBuilder;
    /** Configures custom logging for the {@link @microsoft/signalr.HubConnection}.
     *
     * @param {LogLevel | string | ILogger} logging A {@link @microsoft/signalr.LogLevel}, a string representing a LogLevel, or an object implementing the {@link @microsoft/signalr.ILogger} interface.
     *    See {@link https://docs.microsoft.com/aspnet/core/signalr/configuration#configure-logging|the documentation for client logging configuration} for more details.
     * @returns The {@link @microsoft/signalr.HubConnectionBuilder} instance, for chaining.
     */
    configureLogging(logging: LogLevel | string | ILogger): HubConnectionBuilder;
    /** Configures the {@link @microsoft/signalr.HubConnection} to use HTTP-based transports to connect to the specified URL.
     *
     * The transport will be selected automatically based on what the server and client support.
     *
     * @param {string} url The URL the connection will use.
     * @returns The {@link @microsoft/signalr.HubConnectionBuilder} instance, for chaining.
     */
    withUrl(url: string): HubConnectionBuilder;
    /** Configures the {@link @microsoft/signalr.HubConnection} to use the specified HTTP-based transport to connect to the specified URL.
     *
     * @param {string} url The URL the connection will use.
     * @param {HttpTransportType} transportType The specific transport to use.
     * @returns The {@link @microsoft/signalr.HubConnectionBuilder} instance, for chaining.
     */
    withUrl(url: string, transportType: HttpTransportType): HubConnectionBuilder;
    /** Configures the {@link @microsoft/signalr.HubConnection} to use HTTP-based transports to connect to the specified URL.
     *
     * @param {string} url The URL the connection will use.
     * @param {IHttpConnectionOptions} options An options object used to configure the connection.
     * @returns The {@link @microsoft/signalr.HubConnectionBuilder} instance, for chaining.
     */
    withUrl(url: string, options: IHttpConnectionOptions): HubConnectionBuilder;
    /** Configures the {@link @microsoft/signalr.HubConnection} to use the specified Hub Protocol.
     *
     * @param {IHubProtocol} protocol The {@link @microsoft/signalr.IHubProtocol} implementation to use.
     */
    withHubProtocol(protocol: IHubProtocol): HubConnectionBuilder;
    /** Configures the {@link @microsoft/signalr.HubConnection} to automatically attempt to reconnect if the connection is lost.
     * By default, the client will wait 0, 2, 10 and 30 seconds respectively before trying up to 4 reconnect attempts.
     */
    withAutomaticReconnect(): HubConnectionBuilder;
    /** Configures the {@link @microsoft/signalr.HubConnection} to automatically attempt to reconnect if the connection is lost.
     *
     * @param {number[]} retryDelays An array containing the delays in milliseconds before trying each reconnect attempt.
     * The length of the array represents how many failed reconnect attempts it takes before the client will stop attempting to reconnect.
     */
    withAutomaticReconnect(retryDelays: number[]): HubConnectionBuilder;
    /** Configures the {@link @microsoft/signalr.HubConnection} to automatically attempt to reconnect if the connection is lost.
     *
     * @param {IRetryPolicy} reconnectPolicy An {@link @microsoft/signalR.IRetryPolicy} that controls the timing and number of reconnect attempts.
     */
    withAutomaticReconnect(reconnectPolicy: IRetryPolicy): HubConnectionBuilder;
    /** Creates a {@link @microsoft/signalr.HubConnection} from the configuration options specified in this builder.
     *
     * @returns {HubConnection} The configured {@link @microsoft/signalr.HubConnection}.
     */
    build(): HubConnection;
}
//# sourceMappingURL=HubConnectionBuilder.d.ts.map