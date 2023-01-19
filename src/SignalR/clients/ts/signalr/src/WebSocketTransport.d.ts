import { HttpClient } from "./HttpClient";
import { MessageHeaders } from "./IHubProtocol";
import { ILogger } from "./ILogger";
import { ITransport, TransferFormat } from "./ITransport";
import { WebSocketConstructor } from "./Polyfills";
/** @private */
export declare class WebSocketTransport implements ITransport {
    private readonly _logger;
    private readonly _accessTokenFactory;
    private readonly _logMessageContent;
    private readonly _webSocketConstructor;
    private readonly _httpClient;
    private _webSocket?;
    private _headers;
    onreceive: ((data: string | ArrayBuffer) => void) | null;
    onclose: ((error?: Error) => void) | null;
    constructor(httpClient: HttpClient, accessTokenFactory: (() => string | Promise<string>) | undefined, logger: ILogger, logMessageContent: boolean, webSocketConstructor: WebSocketConstructor, headers: MessageHeaders);
    connect(url: string, transferFormat: TransferFormat): Promise<void>;
    send(data: any): Promise<void>;
    stop(): Promise<void>;
    private _close;
    private _isCloseEvent;
}
//# sourceMappingURL=WebSocketTransport.d.ts.map