var createMemoryHistory = require('history/lib/createMemoryHistory');
var url = require('url');
var babelCore = require('babel-core');
var babelConfig = {
    presets: ["es2015", "react"]
};

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

var domainTasks = require('./domain-tasks.js');
var bootServer = require('../boot-server.jsx').default;

function render(requestUrl, callback) {
    var store;
    var params = {
        location: url.parse(requestUrl),
        history: createMemoryHistory(requestUrl),
        state: undefined
    };
    
    // Open a new domain that can track all the async tasks commenced during first render
    domainTasks.run(function() {
        // Since route matching is asynchronous, add the rendering itself to the list of tasks we're awaiting
        domainTasks.addTask(new Promise(function (resolve, reject) {
            // Now actually perform the first render that will match a route and commence associated tasks
            bootServer(params, function(error, result) {
                if (error) {
                    reject(error);
                } else {
                    // The initial 'loading' state HTML is irrelevant - we only want to capture the state
                    // so we can use it to perform a real render once all data is loaded
                    store = result.store;
                    resolve();
                }
            });
        }));
    }).then(function() {
        // By now, all the data should be loaded, so we can render for real based on the state now
        params.state = store.getState();
        bootServer(params, function(error, result) {
            if (error) {
                callback(error, null);
            } else {
                var html = result.html + `<script>window.__INITIAL_STATE = ${ JSON.stringify(store.getState()) }</script>`;
                callback(null, html)
            }
        });
    }).catch(function(error) {
        process.nextTick(() => { // Because otherwise you can't throw from inside a catch
            callback(error, null);
        });
    });
}

render('/', (err, html) => {
    if (err) {
        throw err;
    }
    
    console.log(html);
});
