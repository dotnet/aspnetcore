// -----------------------------------------------------------------------------------------
// Loading via Webpack
// This is optional. You don't have to use Webpack. But if you are doing, it's extremely convenient
// to be able to load your boot module via Webpack compilation, so you can use whatever source language
// you like (e.g., TypeScript), and so that loader plugins (e.g., require('./mystyles.less')) work in
// exactly the same way on the server as you do on the client.
// If you don't use Webpack, then it's up to you to define a plain-JS boot module that in turn loads
// whatever other files you need (e.g., using some other compiler/bundler API, or maybe just having
// already precompiled to plain JS files on disk).
function loadViaWebpackNoCache(webpackConfigPath, modulePath) {
    var ExternalsPlugin = require('webpack-externals-plugin');
    var requireFromString = require('require-from-string');
    var MemoryFS = require('memory-fs');
    var webpack = require('webpack');

    return new Promise(function(resolve, reject) {
        // Load the Webpack config and make alterations needed for loading the output into Node
        var webpackConfig = require(webpackConfigPath);
        webpackConfig.entry = modulePath;
        webpackConfig.target = 'node';
        webpackConfig.output = { path: '/', filename: 'webpack-output.js', libraryTarget: 'commonjs' };

        // In Node, we want anything under /node_modules/ to be loaded natively and not bundled into the output
        // (partly because it's faster, but also because otherwise there'd be different instances of modules
        // depending on how they were loaded, which could lead to errors)
        webpackConfig.plugins = webpackConfig.plugins || [];
        webpackConfig.plugins.push(new ExternalsPlugin({ type: 'commonjs', include: /node_modules/ }));

        // The CommonsChunkPlugin is not compatible with a CommonJS environment like Node, nor is it needed in that case 
        webpackConfig.plugins = webpackConfig.plugins.filter(function(plugin) {
            return !(plugin instanceof webpack.optimize.CommonsChunkPlugin);
        });

        // Create a compiler instance that stores its output in memory, then load its output
        var compiler = webpack(webpackConfig);
        compiler.outputFileSystem = new MemoryFS();
        compiler.run(function(err, stats) {
            if (err) {
                reject(err);
            } else {
                var fileContent = compiler.outputFileSystem.readFileSync('/webpack-output.js', 'utf8');
                var moduleInstance = requireFromString(fileContent);
                resolve(moduleInstance);
            }
        });
    });
}

// Ensure we only go through the compile process once per [config, module] pair
var loadViaWebpackPromisesCache = {};
function loadViaWebpack(webpackConfigPath, modulePath, callback) {
    var cacheKey = JSON.stringify(webpackConfigPath) + JSON.stringify(modulePath);
    if (!(cacheKey in loadViaWebpackPromisesCache)) {
        loadViaWebpackPromisesCache[cacheKey] = loadViaWebpackNoCache(webpackConfigPath, modulePath);
    }
    loadViaWebpackPromisesCache[cacheKey].then(function(result) {
        callback(null, result);
    }, function(error) {
        callback(error);
    })
}

// -----------------------------------------------------------------------------------------
// Rendering

var url = require('url');
var path = require('path');
var domain = require('domain');
var domainTask = require('domain-task');
var baseUrl = require('domain-task/fetch').baseUrl;

function findBootModule(bootModule, callback) {
    var bootModuleNameFullPath = path.resolve(process.cwd(), bootModule.moduleName); 
    if (bootModule.webpackConfig) {
        var webpackConfigFullPath = path.resolve(process.cwd(), bootModule.webpackConfig);
        loadViaWebpack(webpackConfigFullPath, bootModuleNameFullPath, callback);
    } else {
        callback(null, require(bootModuleNameFullPath));
    }
}

function findBootFunc(bootModule, callback) {
    // First try to load the module (possibly via Webpack)
    findBootModule(bootModule, function(findBootModuleError, foundBootModule) {
        if (findBootModuleError) {
            callback(findBootModuleError);
            return;
        }
        
        // Now try to pick out the function they want us to invoke
        var bootFunc;
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
                callback(new Error('The module at ' + bootModule.moduleName + ' has no function export named ' + bootModule.exportName + '.'));
            } else {
                callback(new Error('The module at ' + bootModule.moduleName + ' does not export a default function, and you have not specified which export to invoke.'));
            }
        } else {
            callback(null, bootFunc);
        }
    });
}

function renderToString(callback, bootModule, absoluteRequestUrl, requestPathAndQuery) {
    findBootFunc(bootModule, function (findBootFuncError, bootFunc) {
        if (findBootFuncError) {
            callback(findBootFuncError);
            return;
        }
        
        // Prepare a promise that will represent the completion of all domain tasks in this execution context.
        // The boot code will wait for this before performing its final render.
        var domainTaskCompletionPromiseResolve;
        var domainTaskCompletionPromise = new Promise(function (resolve, reject) {
            domainTaskCompletionPromiseResolve = resolve;
        });
        var params = {
            location: url.parse(requestPathAndQuery),
            url: requestPathAndQuery,
            absoluteUrl: absoluteRequestUrl,
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
