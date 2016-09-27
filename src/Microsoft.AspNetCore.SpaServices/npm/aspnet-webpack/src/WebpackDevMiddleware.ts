import * as connect from 'connect';
import * as webpack from 'webpack';
import * as url from 'url';
import { requireNewCopy } from './RequireNewCopy';

export type CreateDevServerResult = {
    Port: number,
    PublicPaths: string[],
    PublicPath: string // For backward compatibility with older verions of Microsoft.AspNetCore.SpaServices. Will be removed soon.
};

export interface CreateDevServerCallback {
    (error: any, result: CreateDevServerResult): void;
}

// These are the options passed by WebpackDevMiddleware.cs
interface CreateDevServerOptions {
    webpackConfigPath: string;
    suppliedOptions: DevServerOptions;
}

// These are the options configured in C# and then JSON-serialized, hence the C#-style naming
interface DevServerOptions {
    HotModuleReplacement: boolean;
    HotModuleReplacementServerPort: number;
    ReactHotModuleReplacement: boolean;
}

function attachWebpackDevMiddleware(app: any, webpackConfig: webpack.Configuration, enableHotModuleReplacement: boolean, enableReactHotModuleReplacement: boolean) {
    // Build the final Webpack config based on supplied options
    if (enableHotModuleReplacement) {
        // For this, we only support the key/value config format, not string or string[], since
        // those ones don't clearly indicate what the resulting bundle name will be
        const entryPoints = webpackConfig.entry;
        const isObjectStyleConfig = entryPoints
                                && typeof entryPoints === 'object'
                                && !(entryPoints instanceof Array);
        if (!isObjectStyleConfig) {
            throw new Error('To use HotModuleReplacement, your webpack config must specify an \'entry\' value as a key-value object (e.g., "entry: { main: \'ClientApp/boot-client.ts\' }")');
        }

        // Augment all entry points so they support HMR
        Object.getOwnPropertyNames(entryPoints).forEach(entryPointName => {
            if (typeof entryPoints[entryPointName] === 'string') {
                entryPoints[entryPointName] = ['webpack-hot-middleware/client', entryPoints[entryPointName]];
            } else {
                entryPoints[entryPointName].unshift('webpack-hot-middleware/client');
            }
        });

        webpackConfig.plugins = [].concat(webpackConfig.plugins || []); // Be sure not to mutate the original array, as it might be shared
        webpackConfig.plugins.push(
            new webpack.HotModuleReplacementPlugin()
        );

        // Set up React HMR support if requested. This requires the 'aspnet-webpack-react' package.
        if (enableReactHotModuleReplacement) {
            let aspNetWebpackReactModule: any;
            try {
                aspNetWebpackReactModule = require('aspnet-webpack-react');
            } catch(ex) {
                throw new Error('ReactHotModuleReplacement failed because of an error while loading \'aspnet-webpack-react\'. Error was: ' + ex.stack);
            }

            aspNetWebpackReactModule.addReactHotModuleReplacementBabelTransform(webpackConfig);
        }
    }

    // Attach Webpack dev middleware and optional 'hot' middleware
    const compiler = webpack(webpackConfig);
    app.use(require('webpack-dev-middleware')(compiler, {
        noInfo: true,
        publicPath: webpackConfig.output.publicPath
    }));

    if (enableHotModuleReplacement) {
        let webpackHotMiddlewareModule;
        try {
            webpackHotMiddlewareModule = require('webpack-hot-middleware');
        } catch (ex) {
            throw new Error('HotModuleReplacement failed because of an error while loading \'webpack-hot-middleware\'. Error was: ' + ex.stack);
        }
        app.use(webpackHotMiddlewareModule(compiler));
    }
}

function beginWebpackWatcher(webpackConfig: webpack.Configuration) {
    const compiler = webpack(webpackConfig);
    compiler.watch({ /* watchOptions */ }, (err, stats) => {
        // The default error reporter is fine for now, but could be customized here in the future if desired
    });
}

export function createWebpackDevServer(callback: CreateDevServerCallback, optionsJson: string) {
    const options: CreateDevServerOptions = JSON.parse(optionsJson);

    // Read the webpack config's export, and normalize it into the more general 'array of configs' format
    let webpackConfigArray: webpack.Configuration[] = requireNewCopy(options.webpackConfigPath);
    if (!(webpackConfigArray instanceof Array)) {
        webpackConfigArray = [webpackConfigArray as webpack.Configuration];
    }

    const enableHotModuleReplacement = options.suppliedOptions.HotModuleReplacement;
    const enableReactHotModuleReplacement = options.suppliedOptions.ReactHotModuleReplacement;
    if (enableReactHotModuleReplacement && !enableHotModuleReplacement) {
        callback('To use ReactHotModuleReplacement, you must also enable the HotModuleReplacement option.', null);
        return;
    }

    // The default value, 0, means 'choose randomly'
    const suggestedHMRPortOrZero = options.suppliedOptions.HotModuleReplacementServerPort || 0;

    const app = connect();
    const listener = app.listen(suggestedHMRPortOrZero, () => {
        try {
            // For each webpack config that specifies a public path, add webpack dev middleware for it
            const normalizedPublicPaths: string[] = [];
            webpackConfigArray.forEach(webpackConfig => {
                if (webpackConfig.target === 'node') {
                    // For configs that target Node, it's meaningless to set up an HTTP listener, since
                    // Node isn't going to load those modules over HTTP anyway. It just loads them directly
                    // from disk. So the most relevant thing we can do with such configs is just write
                    // updated builds to disk, just like "webpack --watch".
                    beginWebpackWatcher(webpackConfig);
                } else {
                    // For configs that target browsers, we can set up an HTTP listener, and dynamically
                    // modify the config to enable HMR etc. This just requires that we have a publicPath.
                    const publicPath = (webpackConfig.output.publicPath || '').trim();
                    if (!publicPath) {
                        throw new Error('To use the Webpack dev server, you must specify a value for \'publicPath\' on the \'output\' section of your webpack config (for any configuration that targets browsers)');
                    }
                    normalizedPublicPaths.push(removeTrailingSlash(publicPath));
                    attachWebpackDevMiddleware(app, webpackConfig, enableHotModuleReplacement, enableReactHotModuleReplacement);
                }
            });

            // Tell the ASP.NET app what addresses we're listening on, so that it can proxy requests here
            callback(null, {
                Port: listener.address().port,
                PublicPaths: normalizedPublicPaths,

                // For back-compatibility with older versions of Microsoft.AspNetCore.SpaServices, in the case where
                // you have exactly one webpackConfigArray entry. This will be removed soon.
                PublicPath: normalizedPublicPaths[0]
            });
        } catch (ex) {
            callback(ex.stack, null);
        }
    });
}

function removeTrailingSlash(str: string) {
    if (str.lastIndexOf('/') === str.length - 1) {
        str = str.substring(0, str.length - 1);
    }

    return str;
}

function getPath(publicPath: string) {
    return url.parse(publicPath).path;
}
