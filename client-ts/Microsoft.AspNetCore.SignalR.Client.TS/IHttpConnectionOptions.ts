import { IHttpClient } from "./HttpClient"
import { TransportType, ITransport } from "./Transports"
import { ILogger } from "./ILogger";

export interface IHttpConnectionOptions {
    httpClient?: IHttpClient;
    transport?: TransportType | ITransport;
    logger?: ILogger;
}
