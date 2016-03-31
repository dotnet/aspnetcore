import * as connect from 'connect';
import * as webpack from 'webpack';
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

    const app = connect();
    const defaultPort = 0; // 0 means 'choose randomly'. Could allow an explicit value to be supplied instead.
    const listener = app.listen(defaultPort, () => {
        // Build the final Webpack config based on supplied options
        if (enableHotModuleReplacement) {
            // TODO: Stop assuming there's an entry point called 'main'
            webpackConfig.entry['main'].unshift('webpack-hot-middleware/client');
            webpackConfig.plugins.push(
                new webpack.HotModuleReplacementPlugin()
            );

            // Set up React HMR support if requested. This requires the 'aspnet-webpack-react' package.
            if (enableReactHotModuleReplacement) {
                let aspNetWebpackReactModule: any;
                try {
                    aspNetWebpackReactModule = require('aspnet-webpack-react');
                } catch(ex) {
                    callback('To use ReactHotModuleReplacement, you must install the NPM package \'aspnet-webpack-react\'.', null);
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
                callback('To use HotModuleReplacement, you must install the NPM package \'webpack-hot-middleware\'.', null);
                return;
            }
            app.use(webpackHotMiddlewareModule(compiler));
        }

        // Tell the ASP.NET app what addresses we're listening on, so that it can proxy requests here
        callback(null, {
            Port: listener.address().port,
            PublicPath: removeTrailingSlash(publicPath)
        });
    });
}

function removeTrailingSlash(str: string) {
    if (str.lastIndexOf('/') === str.length - 1) {
        str = str.substring(0, str.length - 1);
    }

    return str;
}
