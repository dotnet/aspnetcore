module.exports = function (callback, message) {
    callback(null, `Hello from the default export. You passed: ${message}`);
};
