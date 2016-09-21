// Pass through the invocation to the 'aspnet-prerendering' package, verifying that it can be loaded
module.exports.renderToString = function (callback) {
    var aspNetPrerendering;
    try {
        aspNetPrerendering = require('aspnet-prerendering');
    } catch (ex) {
        // Developers sometimes have trouble with badly-configured Node installations, where it's unable
        // to find node_modules. Or they accidentally fail to deploy node_modules, or even to run 'npm install'.
        // Make sure such errors are reported back to the .NET part of the app.
        callback(
            'Prerendering failed because of an error while loading \'aspnet-prerendering\'. Error was: '
            + ex.stack
            + '\nCurrent directory is: '
            + process.cwd()
        );
        return;
    }

    return aspNetPrerendering.renderToString.apply(this, arguments);
};
