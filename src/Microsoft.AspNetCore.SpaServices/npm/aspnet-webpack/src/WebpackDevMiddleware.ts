import * as connect from 'connect';
import * as webpack from 'webpack';
import * as url from 'url';
import * as fs from 'fs';
import * as path from 'path';
import * as querystring from 'querystring';
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
    hotModuleReplacementEndpointUrl: string;
}

type StringMap<T> = [(key: string) => T];

// These are the options configured in C# and then JSON-serialized, hence the C#-style naming
interface DevServerOptions {
    HotModuleReplacement: boolean;
    HotModuleReplacementServerPort: number;
    HotModuleReplacementClientOptions: StringMap<string>;
    ReactHotModuleReplacement: boolean;
}

// We support these three kinds of webpack.config.js export. We don't currently support exported promises
// (though we might be able to add that in the future, if there's a need).
type WebpackConfigOrArray = webpack.Configuration | webpack.Configuration[];
interface WebpackConfigFunc {
    (env?: any): WebpackConfigOrArray;
}
type WebpackConfigFileExport = WebpackConfigOrArray | WebpackConfigFunc;

function attachWebpackDevMiddleware(app: any, webpackConfig: webpack.Configuration, enableHotModuleReplacement: boolean, enableReactHotModuleReplacement: boolean, hmrClientOptions: StringMap<string>, hmrServerEndpoint: string) {
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

        // Augment all entry points so they support HMR (unless they already do)
        Object.getOwnPropertyNames(entryPoints).forEach(entryPointName => {
            const webpackHotMiddlewareEntryPoint = 'webpack-hot-middleware/client';
            const webpackHotMiddlewareOptions = '?' + querystring.stringify(hmrClientOptions);
            if (typeof entryPoints[entryPointName] === 'string') {
                entryPoints[entryPointName] = [webpackHotMiddlewareEntryPoint + webpackHotMiddlewareOptions, entryPoints[entryPointName]];
            } else if (firstIndexOfStringStartingWith(entryPoints[entryPointName], webpackHotMiddlewareEntryPoint) < 0) {
                entryPoints[entryPointName].unshift(webpackHotMiddlewareEntryPoint + webpackHotMiddlewareOptions);
            }

            // Now also inject eventsource polyfill so this can work on IE/Edge (unless it's already there)
            // To avoid this being a breaking change for everyone who uses aspnet-webpack, we only do this if you've
            // referenced event-source-polyfill in your package.json. Note that having event-source-polyfill available
            // on the server in node_modules doesn't imply that you've also included it in your client-side bundle,
            // but the converse is true (if it's not in node_modules, then you obviously aren't trying to use it at
            // all, so it would definitely not work to take a dependency on it).
            const eventSourcePolyfillEntryPoint = 'event-source-polyfill';
            if (npmModuleIsPresent(eventSourcePolyfillEntryPoint)) {
                const entryPointsArray: string[] = entryPoints[entryPointName]; // We know by now that it's an array, because if it wasn't, we already wrapped it in one
                if (entryPointsArray.indexOf(eventSourcePolyfillEntryPoint) < 0) {
                    const webpackHmrIndex = firstIndexOfStringStartingWith(entryPointsArray, webpackHotMiddlewareEntryPoint);
                    if (webpackHmrIndex < 0) {
                        // This should not be possible, since we just added it if it was missing
                        throw new Error('Cannot find ' + webpackHotMiddlewareEntryPoint + ' in entry points array: ' + entryPointsArray);
                    }

                    // Insert the polyfill just before the HMR entrypoint
                    entryPointsArray.splice(webpackHmrIndex, 0, eventSourcePolyfillEntryPoint);
                }
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
        publicPath: webpackConfig.output.publicPath,
        watchOptions: webpackConfig.watchOptions
    }));

    // After each compilation completes, copy the in-memory filesystem to disk.
    // This is needed because the debuggers in both VS and VS Code assume that they'll be able to find
    // the compiled files on the local disk (though it would be better if they got the source file from
    // the browser they are debugging, which would be more correct and make this workaround unnecessary).
    // Without this, Webpack plugins like HMR that dynamically modify the compiled output in the dev
    // middleware's in-memory filesystem only (and not on disk) would confuse the debugger, because the
    // file on disk wouldn't match the file served to the browser, and the source map line numbers wouldn't
    // match up. Breakpoints would either not be hit, or would hit the wrong lines.
    (compiler as any).plugin('done', stats => {
        copyRecursiveToRealFsSync(compiler.outputFileSystem, '/', [/\.hot-update\.(js|json|js\.map)$/]);
    });

    if (enableHotModuleReplacement) {
        let webpackHotMiddlewareModule;
        try {
            webpackHotMiddlewareModule = require('webpack-hot-middleware');
        } catch (ex) {
            throw new Error('HotModuleReplacement failed because of an error while loading \'webpack-hot-middleware\'. Error was: ' + ex.stack);
        }
        app.use(workaroundIISExpressEventStreamFlushingIssue(hmrServerEndpoint));
        app.use(webpackHotMiddlewareModule(compiler, {
            path: hmrServerEndpoint
        }));
    }
}

function workaroundIISExpressEventStreamFlushingIssue(path: string): connect.NextHandleFunction {
    // IIS Express makes HMR seem very slow, because when it's reverse-proxying an EventStream response
    // from Kestrel, it doesn't pass through the lines to the browser immediately, even if you're calling
    // response.Flush (or equivalent) in your ASP.NET Core code. For some reason, it waits until the following
    // line is sent. By default, that wouldn't be until the next HMR heartbeat, which can be up to 5 seconds later.
    // In effect, it looks as if your code is taking 5 seconds longer to compile than it really does.
    //
    // As a workaround, this connect middleware intercepts requests to the HMR endpoint, and modifies the response
    // stream so that all EventStream 'data' lines are immediately followed with a further blank line. This is
    // harmless in non-IIS-Express cases, because it's OK to have extra blank lines in an EventStream response.
    // The implementation is simplistic - rather than using a true stream reader, we just patch the 'write'
    // method. This relies on webpack's HMR code always writing complete EventStream messages with a single
    // 'write' call. That works fine today, but if webpack's HMR code was changed, this workaround might have
    // to be updated.
    const eventStreamLineStart = /^data\:/;
    return (req, res, next) => {
        // We only want to interfere with requests to the HMR endpoint, so check this request matches
        const urlMatchesPath = (req.url === path) || (req.url.split('?', 1)[0] === path);
        if (urlMatchesPath) {
            const origWrite = res.write;
            res.write = function (chunk) {
                const result = origWrite.apply(this, arguments);

                // We only want to interfere with actual EventStream data lines, so check it is one
                if (typeof (chunk) === 'string') {
                    if (eventStreamLineStart.test(chunk) && chunk.charAt(chunk.length - 1) === '\n') {
                        origWrite.call(this, '\n\n');
                    }
                }

                return result;
            }
        }

        return next();
    };
}

function copyRecursiveToRealFsSync(from: typeof fs, rootDir: string, exclude: RegExp[]) {
    from.readdirSync(rootDir).forEach(filename => {
        const fullPath = pathJoinSafe(rootDir, filename);
        const shouldExclude = exclude.filter(re => re.test(fullPath)).length > 0;
        if (!shouldExclude) {
            const fileStat = from.statSync(fullPath);
            if (fileStat.isFile()) {
                const fileBuf = from.readFileSync(fullPath);
                fs.writeFileSync(fullPath, fileBuf);
            } else if (fileStat.isDirectory()) {
                if (!fs.existsSync(fullPath)) {
                    fs.mkdirSync(fullPath);
                }
                copyRecursiveToRealFsSync(from, fullPath, exclude);
            }
        }
    });
}

function pathJoinSafe(rootPath: string, filePath: string) {
    // On Windows, MemoryFileSystem's readdirSync output produces directory entries like 'C:'
    // which then trigger errors if you call statSync for them. Avoid this by detecting drive
    // names at the root, and adding a backslash (so 'C:' becomes 'C:\', which works).
    if (rootPath === '/' && path.sep === '\\' && filePath.match(/^[a-z0-9]+\:$/i)) {
        return filePath + '\\';
    } else {
        return path.join(rootPath, filePath);
    }
}

function beginWebpackWatcher(webpackConfig: webpack.Configuration) {
    const compiler = webpack(webpackConfig);
    compiler.watch(webpackConfig.watchOptions || {}, (err, stats) => {
        // The default error reporter is fine for now, but could be customized here in the future if desired
    });
}

export function createWebpackDevServer(callback: CreateDevServerCallback, optionsJson: string) {
    const options: CreateDevServerOptions = JSON.parse(optionsJson);

    // Read the webpack config's export, and normalize it into the more general 'array of configs' format
    let webpackConfigExport: WebpackConfigFileExport = requireNewCopy(options.webpackConfigPath);
    if (webpackConfigExport instanceof Function) {
        // If you export a function, we'll call it with an undefined 'env' arg, since we have nothing else
        // to pass. This is the same as what the webpack CLI tool does if you specify no '--env.x' values.
        // In the future, we could add support for configuring the 'env' param in Startup.cs. But right
        // now, it's not clear that people will want to do that (and they can always make up their own
        // default env values in their webpack.config.js).
        webpackConfigExport = webpackConfigExport();
    }
    const webpackConfigArray = webpackConfigExport instanceof Array ? webpackConfigExport : [webpackConfigExport];

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

                    // Newer versions of Microsoft.AspNetCore.SpaServices will explicitly pass an HMR endpoint URL
                    // (because it's relative to the app's URL space root, which the client doesn't otherwise know).
                    // For back-compatibility, fall back on connecting directly to the underlying HMR server (though
                    // that won't work if the app is hosted on HTTPS because of the mixed-content rule, and we can't
                    // run the HMR server itself on HTTPS because in general it has no valid cert).
                    const hmrClientEndpoint = options.hotModuleReplacementEndpointUrl   // The URL that we'll proxy (e.g., /__asp_webpack_hmr)
                        || `http://localhost:${listener.address().port}/__webpack_hmr`; // Fall back on absolute URL to bypass proxying
                    const hmrServerEndpoint = options.hotModuleReplacementEndpointUrl
                        || '/__webpack_hmr';                                            // URL is relative to webpack dev server root

                    // We always overwrite the 'path' option as it needs to match what the .NET side is expecting
                    const hmrClientOptions = options.suppliedOptions.HotModuleReplacementClientOptions || <StringMap<string>>{};
                    hmrClientOptions['path'] = hmrClientEndpoint;

                    attachWebpackDevMiddleware(app, webpackConfig, enableHotModuleReplacement, enableReactHotModuleReplacement, hmrClientOptions, hmrServerEndpoint);
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

function firstIndexOfStringStartingWith(array: string[], prefixToFind: string) {
    for (let index = 0; index < array.length; index++) {
        const candidate = array[index];
        if ((typeof candidate === 'string') && (candidate.substring(0, prefixToFind.length) === prefixToFind)) {
            return index;
        }
    }

    return -1; // Not found
}

function npmModuleIsPresent(moduleName: string) {
    try {
        require.resolve(moduleName);
        return true;
    } catch (ex) {
        return false;
    }
}
