import * as url from 'url';
import * as domain from 'domain';
import * as domainContext from 'domain-context';
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
    // As of domain-task 2.0.0, we no longer auto-add the 'fetch' promise to the current domain task list.
    // This is because it's misleading to do so, and can result in race-condition bugs, e.g.,
    // https://github.com/aspnet/JavaScriptServices/issues/166
    //
    // Consider this usage:
    // 
    // import { fetch } from 'domain-task/fetch';
    // fetch(something).then(callback1).then(callback2) ...etc... .then(data => updateCriticalAppState);
    //
    // If we auto-add the very first 'fetch' promise to the domain task list, then the domain task completion
    // callback might fire at any point among all the chained callbacks. If there are enough chained callbacks,
    // it's likely to occur before the final 'updateCriticalAppState' one. Previously we thought it was enough
    // for domain-task to use setTimeout(..., 0) so that its action occurred after all synchronously-scheduled
    // chained promise callbacks, but this turns out not to be the case. Current versions of Node will run
    // setTimeout-scheduled callbacks *before* setImmediate ones, if their timeout has elapsed. So even if you
    // use setTimeout(..., 10), then this callback will run before setImmediate(...) if there were 10ms or more
    // of CPU-blocking activity. In other words, a race condition.
    //
    // The correct design is for the final chained promise to be the thing added to the domain task list, but
    // this can only be done by the developer and not baked into the 'fetch' API. The developer needs to write
    // something like:
    //
    // var myTask = fetch(something).then(callback1).then(callback2) ...etc... .then(data => updateCriticalAppState);
    // addDomainTask(myTask);
    //
    // ... so that the domain-tasks-completed callback never fires until after 'updateCriticalAppState'.
    return issueRequest(baseUrl(), url, init);
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
