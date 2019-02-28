// Function signatures follow Node conventions.
// i.e., parameters: (callback, arg0, arg1, ... etc ...)
// When done, functions must invoke 'callback', passing (errorInfo, result)
// where errorInfo should be null/undefined if there was no error.

exports.getFixedString = function (callback) {
    callback(null, 'test result');
};

exports.getFixedStringWithDelay = function (callback) {
    setTimeout(callback(null, 'delayed test result'), 100);
};

exports.raiseError = function (callback) {
    callback('This is an error from Node');
};

exports.echoSimpleParameters = function (callback, param0, param1) {
    callback(null, `Param0: ${param0}; Param1: ${param1}`);
};

exports.echoComplexParameters = function (callback, ...otherArgs) {
    callback(null, `Received: ${JSON.stringify(otherArgs)}`);
};

exports.getComplexObject = function (callback) {
    callback(null, { stringProp: 'Hi from Node', intProp: 456, boolProp: true });
};
