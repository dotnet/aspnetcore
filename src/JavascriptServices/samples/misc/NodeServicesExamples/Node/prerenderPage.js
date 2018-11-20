var createServerRenderer = require('aspnet-prerendering').createServerRenderer;

module.exports = createServerRenderer(function(params) {
    return new Promise(function (resolve, reject) {
        var message = 'The HTML was returned by the prerendering boot function. '
            + 'The boot function received the following params:'
            + '<pre>' + JSON.stringify(params, null, 4) + '</pre>';

        resolve({
            html: '<h3>Hello, world!</h3>' + message,
            globals: { sampleData: { nodeVersion: process.version } }
        });
    });
});
