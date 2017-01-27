import * as webpack from 'webpack';
type OldOrNewModule = webpack.OldModule & webpack.NewModule;

export function addReactHotModuleReplacementBabelTransform(webpackConfig: webpack.Configuration) {
    const moduleConfig = webpackConfig.module as OldOrNewModule;
    const moduleRules = moduleConfig.rules      // Webpack >= 2.1.0 beta 23
                     || moduleConfig.loaders;   // Legacy/back-compat
    if (!moduleRules) {
        return; // Unknown rules list format
    }

    moduleRules.forEach(rule => {
        // Allow rules/loaders entries to be either { loader: ... } or { use: ... }
        // Ignore other config formats (too many combinations to support them all)
        let loaderConfig =
            (rule as webpack.NewUseRule).use        // Recommended config format for Webpack 2.x
            || (rule as webpack.LoaderRule).loader; // Typical config format for Webpack 1.x
        if (!loaderConfig) {
            return; // Not a supported rule format (e.g., an array)
        }

        // Allow use/loader values to be either { loader: 'name' } or 'name'
        // We don't need to support other possible ways of specifying loaders (e.g., arrays),
        // so skip unrecognized formats.
        const loaderNameString =
            (loaderConfig as (webpack.OldLoader | webpack.NewLoader)).loader
            || (loaderConfig as string);
        if (!loaderNameString || (typeof loaderNameString !== 'string')) {
            return; // Not a supported loader format (e.g., an array)
        }

        // Find the babel-loader entry
        if (loaderNameString.match(/\bbabel-loader\b/)) {
            // If the rule is of the form { use: 'name' }, then replace it
            // with { use: { loader: 'name' }} so we can attach options
            if ((rule as webpack.NewUseRule).use && typeof loaderConfig === 'string') {
                loaderConfig = (rule as webpack.NewUseRule).use = { loader: loaderConfig };
            }

            const configItemWithOptions = typeof loaderConfig === 'string'
                ? rule          // The rule is of the form { loader: 'name' }, so put options on the rule
                : loaderConfig; // The rule is of the form { use/loader: { loader: 'name' }}, so put options on the use/loader

            // Ensure the config has an 'options' (or a legacy 'query')
            let optionsObject =
                (configItemWithOptions as webpack.NewLoader).options        // Recommended config format for Webpack 2.x
                || (configItemWithOptions as webpack.OldLoaderRule).query;  // Legacy
            if (!optionsObject) {
                // If neither options nor query was set, define a new value,
                // using the legacy format ('query') for compatibility with Webpack 1.x
                optionsObject = (configItemWithOptions as webpack.OldLoaderRule).query = {};
            }

            // Ensure Babel plugins includes 'react-transform'
            const plugins = optionsObject['plugins'] = optionsObject['plugins'] || [];
            const hasReactTransform = plugins.some(p => p && p[0] === 'react-transform');
            if (!hasReactTransform) {
                plugins.push(['react-transform', {}]);
            }

            // Ensure 'react-transform' plugin is configured to use 'react-transform-hmr'
            plugins.forEach(pluginConfig => {
                if (pluginConfig && pluginConfig[0] === 'react-transform') {
                    const pluginOpts = pluginConfig[1] = pluginConfig[1] || {};
                    const transforms = pluginOpts.transforms = pluginOpts.transforms || [];
                    const hasReactTransformHmr = transforms.some(t => t.transform === 'react-transform-hmr');
                    if (!hasReactTransformHmr) {
                        transforms.push({
                            transform: 'react-transform-hmr',
                            imports: ['react'],
                            locals: ['module'] // Important for Webpack HMR
                        });
                    }
                }
            });
        }
    });
}
