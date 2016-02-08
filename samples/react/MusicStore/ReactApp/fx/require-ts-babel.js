var fs = require('fs');
var ts = require('ntypescript');
var babelCore = require('babel-core');
var resolveBabelRc = require('babel-loader/lib/resolve-rc'); // If this ever breaks, we can easily scan up the directory hierarchy ourselves 
var origJsLoader = require.extensions['.js'];

function resolveBabelOptions(relativeToFilename) {
    var babelRcText = resolveBabelRc(relativeToFilename);
    return babelRcText ? JSON.parse(babelRcText) : {};
}

function loadViaTypeScript(module, filename) {
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

module.exports = function register() {
    require.extensions['.js'] = loadViaBabel;
    require.extensions['.jsx'] = loadViaBabel;
    require.extensions['.ts'] = loadViaTypeScript;
    require.extensions['.tsx'] = loadViaTypeScript;
}
