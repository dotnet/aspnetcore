import { IHttpClient } from "./HttpClient"
import { TransportType, ITransport } from "./Transports"
import { ILogger, LogLevel } from "./ILogger";

export interface IHttpConnectionOptions {
    httpClient?: IHttpClient;
    transport?: TransportType | ITransport;
    logging?: ILogger | LogLevel;
}
