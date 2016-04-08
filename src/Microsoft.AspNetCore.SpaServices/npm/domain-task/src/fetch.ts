import * as url from 'url';
import * as domain from 'domain';
import * as domainContext from 'domain-context';
import { addTask } from './main';
const isomorphicFetch = require('isomorphic-fetch');
const isBrowser: boolean = (new Function('try { return this === window; } catch (e) { return false; }'))();

// Not using a symbol, because this may need to run in a version of Node.js that doesn't support them
const domainTaskStateKey = '__DOMAIN_TASK_INTERNAL_FETCH_BASEURL__DO_NOT_REFERENCE_THIS__';
let noDomainBaseUrl: string;

function issueRequest(baseUrl: string, req: string | Request, init?: RequestInit): Promise<any> {
    // Resolve relative URLs
    if (baseUrl) {
        if (req instanceof Request) {
            const reqAsRequest = req as Request;
            reqAsRequest.url = url.resolve(baseUrl, reqAsRequest.url);
        } else {
            req = url.resolve(baseUrl, req as string);
        }
    } else if (!isBrowser) {
        // TODO: Consider only throwing if it's a relative URL, since absolute ones would work fine
        throw new Error(`
            When running outside the browser (e.g., in Node.js), you must specify a base URL
            before invoking domain-task's 'fetch' wrapper.
            Example:
                import { baseUrl } from 'domain-task/fetch';
                baseUrl('http://example.com'); // Relative URLs will be resolved against this
        `);
    }

    // Currently, some part of ASP.NET (perhaps just Kestrel on Mac - unconfirmed) doesn't complete
    // its responses if we send 'Connection: close', which is the default. So if no 'Connection' header
    // has been specified explicitly, use 'Connection: keep-alive'.
    init = init || {};
    init.headers = init.headers || {};
    if (!init.headers['Connection']) {
        init.headers['Connection'] = 'keep-alive';
    }

    return isomorphicFetch(req, init);
}

export function fetch(url: string | Request, init?: RequestInit): Promise<any> {
    const promise = issueRequest(baseUrl(), url, init);
    addTask(promise);
    return promise;
}

export function baseUrl(url?: string): string {
    if (url) {
        if (domain.active) {
            // There's an active domain (e.g., in Node.js), so associate the base URL with it
            domainContext.set(domainTaskStateKey, url);
        } else {
            // There's no active domain (e.g., in browser), so there's just one shared base URL
            noDomainBaseUrl = url;
        }
    }

    return domain.active ? domainContext.get(domainTaskStateKey) : noDomainBaseUrl;
}
