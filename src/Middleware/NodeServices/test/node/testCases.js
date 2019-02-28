module.exports = {
    // Function signatures follow Node conventions.
    // i.e., parameters: (callback, arg0, arg1, ... etc ...)
    // When done, functions must invoke 'callback', passing (errorInfo, result)
    // where errorInfo should be null/undefined if there was no error.
    getFixedString: callback => callback(null, 'test result'),

    raiseError: callback => callback('This is an error from Node'),
};
