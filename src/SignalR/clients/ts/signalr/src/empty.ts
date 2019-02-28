import { HttpClient, HttpResponse } from "./HttpClient";
import { ILogger } from "./ILogger";

export class NodeHttpClient extends HttpClient {
    // @ts-ignore
    public constructor(logger: ILogger) {
        super();
    }

    public send(): Promise<HttpResponse> {
        throw new Error("Method not implemented.");
    }
}
