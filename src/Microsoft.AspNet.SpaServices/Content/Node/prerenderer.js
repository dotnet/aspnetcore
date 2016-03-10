// Pass through the invocation to the 'aspnet-prerendering' package, verifying that it can be loaded
module.exports.renderToString = function (callback) {
    var aspNetPrerendering;
    try {
        aspNetPrerendering = require('aspnet-prerendering');
    } catch (ex) {
        callback('To use prerendering, you must install the \'aspnet-prerendering\' NPM package.');
        return;
    }
    
    return aspNetPrerendering.renderToString.apply(this, arguments);
};
