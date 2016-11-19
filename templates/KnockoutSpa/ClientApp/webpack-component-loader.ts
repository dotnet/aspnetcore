import * as ko from 'knockout';

// This Knockout component loader integrates with Webpack's lazy-loaded bundle feature.
// Having this means you can optionally declare components as follows:
//   ko.components.register('my-component', require('bundle-loader?lazy!../some-path-to-a-js-or-ts-module'));
// ... and then it will be loaded on demand instead of being loaded up front.
ko.components.loaders.unshift({
    loadComponent: (name, componentConfig, callback) => {
        if (typeof componentConfig === 'function') {
            // It's a lazy-loaded Webpack bundle
            (componentConfig as any)(loadedModule => {
                // Handle TypeScript-style default exports
                if (loadedModule.__esModule && loadedModule.default) {
                    loadedModule = loadedModule.default;
                }

                // Pass the loaded module to KO's default loader
                ko.components.defaultLoader.loadComponent(name, loadedModule, callback);
            });
        } else {
            // It's something else - let another component loader handle it
            callback(null);
        }
    }
});
