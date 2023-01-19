import { HttpClient, HttpRequest, HttpResponse } from "./HttpClient";
import { ILogger } from "./ILogger";
export declare class XhrHttpClient extends HttpClient {
    private readonly _logger;
    constructor(logger: ILogger);
    /** @inheritDoc */
    send(request: HttpRequest): Promise<HttpResponse>;
}
//# sourceMappingURL=XhrHttpClient.d.ts.map