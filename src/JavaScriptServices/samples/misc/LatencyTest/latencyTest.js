module.exports = function(callback, incomingParam1) {
    var result = 'Hello, ' + incomingParam1 + '!';
    callback(/* error */ null, result);
}
