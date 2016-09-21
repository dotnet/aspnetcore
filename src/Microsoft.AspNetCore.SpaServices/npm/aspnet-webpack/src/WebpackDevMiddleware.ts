import * as connect from 'connect';
import * as webpack from 'webpack';
import * as url from 'url';
import { requireNewCopy } from './RequireNewCopy';

export interface CreateDevServerCallback {
    (error: any, result: { Port: number, PublicPath: string }): void;
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

export function createWebpackDevServer(callback: CreateDevServerCallback, optionsJson: string) {
    const options: CreateDevServerOptions = JSON.parse(optionsJson);
    const webpackConfig: webpack.Configuration = requireNewCopy(options.webpackConfigPath);
    const publicPath = (webpackConfig.output.publicPath || '').trim();
    if (!publicPath) {
        callback('To use the Webpack dev server, you must specify a value for \'publicPath\' on the \'output\' section of your webpack.config.', null);
        return;
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
        // Build the final Webpack config based on supplied options
        if (enableHotModuleReplacement) {
            // For this, we only support the key/value config format, not string or string[], since
            // those ones don't clearly indicate what the resulting bundle name will be
            const entryPoints = webpackConfig.entry;
            const isObjectStyleConfig = entryPoints
                                     && typeof entryPoints === 'object'
                                     && !(entryPoints instanceof Array);
            if (!isObjectStyleConfig) {
                callback('To use HotModuleReplacement, your webpack config must specify an \'entry\' value as a key-value object (e.g., "entry: { main: \'ClientApp/boot-client.ts\' }")', null);
                return;
            }

            // Augment all entry points so they support HMR
            Object.getOwnPropertyNames(entryPoints).forEach(entryPointName => {
                if (typeof entryPoints[entryPointName] === 'string') {
                    entryPoints[entryPointName] = ['webpack-hot-middleware/client', entryPoints[entryPointName]];
                } else {
                    entryPoints[entryPointName].unshift('webpack-hot-middleware/client');
                }
            });

            webpackConfig.plugins.push(
                new webpack.HotModuleReplacementPlugin()
            );

            // Set up React HMR support if requested. This requires the 'aspnet-webpack-react' package.
            if (enableReactHotModuleReplacement) {
                let aspNetWebpackReactModule: any;
                try {
                    aspNetWebpackReactModule = require('aspnet-webpack-react');
                } catch(ex) {
                    callback('ReactHotModuleReplacement failed because of an error while loading \'aspnet-webpack-react\'. Error was: ' + ex.stack, null);
                    return;
                }

                aspNetWebpackReactModule.addReactHotModuleReplacementBabelTransform(webpackConfig);
            }
        }

        // Attach Webpack dev middleware and optional 'hot' middleware
        const compiler = webpack(webpackConfig);
        app.use(require('webpack-dev-middleware')(compiler, {
            noInfo: true,
            publicPath: publicPath
        }));

        if (enableHotModuleReplacement) {
            let webpackHotMiddlewareModule;
            try {
                webpackHotMiddlewareModule = require('webpack-hot-middleware');
            } catch (ex) {
                callback('HotModuleReplacement failed because of an error while loading \'webpack-hot-middleware\'. Error was: ' + ex.stack, null);
                return;
            }
            app.use(webpackHotMiddlewareModule(compiler));
        }

        // Tell the ASP.NET app what addresses we're listening on, so that it can proxy requests here
        callback(null, {
            Port: listener.address().port,
            PublicPath: removeTrailingSlash(getPath(publicPath))
        });
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
