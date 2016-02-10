// -----------------------------------------------------------------------------------------
// Prepare the Node environment to support loading .jsx/.ts/.tsx files without needing precompilation,
// since that's such a common scenario. In the future, this might become a config option.
// This is bundled in with the actual prerendering logic below just to simplify the initialization
// logic (we can't have cross-file imports, because these files don't exist on disk until the
// StringAsTempFile utility puts them there temporarily).

// TODO: Consider some general method for checking if you have all the necessary NPM modules installed,
// and if not, giving an error that tells you what command to execute to install the missing ones.
var fs = require('fs');
var ts = requireIfInstalled('ntypescript');
var babelCore = require('babel-core');
var resolveBabelRc = require('babel-loader/lib/resolve-rc'); // If this ever breaks, we can easily scan up the directory hierarchy ourselves 
var origJsLoader = require.extensions['.js'];

function resolveBabelOptions(relativeToFilename) {
    var babelRcText = resolveBabelRc(relativeToFilename);
    try {
        return babelRcText ? JSON.parse(babelRcText) : {};
    } catch (ex) {
        ex.message = 'Error while parsing babelrc JSON: ' + ex.message;
        throw ex;
    }
}

function loadViaTypeScript(module, filename) {
    if (!ts) {
        throw new Error('Can\'t load .ts/.tsx files because the \'ntypescript\' package isn\'t installed.\nModule requested: ' + module);
    }
    
    // First perform a minimal transpilation from TS code to ES2015. This is very fast (doesn't involve type checking)
    // and is unlikely to need any special compiler options
    var src = fs.readFileSync(filename, 'utf8');
    var compilerOptions = { jsx: ts.JsxEmit.Preserve, module: ts.ModuleKind.ES2015, target: ts.ScriptTarget.ES6 };
    var es6Code = ts.transpile(src, compilerOptions, 'test.tsx', /* diagnostics */ []);
    
    // Second, process the ES2015 via Babel. We have to do this (instead of going directly from TS to ES5) because
    // TypeScript's ES5 output isn't exactly compatible with Node-style CommonJS modules. The main issue is with
    // resolving default exports - https://github.com/Microsoft/TypeScript/issues/2719
    var es5Code = babelCore.transform(es6Code, resolveBabelOptions(filename)).code;
    return module._compile(es5Code, filename);
}

function loadViaBabel(module, filename) {
    // Assume that all the app's own code is ES2015+ (optionally with JSX), but that none of the node_modules are.
    // The distinction is important because ES2015+ forces strict mode, and it may break ES3/5 if you try to run it in strict
    // mode when the developer didn't expect that (e.g., current versions of underscore.js can't be loaded in strict mode).
    var useBabel = filename.indexOf('node_modules') < 0;
    if (useBabel) {
        var transformedFile = babelCore.transformFileSync(filename, resolveBabelOptions(filename));
        return module._compile(transformedFile.code, filename);
    } else {
        return origJsLoader.apply(this, arguments);
    }
}

function requireIfInstalled(packageName) {
    return isPackageInstalled(packageName) ? require(packageName) : null;
}

function isPackageInstalled(packageName) {
    try {
        require.resolve(packageName);
        return true;
    } catch(e) {
        return false;
    }
}

function register() {
    require.extensions['.js'] = loadViaBabel;
    require.extensions['.jsx'] = loadViaBabel;
    require.extensions['.ts'] = loadViaTypeScript;
    require.extensions['.tsx'] = loadViaTypeScript;
};

register();

// -----------------------------------------------------------------------------------------
// Rendering

var url = require('url');
var path = require('path');
var domain = require('domain');
var domainTask = require('domain-task');
var baseUrl = require('domain-task/fetch').baseUrl;

function findBootFunc(bootModulePath, bootModuleExport) {
    var resolvedPath = path.resolve(process.cwd(), bootModulePath);
    var bootFunc = require(resolvedPath);
    if (bootModuleExport) {
        bootFunc = bootFunc[bootModuleExport];
    } else if (typeof bootFunc !== 'function') {
        bootFunc = bootFunc.default; // TypeScript sometimes uses this name for default exports
    }
    if (typeof bootFunc !== 'function') {
        if (bootModuleExport) {
            throw new Error('The module at ' + bootModulePath + ' has no function export named ' + bootModuleExport + '.');
        } else {
            throw new Error('The module at ' + bootModulePath + ' does not export a default function, and you have not specified which export to invoke.');
        }
    }
    
    return bootFunc;
}

function renderToString(callback, bootModulePath, bootModuleExport, absoluteRequestUrl, requestPathAndQuery) {
    var bootFunc = findBootFunc(bootModulePath, bootModuleExport);

    // Prepare a promise that will represent the completion of all domain tasks in this execution context.
    // The boot code will wait for this before performing its final render.
    var domainTaskCompletionPromiseResolve;
    var domainTaskCompletionPromise = new Promise(function (resolve, reject) {
        domainTaskCompletionPromiseResolve = resolve;
    });
    var params = {
        location: url.parse(requestPathAndQuery),
        url: requestPathAndQuery,
        domainTasks: domainTaskCompletionPromise
    };

    // Open a new domain that can track all the async tasks involved in the app's execution
    domainTask.run(function() {
        // Workaround for Node bug where native Promise continuations lose their domain context
        // (https://github.com/nodejs/node-v0.x-archive/issues/8648)
        bindPromiseContinuationsToDomain(domainTaskCompletionPromise, domain.active);
        
        // Make the base URL available to the 'domain-tasks/fetch' helper within this execution context
        baseUrl(absoluteRequestUrl);
        
        // Actually perform the rendering
        bootFunc(params).then(function(successResult) {
            callback(null, { html: successResult.html, globals: successResult.globals });
        }, function(error) {
            callback(error, null);
        });
    }, function allDomainTasksCompleted(error) {
        // There are no more ongoing domain tasks (typically data access operations), so we can resolve
        // the domain tasks promise which notifies the boot code that it can do its final render.
        if (error) {
            callback(error, null);
        } else {
            domainTaskCompletionPromiseResolve();
        }
    });
}

function bindPromiseContinuationsToDomain(promise, domainInstance) {
    var originalThen = promise.then; 
    promise.then = function then(resolve, reject) {
        if (typeof resolve === 'function') { resolve = domainInstance.bind(resolve); }
        if (typeof reject === 'function') { reject = domainInstance.bind(reject); }
        return originalThen.call(this, resolve, reject);
    };
}

module.exports.renderToString = renderToString;
