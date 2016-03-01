// Limit dependencies to core Node modules. This means the code in this file has to be very low-level and unattractive,
// but simplifies things for the consumer of this module.
var http = require('http');
var path = require('path');
var parsedArgs = parseArgs(process.argv);
var requestedPortOrZero = parsedArgs.port || 0; // 0 means 'let the OS decide'

if (parsedArgs.watch) {
    autoQuitOnFileChange(process.cwd(), parsedArgs.watch.split(','));
}

var server = http.createServer(function(req, res) {
    readRequestBodyAsJson(req, function(bodyJson) {
        var resolvedPath = path.resolve(process.cwd(), bodyJson.moduleName);
        var invokedModule = require(resolvedPath);
        var func = bodyJson.exportedFunctionName ? invokedModule[bodyJson.exportedFunctionName] : invokedModule;
        if (!func) {
            throw new Error('The module "' + resolvedPath + '" has no export named "' + bodyJson.exportedFunctionName + '"');
        }

        var hasSentResult = false;
        var callback = function(errorValue, successValue) {
            if (!hasSentResult) {
                hasSentResult = true;
                if (errorValue) {
                    res.statusCode = 500;

                    if (errorValue.stack) {
                      res.end(errorValue.stack);
                    } else {
                      res.end(errorValue.toString());
                    }
                } else if (typeof successValue !== 'string') {
                    // Arbitrary object/number/etc - JSON-serialize it
                    res.setHeader('Content-Type', 'application/json');
                    res.end(JSON.stringify(successValue));
                } else {
                    // String - can bypass JSON-serialization altogether
                    res.setHeader('Content-Type', 'text/plain');
                    res.end(successValue);
                }
            }
        };

        try {
            func.apply(null, [callback].concat(bodyJson.args));
        } catch (synchronousException) {
            callback(synchronousException, null);
        }
    });
});

server.listen(requestedPortOrZero, 'localhost', function () {
    // Signal to HttpNodeHost which port it should make its HTTP connections on
    console.log('[Microsoft.AspNet.NodeServices.HttpNodeHost:Listening on port ' + server.address().port + '\]');

    // Signal to the NodeServices base class that we're ready to accept invocations
    console.log('[Microsoft.AspNet.NodeServices:Listening]');
});

function readRequestBodyAsJson(request, callback) {
    var requestBodyAsString = '';
    request
        .on('data', function(chunk) { requestBodyAsString += chunk; })
        .on('end', function() { callback(JSON.parse(requestBodyAsString)); });
}

function autoQuitOnFileChange(rootDir, extensions) {
    // Note: This will only work on Windows/OS X, because the 'recursive' option isn't supported on Linux.
    // Consider using a different watch mechanism (though ideally without forcing further NPM dependencies).
    var fs = require('fs');
    var path = require('path');
    fs.watch(rootDir, { persistent: false, recursive: true }, function(event, filename) {
        var ext = path.extname(filename);
        if (extensions.indexOf(ext) >= 0) {
            console.log('Restarting due to file change: ' + filename);
            process.exit(0);
        }
    });
}

function parseArgs(args) {
    // Very simplistic parsing which is sufficient for the cases needed. We don't want to bring in any external
    // dependencies (such as an args-parsing library) to this file.
    var result = {};
    var currentKey = null;
    args.forEach(function(arg) {
        if (arg.indexOf('--') === 0) {
            var argName = arg.substring(2);
            result[argName] = undefined;
            currentKey = argName;
        } else if (currentKey) {
            result[currentKey] = arg;
            currentKey = null;
        }
    });

    return result;
}
