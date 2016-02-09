require('./require-ts-babel')(); // Enable loading TS/TSX/JSX/ES2015 modules
var url = require('url');
var domainTasks = require('domain-tasks');
var baseUrl = require('domain-tasks/fetch').baseUrl;

function render(bootModulePath, absoluteRequestUrl, requestPathAndQuery, callback) {
    var bootFunc = require(bootModulePath);
    if (typeof bootFunc !== 'function') {
        bootFunc = bootFunc.default;
    }
    if (typeof bootFunc !== 'function') {
        throw new Error('The module at ' + bootModulePath + ' must export a default function, otherwise we don\'t know how to invoke it.')   
    }
    
    var params = {
        location: url.parse(requestPathAndQuery),
        url: requestPathAndQuery,
        state: undefined
    };
    
    // Open a new domain that can track all the async tasks commenced during first render
    domainTasks.run(function() {
        baseUrl(absoluteRequestUrl);

        // Since route matching is asynchronous, add the rendering itself to the list of tasks we're awaiting
        domainTasks.addTask(new Promise(function (resolve, reject) {
            // Now actually perform the first render that will match a route and commence associated tasks
            bootFunc(params, function(error, result) {
                if (error) {
                    reject(error);
                } else {
                    // The initial 'loading' state HTML is irrelevant - we only want to capture the state
                    // so we can use it to perform a real render once all data is loaded
                    params.state = result.state;
                    resolve();
                }
            });
        }));
    }, function(error) {
        // By now, all the data should be loaded, so we can render for real based on the state now
        // TODO: Add an optimisation where, if domain-tasks had no outstanding tasks at the end of
        // the previous render, we don't re-render (we can use the previous html and state).
        if (error) { console.error(error); throw error; }
        bootFunc(params, callback);
    });
}

render('../boot-server.tsx', 'http://localhost:5000', '/', (err, html) => {
    if (err) {
        throw err;
    }
    
    console.log(html);
});
