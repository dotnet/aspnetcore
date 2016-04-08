// Pass through the invocation to the 'aspnet-webpack' package, verifying that it can be loaded
module.exports.createWebpackDevServer = function (callback) {
    var aspNetWebpack;
    try {
        aspNetWebpack = require('aspnet-webpack');
    } catch (ex) {
        callback('To use webpack dev middleware, you must install the \'aspnet-webpack\' NPM package.');
        return;
    }
    
    return aspNetWebpack.createWebpackDevServer.apply(this, arguments);
};
