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
        var resolvedPath = path.resolve(process.cwd(), options.moduleName);
        var requestedModule = require(resolvedPath);
        var component = options.exportName ? requestedModule[options.exportName] : requestedModule; 
        if (!component) {
            throw new Error('The module "' + resolvedPath + '" has no export named "' + options.exportName + '"');
        }
        
        var history = createMemoryHistory(options.baseUrl);
        var reactElement = React.createElement(component, { history: history });
        var html = ReactDOMServer.renderToString(reactElement);
        callback(null, html);
    }
};
