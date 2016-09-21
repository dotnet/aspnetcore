import 'es6-promise';
import * as url from 'url';
import * as path from 'path';
import * as domain from 'domain';
import { run as domainTaskRun } from 'domain-task/main';
import { baseUrl } from 'domain-task/fetch';

const defaultTimeoutMilliseconds = 30 * 1000;

export interface RenderToStringCallback {
    (error: any, result: RenderToStringResult): void;
}

export interface RenderToStringResult {
    html: string;
    globals: { [key: string]: any };
}

export interface BootFunc {
    (params: BootFuncParams): Promise<RenderToStringResult>;
}

export interface BootFuncParams {
    location: url.Url;          // e.g., Location object containing information '/some/path'
    origin: string;             // e.g., 'https://example.com:1234'
    url: string;                // e.g., '/some/path'
    absoluteUrl: string;        // e.g., 'https://example.com:1234/some/path'
    domainTasks: Promise<any>;
    data: any;                  // any custom object passed through from .NET
}

export interface BootModuleInfo {
    moduleName: string;
    exportName?: string;
    webpackConfig?: string;
}

export function renderToString(callback: RenderToStringCallback, applicationBasePath: string, bootModule: BootModuleInfo, absoluteRequestUrl: string, requestPathAndQuery: string, customDataParameter: any, overrideTimeoutMilliseconds: number) {
    findBootFunc(applicationBasePath, bootModule, (findBootFuncError, bootFunc) => {
        if (findBootFuncError) {
            callback(findBootFuncError, null);
            return;
        }

        // Prepare a promise that will represent the completion of all domain tasks in this execution context.
        // The boot code will wait for this before performing its final render.
        let domainTaskCompletionPromiseResolve;
        const domainTaskCompletionPromise = new Promise((resolve, reject) => {
            domainTaskCompletionPromiseResolve = resolve;
        });
        const parsedAbsoluteRequestUrl = url.parse(absoluteRequestUrl);
        const params: BootFuncParams = {
            location: url.parse(requestPathAndQuery),
            origin: parsedAbsoluteRequestUrl.protocol + '//' + parsedAbsoluteRequestUrl.host,
            url: requestPathAndQuery,
            absoluteUrl: absoluteRequestUrl,
            domainTasks: domainTaskCompletionPromise,
            data: customDataParameter
        };

        // Open a new domain that can track all the async tasks involved in the app's execution
        domainTaskRun(/* code to run */ () => {
            // Workaround for Node bug where native Promise continuations lose their domain context
            // (https://github.com/nodejs/node-v0.x-archive/issues/8648)
            // The domain.active property is set by the domain-context module
            bindPromiseContinuationsToDomain(domainTaskCompletionPromise, domain['active']);

            // Make the base URL available to the 'domain-tasks/fetch' helper within this execution context
            baseUrl(absoluteRequestUrl);

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
    });
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

function findBootModule<T>(applicationBasePath: string, bootModule: BootModuleInfo, callback: (error: any, foundModule: T) => void) {
    const bootModuleNameFullPath = path.resolve(applicationBasePath, bootModule.moduleName);
    if (bootModule.webpackConfig) {
        const webpackConfigFullPath = path.resolve(applicationBasePath, bootModule.webpackConfig);

        let aspNetWebpackModule: any;
        try {
            aspNetWebpackModule = require('aspnet-webpack');
        } catch (ex) {
            callback('To load your boot module via webpack (i.e., if you specify a \'webpackConfig\' option), you must install the \'aspnet-webpack\' NPM package. Error encountered while loading \'aspnet-webpack\': ' + ex.stack, null);
            return;
        }

        aspNetWebpackModule.loadViaWebpack(webpackConfigFullPath, bootModuleNameFullPath, callback);
    } else {
        callback(null, require(bootModuleNameFullPath));
    }
}

function findBootFunc(applicationBasePath: string, bootModule: BootModuleInfo, callback: (error: any, bootFunc: BootFunc) => void) {
    // First try to load the module (possibly via Webpack)
    findBootModule<any>(applicationBasePath, bootModule, (findBootModuleError, foundBootModule) => {
        if (findBootModuleError) {
            callback(findBootModuleError, null);
            return;
        }

        // Now try to pick out the function they want us to invoke
        let bootFunc: BootFunc;
        if (bootModule.exportName) {
            // Explicitly-named export
            bootFunc = foundBootModule[bootModule.exportName];
        } else if (typeof foundBootModule !== 'function') {
            // TypeScript-style default export
            bootFunc = foundBootModule.default;
        } else {
            // Native default export
            bootFunc = foundBootModule;
        }

        // Validate the result
        if (typeof bootFunc !== 'function') {
            if (bootModule.exportName) {
                callback(`The module at ${ bootModule.moduleName } has no function export named ${ bootModule.exportName }.`, null);
            } else {
                callback(`The module at ${ bootModule.moduleName } does not export a default function, and you have not specified which export to invoke.`, null);
            }
        } else {
            callback(null, bootFunc);
        }
    });
}

function bindPromiseContinuationsToDomain(promise: Promise<any>, domainInstance: domain.Domain) {
    const originalThen = promise.then;
    promise.then = function then(resolve, reject) {
        if (typeof resolve === 'function') {
            resolve = domainInstance.bind(resolve);
        }

        if (typeof reject === 'function') {
            reject = domainInstance.bind(reject);
        }

        return originalThen.call(this, resolve, reject);
    };
}
