var express = require('express');
var webpack = require('webpack');
var defaultPort = 0; // 0 means 'choose randomly'. Could allow an explicit value to be supplied instead.

module.exports = {
    createWebpackDevServer: function(callback, optionsJson) {
        var options = JSON.parse(optionsJson);
        var webpackConfig = require(options.webpackConfigPath);
        var publicPath = (webpackConfig.output.publicPath || '').trim();
        if (!publicPath) {
            throw new Error('To use the Webpack dev server, you must specify a value for \'publicPath\' on the \'output\' section of your webpack.config.');
        }
        
        var enableHotModuleReplacement = options.suppliedOptions.HotModuleReplacement;
        var enableReactHotModuleReplacement = options.suppliedOptions.ReactHotModuleReplacement;
 
        var app = new express();
        var listener = app.listen(defaultPort, function() {
            // Build the final Webpack config based on supplied options
            if (enableHotModuleReplacement) {
                webpackConfig.entry.main.unshift('webpack-hot-middleware/client');
                webpackConfig.plugins.push(
                    new webpack.HotModuleReplacementPlugin()
                );
                
                if (enableReactHotModuleReplacement) {
                    addReactHotModuleReplacementBabelTransform(webpackConfig);                    
                }
            }

            // Attach Webpack dev middleware and optional 'hot' middleware
            var compiler = webpack(webpackConfig);
            app.use(require('webpack-dev-middleware')(compiler, {
                noInfo: true,
                publicPath: publicPath
            }));

            if (enableHotModuleReplacement) {
                app.use(require('webpack-hot-middleware')(compiler));
            }

            // Tell the ASP.NET app what addresses we're listening on, so that it can proxy requests here
            callback(null, {
                Port: listener.address().port,
                PublicPath: removeTrailingSlash(publicPath)
            });
        });
    }
};

function addReactHotModuleReplacementBabelTransform(webpackConfig) {
    webpackConfig.module.loaders.forEach(function(loaderConfig) {
        if (loaderConfig.loader && loaderConfig.loader.match(/\bbabel-loader\b/)) {
            // Ensure the babel-loader options includes a 'query'
            var query = loaderConfig.query = loaderConfig.query || {};
            
            // Ensure Babel plugins includes 'react-transform'
            var plugins = query.plugins = query.plugins || [];
            if (!plugins.some(function(pluginConfig) {
                return pluginConfig && pluginConfig[0] === 'react-transform';
            })) {
                plugins.push(['react-transform', {}]);
            }
            
            // Ensure 'react-transform' plugin is configured to use 'react-transform-hmr'
            plugins.forEach(function(pluginConfig) {
                if (pluginConfig && pluginConfig[0] === 'react-transform') {
                    var pluginOpts = pluginConfig[1] = pluginConfig[1] || {};
                    var transforms = pluginOpts.transforms = pluginOpts.transforms || [];
                    if (!transforms.some(function(transform) {
                        return transform.transform === 'react-transform-hmr';
                    })) {
                        transforms.push({
                            transform: "react-transform-hmr",
                            imports: ["react"],
                            locals: ["module"] // Important for Webpack HMR
                        });
                    }
                }
            });
        }
    });
}

function removeTrailingSlash(str) {
    if (str.lastIndexOf('/') === str.length - 1) {
        str = str.substring(0, str.length - 1);
    }
    
    return str;
}
