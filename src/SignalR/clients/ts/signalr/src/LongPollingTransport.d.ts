import { HttpClient } from "./HttpClient";
import { ILogger } from "./ILogger";
import { ITransport, TransferFormat } from "./ITransport";
import { IHttpConnectionOptions } from "./IHttpConnectionOptions";
/** @private */
export declare class LongPollingTransport implements ITransport {
    private readonly _httpClient;
    private readonly _logger;
    private readonly _options;
    private readonly _pollAbort;
    private _url?;
    private _running;
    private _receiving?;
    private _closeError?;
    onreceive: ((data: string | ArrayBuffer) => void) | null;
    onclose: ((error?: Error | unknown) => void) | null;
    get pollAborted(): boolean;
    constructor(httpClient: HttpClient, logger: ILogger, options: IHttpConnectionOptions);
    connect(url: string, transferFormat: TransferFormat): Promise<void>;
    private _poll;
    send(data: any): Promise<void>;
    stop(): Promise<void>;
    private _raiseOnClose;
}
//# sourceMappingURL=LongPollingTransport.d.ts.map