import { HttpClient } from "./HttpClient";
import { ILogger } from "./ILogger";
import { ITransport, TransferFormat } from "./ITransport";
import { IHttpConnectionOptions } from "./IHttpConnectionOptions";
/** @private */
export declare class ServerSentEventsTransport implements ITransport {
    private readonly _httpClient;
    private readonly _accessToken;
    private readonly _logger;
    private readonly _options;
    private _eventSource?;
    private _url?;
    onreceive: ((data: string | ArrayBuffer) => void) | null;
    onclose: ((error?: Error | unknown) => void) | null;
    constructor(httpClient: HttpClient, accessToken: string | undefined, logger: ILogger, options: IHttpConnectionOptions);
    connect(url: string, transferFormat: TransferFormat): Promise<void>;
    send(data: any): Promise<void>;
    stop(): Promise<void>;
    private _close;
}
//# sourceMappingURL=ServerSentEventsTransport.d.ts.map