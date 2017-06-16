import { IHttpClient } from "./HttpClient"
import { TransportType, ITransport } from "./Transports"

export interface IHttpConnectionOptions {
    httpClient?: IHttpClient;
    transport?: TransportType | ITransport;
}
