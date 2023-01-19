import { HttpClient, HttpRequest, HttpResponse } from "./HttpClient";
import { ILogger } from "./ILogger";
/** Default implementation of {@link @microsoft/signalr.HttpClient}. */
export declare class DefaultHttpClient extends HttpClient {
    private readonly _httpClient;
    /** Creates a new instance of the {@link @microsoft/signalr.DefaultHttpClient}, using the provided {@link @microsoft/signalr.ILogger} to log messages. */
    constructor(logger: ILogger);
    /** @inheritDoc */
    send(request: HttpRequest): Promise<HttpResponse>;
    getCookieString(url: string): string;
}
//# sourceMappingURL=DefaultHttpClient.d.ts.map