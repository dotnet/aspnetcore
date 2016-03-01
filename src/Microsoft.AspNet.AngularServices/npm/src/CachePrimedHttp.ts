import { provide, Injectable, Provider } from 'angular2/core';
import { Connection, ConnectionBackend, Http, XHRBackend, RequestOptions, Request, RequestMethod, Response, ResponseOptions, ReadyState } from 'angular2/http';

@Injectable()
export class CachePrimedConnectionBackend extends ConnectionBackend {
    private _preCachedResponses: PreCachedResponses;

    constructor(private _underlyingBackend: ConnectionBackend, private _baseResponseOptions: ResponseOptions) {
        super();
        this._preCachedResponses = (<any>window).__preCachedResponses || {};
    }

    public createConnection(request: Request): Connection {
        let cacheKey = request.url;
        if (request.method === RequestMethod.Get && this._preCachedResponses.hasOwnProperty(cacheKey)) {
            return new CacheHitConnection(request, this._preCachedResponses[cacheKey], this._baseResponseOptions);
        } else {
            return this._underlyingBackend.createConnection(request);
        }
    }
}

class CacheHitConnection implements Connection {
    readyState: ReadyState;
    request: Request;
    response: any;

    constructor (req: Request, cachedResponse: PreCachedResponse, baseResponseOptions: ResponseOptions) {
        this.request = req;
        this.readyState = ReadyState.Done;

        // Workaround for difficulty consuming CommonJS default exports in TypeScript. Note that it has to be a dynamic
        // 'require', and not an 'import' statement, because the module isn't available on the server.
        let obsCtor: any = require('rxjs/Observable').Observable;
        this.response = new obsCtor(responseObserver => {
            let response = new Response(new ResponseOptions({ body: cachedResponse.body, status: cachedResponse.statusCode }));
            responseObserver.next(response);
            responseObserver.complete();
        });
    }
}

declare var require: any; // Part of the workaround mentioned below. Can remove this after updating Angular.

interface PreCachedResponses {
    [url: string]: PreCachedResponse;
}

interface PreCachedResponse {
    statusCode: number;
    body: string;
}

export const CACHE_PRIMED_HTTP_PROVIDERS = [
    provide(Http, {
        useFactory: (xhrBackend, requestOptions, responseOptions) => new Http(new CachePrimedConnectionBackend(xhrBackend, responseOptions), requestOptions),
        deps: [XHRBackend, RequestOptions, ResponseOptions]
    }),
];
