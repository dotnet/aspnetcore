import { Provider, NgModule, Inject } from '@angular/core';
import { Headers, Http, ResponseOptions, RequestOptionsArgs, Response } from '@angular/http';
import { Observable } from 'rxjs/Observable';
import 'rxjs/add/observable/of';
import 'rxjs/add/operator/map';
const globalSerializedStateKey = 'HTTP_STATE_TRANSFER';
const backingStoreDIToken = 'HTTP_STATE_BACKING_STORE';

export interface CacheOptions {
    permanent: boolean;
}

export interface CachedHttpResponse {
    headers: { [name: string]: any } | null;
    status: number;
    statusText: string | null;
    text: string;
    url: string;
}

export type BackingStore = { [key: string]: CachedHttpResponse };

export class HttpWithStateTransfer {
    private backingStore: BackingStore;
    private http: Http;

    constructor(@Inject(Http) http: Http, @Inject(backingStoreDIToken) backingStore: BackingStore) {
        this.http = http;
        this.backingStore = backingStore;
    }

    public stateForTransfer(): any {
        return { [globalSerializedStateKey]: this.backingStore };
    }

    public get(url: string, options?: CacheOptions, requestOptions?: RequestOptionsArgs): Observable<Response> {
        return this.getCachedResponse(/* cacheKey */ url, () => this.http.get(url, requestOptions), options);
    }

    private getCachedResponse(cacheKey: string, provider: () => Observable<Response>, options?: CacheOptions): Observable<Response> {        
        // By default, the cache is only used for the *first* client-side read. So, we're only performing
        // a one-time transfer of server-side response to the client. If you want to keep and reuse cached
        // responses continually during server-side and client-side execution, set 'permanent' to 'true.
        const isClient = typeof window !== 'undefined';
        const isPermanent = options && options.permanent;

        const allowReadFromCache = isClient || isPermanent;
        if (allowReadFromCache && this.backingStore.hasOwnProperty(cacheKey)) {
            const cachedValue = this.backingStore[cacheKey];
            if (!isPermanent) {
                delete this.backingStore[cacheKey];
            }
            return Observable.of(new Response(new ResponseOptions({
                body: cachedValue.text,
                headers: new Headers(cachedValue.headers),
                status: cachedValue.status,
                url: cachedValue.url
            })));
        }

        return provider()
            .map(response => {
                const allowWriteToCache = !isClient || isPermanent;
                if (allowWriteToCache) {
                    this.backingStore[cacheKey] = {
                        headers: response.headers ? response.headers.toJSON() : null,
                        status: response.status,
                        statusText: response.statusText,
                        text: response.text(),
                        url: response.url
                    };
                }

                return response;
            });
    }
}

export function defaultBackingStoreFactory() {
    const transferredData = typeof window !== 'undefined' ? (window as any)[globalSerializedStateKey] : null;
    return transferredData || {};
}

@NgModule({
    providers: [
        // The backing store is a separate DI service so you could override exactly how it gets
        // transferred from server to client
        { provide: backingStoreDIToken, useFactory: defaultBackingStoreFactory },

        { provide: HttpWithStateTransfer, useClass: HttpWithStateTransfer },
    ]
})
export class HttpWithStateTransferModule {
}
