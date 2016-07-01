// Limit dependencies to core Node modules. This means the code in this file has to be very low-level and unattractive,
// but simplifies things for the consumer of this module.
import * as http from 'http';
import * as path from 'path';
import { parseArgs } from './Util/ArgsUtil';
import { autoQuitOnFileChange } from './Util/AutoQuit';

// Webpack doesn't support dynamic requires for files not present at compile time, so grab a direct
// reference to Node's runtime 'require' function.
const dynamicRequire: (name: string) => any = eval('require');

const parsedArgs = parseArgs(process.argv);
if (parsedArgs.watch) {
    autoQuitOnFileChange(process.cwd(), parsedArgs.watch.split(','));
}

const server = http.createServer((req, res) => {
    readRequestBodyAsJson(req, bodyJson => {
        const resolvedPath = path.resolve(process.cwd(), bodyJson.moduleName);
        const invokedModule = dynamicRequire(resolvedPath);
        const func = bodyJson.exportedFunctionName ? invokedModule[bodyJson.exportedFunctionName] : invokedModule;
        if (!func) {
            throw new Error('The module "' + resolvedPath + '" has no export named "' + bodyJson.exportedFunctionName + '"');
        }

        let hasSentResult = false;
        const callback = (errorValue, successValue) => {
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

        // Support streamed responses
        Object.defineProperty(callback, 'stream', {
            enumerable: true,
            get: function() {
                if (!hasSentResult) {
                    hasSentResult = true;
                    res.setHeader('Content-Type', 'application/octet-stream');
                }

                return res;
            }
        });

        try {
            func.apply(null, [callback].concat(bodyJson.args));
        } catch (synchronousException) {
            callback(synchronousException, null);
        }
    });
});

const requestedPortOrZero = parsedArgs.port || 0; // 0 means 'let the OS decide'
server.listen(requestedPortOrZero, 'localhost', function () {
    // Signal to HttpNodeHost which port it should make its HTTP connections on
    console.log('[Microsoft.AspNetCore.NodeServices.HttpNodeHost:Listening on port ' + server.address().port + '\]');

    // Signal to the NodeServices base class that we're ready to accept invocations
    console.log('[Microsoft.AspNetCore.NodeServices:Listening]');
});

function readRequestBodyAsJson(request, callback) {
    let requestBodyAsString = '';
    request
        .on('data', chunk => { requestBodyAsString += chunk; })
        .on('end', () => { callback(JSON.parse(requestBodyAsString)); });
}
