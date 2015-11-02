var path = require('path');
var ngUniversal = require('angular2-universal-patched');
var ng = require('angular2/angular2');
var ngRouter = require('angular2/router');

module.exports = {
    renderComponent: function(callback, options) {
        // Find the component class. Use options.componentExport if specified, otherwise convert tag-name to PascalCase.
        var loadedModule = require(path.resolve(process.cwd(), options.componentModule));
        var componentExport = options.componentExport || options.tagName.replace(/(-|^)([a-z])/g, function (m1, m2, char) { return char.toUpperCase(); });
        var component = loadedModule[componentExport];
        if (!component) {
            throw new Error('The module "' + options.componentModule + '" has no export named "' + componentExport + '"');
        }

        var serverBindings = [
            ngRouter.ROUTER_BINDINGS,
            ngUniversal.HTTP_PROVIDERS,
            ng.provide(ngUniversal.BASE_URL, { useValue: options.baseUrl }),
            ngUniversal.SERVER_LOCATION_PROVIDERS
        ];

        return ngUniversal.renderToString(component, serverBindings).then(
            function(successValue) { callback(null, successValue); },
            function(errorValue) { callback(errorValue); }
        );
    }
};
