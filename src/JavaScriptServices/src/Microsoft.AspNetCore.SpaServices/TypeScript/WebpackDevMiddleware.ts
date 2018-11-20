// Pass through the invocation to the 'aspnet-webpack' package, verifying that it can be loaded
export function createWebpackDevServer(callback) {
    let aspNetWebpack;
    try {
        aspNetWebpack = require('aspnet-webpack');
    } catch (ex) {
        // Developers sometimes have trouble with badly-configured Node installations, where it's unable
        // to find node_modules. Or they accidentally fail to deploy node_modules, or even to run 'npm install'.
        // Make sure such errors are reported back to the .NET part of the app.
        callback(
            'Webpack dev middleware failed because of an error while loading \'aspnet-webpack\'. Error was: '
            + ex.stack
            + '\nCurrent directory is: '
            + process.cwd()
        );
        return;
    }

    return aspNetWebpack.createWebpackDevServer.apply(this, arguments);
}
