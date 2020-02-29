import * as url from 'url';
import * as path from 'path';
import * as domain from 'domain';
import { run as domainTaskRun, baseUrl as domainTaskBaseUrl } from 'domain-task/main';
import { BootFunc, BootFuncParams, BootModuleInfo, RenderToStringCallback, RenderToStringFunc } from './PrerenderingInterfaces';

const defaultTimeoutMilliseconds = 30 * 1000;

export function createServerRenderer(bootFunc: BootFunc): RenderToStringFunc {
    const resultFunc = (callback: RenderToStringCallback, applicationBasePath: string, bootModule: BootModuleInfo, absoluteRequestUrl: string, requestPathAndQuery: string, customDataParameter: any, overrideTimeoutMilliseconds: number, requestPathBase: string) => {
        // Prepare a promise that will represent the completion of all domain tasks in this execution context.
        // The boot code will wait for this before performing its final render.
        let domainTaskCompletionPromiseResolve;
        const domainTaskCompletionPromise = new Promise((resolve, reject) => {
            domainTaskCompletionPromiseResolve = resolve;
        });
        const parsedAbsoluteRequestUrl = url.parse(absoluteRequestUrl);
        const params: BootFuncParams = {
            // It's helpful for boot funcs to receive the query as a key-value object, so parse it here
            // e.g., react-redux-router requires location.query to be a key-value object for consistency with client-side behaviour
            location: url.parse(requestPathAndQuery, /* parseQueryString */ true),
            origin: parsedAbsoluteRequestUrl.protocol + '//' + parsedAbsoluteRequestUrl.host,
            url: requestPathAndQuery,
            baseUrl: (requestPathBase || '') + '/',
            absoluteUrl: absoluteRequestUrl,
            domainTasks: domainTaskCompletionPromise,
            data: customDataParameter
        };
        const absoluteBaseUrl = params.origin + params.baseUrl; // Should be same value as page's <base href>

        // Open a new domain that can track all the async tasks involved in the app's execution
        domainTaskRun(/* code to run */ () => {
            // Workaround for Node bug where native Promise continuations lose their domain context
            // (https://github.com/nodejs/node-v0.x-archive/issues/8648)
            // The domain.active property is set by the domain-context module
            bindPromiseContinuationsToDomain(domainTaskCompletionPromise, domain['active']);

            // Make the base URL available to the 'domain-tasks/fetch' helper within this execution context
            domainTaskBaseUrl(absoluteBaseUrl);

            // Begin rendering, and apply a timeout
            const bootFuncPromise = bootFunc(params);
            if (!bootFuncPromise || typeof bootFuncPromise.then !== 'function') {
                callback(`Prerendering failed because the boot function in ${bootModule.moduleName} did not return a promise.`, null);
                return;
            }
            const timeoutMilliseconds = overrideTimeoutMilliseconds || defaultTimeoutMilliseconds; // e.g., pass -1 to override as 'never time out'
            const bootFuncPromiseWithTimeout = timeoutMilliseconds > 0
                ? wrapWithTimeout(bootFuncPromise, timeoutMilliseconds,
                    `Prerendering timed out after ${timeoutMilliseconds}ms because the boot function in '${bootModule.moduleName}' `
                    + 'returned a promise that did not resolve or reject. Make sure that your boot function always resolves or '
                    + 'rejects its promise. You can change the timeout value using the \'asp-prerender-timeout\' tag helper.')
                : bootFuncPromise;

            // Actually perform the rendering
            bootFuncPromiseWithTimeout.then(successResult => {
                callback(null, successResult);
            }, error => {
                callback(error, null);
            });
        }, /* completion callback */ errorOrNothing => {
            if (errorOrNothing) {
                callback(errorOrNothing, null);
            } else {
                // There are no more ongoing domain tasks (typically data access operations), so we can resolve
                // the domain tasks promise which notifies the boot code that it can do its final render.
                domainTaskCompletionPromiseResolve();
            }
        });
    };

    // Indicate to the prerendering code bundled into Microsoft.AspNetCore.SpaServices that this is a serverside rendering
    // function, so it can be invoked directly. This flag exists only so that, in its absence, we can run some different
    // backward-compatibility logic.
    resultFunc['isServerRenderer'] = true;

    return resultFunc;
}

function wrapWithTimeout<T>(promise: Promise<T>, timeoutMilliseconds: number, timeoutRejectionValue: any): Promise<T> {
    return new Promise<T>((resolve, reject) => {
        const timeoutTimer = setTimeout(() => {
            reject(timeoutRejectionValue);
        }, timeoutMilliseconds);

        promise.then(
            resolvedValue => {
                clearTimeout(timeoutTimer);
                resolve(resolvedValue);
            },
            rejectedValue => {
                clearTimeout(timeoutTimer);
                reject(rejectedValue);
            }
        )
    });
}

function bindPromiseContinuationsToDomain(promise: Promise<any>, domainInstance: domain.Domain) {
    const originalThen = promise.then;
    promise.then = (function then(resolve, reject) {
        if (typeof resolve === 'function') {
            resolve = domainInstance.bind(resolve);
        }

        if (typeof reject === 'function') {
            reject = domainInstance.bind(reject);
        }

        return originalThen.call(this, resolve, reject);
    }) as any;
}
