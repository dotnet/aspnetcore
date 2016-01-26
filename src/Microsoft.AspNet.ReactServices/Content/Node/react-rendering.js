var fs = require('fs');
var path = require('path');
var React = require('react');
var ReactDOMServer = require('react-dom/server');
var createMemoryHistory = require('history/lib/createMemoryHistory');
var babelCore = require('babel-core');
var babelConfig = {};

var origJsLoader = require.extensions['.js']; 
require.extensions['.js'] = loadViaBabel;
require.extensions['.jsx'] = loadViaBabel;

function findReactComponent(options) {
    var resolvedPath = path.resolve(process.cwd(), options.moduleName);
    var loadedModule = require(resolvedPath);
    if (options.exportName) {
        // If exportName is specified explicitly, use it
        if (!(options.exportName in loadedModule)) {
            throw new Error('The module "' + resolvedPath + '" has no export named "' + options.exportName + '"');
        }
        return loadedModule[options.exportName];
    } else if (typeof loadedModule === 'function') {
        // Otherwise, if the module itself is a function, assume that is the component 
        return loadedModule;
    } else if (typeof loadedModule.default === 'function') {
        // Otherwise, if the module has a default export which is a function, assume that is the component
        return loadedModule.default;
    } else {
        throw new Error('Cannot find React component, because no export name was specified, and the module "' + resolvedPath + '" has no default exported class.');
    }
}

function loadViaBabel(module, filename) {
    // Assume that all the app's own code is ES2015+ (optionally with JSX), but that none of the node_modules are.
    // The distinction is important because ES2015+ forces strict mode, and it may break ES3/5 if you try to run it in strict
    // mode when the developer didn't expect that (e.g., current versions of underscore.js can't be loaded in strict mode). 
    var useBabel = filename.indexOf('node_modules') < 0;
    if (useBabel) {
        var transformedFile = babelCore.transformFileSync(filename, babelConfig);
        return module._compile(transformedFile.code, filename);        
    } else {
        return origJsLoader.apply(this, arguments);
    }
}

module.exports = {
    renderToString: function(callback, options) {
        try {
            var component = findReactComponent(options);
            var history = createMemoryHistory(options.requestUrl);
            var reactElement = React.createElement(component, { history: history });
            var html = ReactDOMServer.renderToString(reactElement);
            callback(null, html);
        } catch (synchronousException) {
            callback(synchronousException);
        }
    }
};
