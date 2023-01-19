import { HttpClient, HttpRequest, HttpResponse } from "./HttpClient";
import { ILogger } from "./ILogger";
export declare class FetchHttpClient extends HttpClient {
    private readonly _abortControllerType;
    private readonly _fetchType;
    private readonly _jar?;
    private readonly _logger;
    constructor(logger: ILogger);
    /** @inheritDoc */
    send(request: HttpRequest): Promise<HttpResponse>;
    getCookieString(url: string): string;
}
//# sourceMappingURL=FetchHttpClient.d.ts.map