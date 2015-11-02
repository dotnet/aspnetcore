var path = require('path');
var express = require('express');
var bodyParser = require('body-parser')
var requestedPortOrZero = parseInt(process.argv[2]) || 0; // 0 means 'let the OS decide'

autoQuitOnFileChange(process.cwd(), ['.js', '.json', '.html']);

var app = express();
app.use(bodyParser.json());

app.all('/', function (req, res) {
    var resolvedPath = path.resolve(process.cwd(), req.body.moduleName);
    var invokedModule = require(resolvedPath);
    var func = req.body.exportedFunctionName ? invokedModule[req.body.exportedFunctionName] : invokedModule;
    if (!func) {
        throw new Error('The module "' + resolvedPath + '" has no export named "' + req.body.exportedFunctionName + '"');
    }
    
    var hasSentResult = false;
    var callback = function(errorValue, successValue) {
        if (!hasSentResult) {
            hasSentResult = true;
            if (errorValue) {
                res.status(500).send(errorValue);
            } else {
                sendResult(res, successValue);
            }
        }
    };
    
    func.apply(null, [callback].concat(req.body.args));
});

var listener = app.listen(requestedPortOrZero, 'localhost', function () {
    // Signal to HttpNodeHost which port it should make its HTTP connections on
    console.log('[Microsoft.AspNet.NodeServices.HttpNodeHost:Listening on port ' + listener.address().port + '\]');
    
    // Signal to the NodeServices base class that we're ready to accept invocations
    console.log('[Microsoft.AspNet.NodeServices:Listening]');
});

function sendResult(response, result) {
    if (typeof result === 'object') {
        response.json(result);
    } else {
        response.send(result);
    }
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
