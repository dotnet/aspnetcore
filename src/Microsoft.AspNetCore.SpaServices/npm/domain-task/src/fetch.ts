import * as url from 'url';
import * as domain from 'domain';
import * as domainContext from 'domain-context';
import { baseUrl } from './main';
const isomorphicFetch = require('isomorphic-fetch');
const isNode = typeof process === 'object' && process.versions && !!process.versions.node;

function issueRequest(baseUrl: string, req: string | Request, init?: RequestInit): Promise<any> {
    // Resolve relative URLs
    if (baseUrl) {
        if (req instanceof Request) {
            const reqAsRequest = req as Request;
            reqAsRequest.url = url.resolve(baseUrl, reqAsRequest.url);
        } else {
            req = url.resolve(baseUrl, req as string);
        }
    } else if (isNode) {
        // TODO: Consider only throwing if it's a relative URL, since absolute ones would work fine
        throw new Error(`
            When running outside the browser (e.g., in Node.js), you must specify a base URL
            before invoking domain-task's 'fetch' wrapper.
            Example:
                import { baseUrl } from 'domain-task/fetch';
                baseUrl('http://example.com'); // Relative URLs will be resolved against this
        `);
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

// Re-exporting baseUrl from this module for back-compatibility only
// Newer code that wants to access baseUrl should use the version exported from the root of this package
export { baseUrl } from './main';
