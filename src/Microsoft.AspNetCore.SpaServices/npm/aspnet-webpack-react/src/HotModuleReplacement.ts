import * as webpack from 'webpack';

export function addReactHotModuleReplacementBabelTransform(webpackConfig: webpack.Configuration) {
    webpackConfig.module.loaders.forEach(loaderConfig => {
        if (loaderConfig.loader && loaderConfig.loader.match(/\bbabel-loader\b/)) {
            // Ensure the babel-loader options includes a 'query'
            const query = loaderConfig.query = loaderConfig.query || {};

            // Ensure Babel plugins includes 'react-transform'
            const plugins = query['plugins'] = query['plugins'] || [];
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
