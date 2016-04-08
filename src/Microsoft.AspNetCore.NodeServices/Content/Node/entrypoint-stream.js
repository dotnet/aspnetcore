var path = require('path');
var readline = require('readline');
var invocationPrefix = 'invoke:';

function invocationCallback(errorValue, successValue) {
    if (errorValue) {
        throw new Error('InputOutputStreamHost doesn\'t support errors. Got error: ' + errorValue.toString());
    } else {
        var serializedResult = JSON.stringify(successValue);
        console.log(serializedResult);
    }
}

readline.createInterface({ input: process.stdin }).on('line', function (message) {
  if (message && message.substring(0, invocationPrefix.length) === invocationPrefix) {
    var invocation = JSON.parse(message.substring(invocationPrefix.length));
    var invokedModule = require(path.resolve(process.cwd(), invocation.moduleName));
    var func = invocation.exportedFunctionName ? invokedModule[invocation.exportedFunctionName] : invokedModule;
    func.apply(null, [invocationCallback].concat(invocation.args));
  }
});

console.log('[Microsoft.AspNetCore.NodeServices:Listening]'); // The .NET app waits for this signal before sending any invocations
