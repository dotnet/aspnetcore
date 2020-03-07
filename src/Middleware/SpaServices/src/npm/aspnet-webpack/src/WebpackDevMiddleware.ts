import * as connect from 'connect';
import * as webpack from 'webpack';
import * as url from 'url';
import * as fs from 'fs';
import * as path from 'path';
import * as querystring from 'querystring';
import { requireNewCopy } from './RequireNewCopy';
import { hasSufficientPermissions } from './WebpackTestPermissions';

export type CreateDevServerResult = {
    Port: number,
    PublicPaths: string[]
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

type EsModuleExports<T> = { __esModule: true, default: T };
type StringMap<T> = [(key: string) => T];

// These are the options configured in C# and then JSON-serialized, hence the C#-style naming
interface DevServerOptions {
    HotModuleReplacement: boolean;
    HotModuleReplacementServerPort: number;
    HotModuleReplacementClientOptions: StringMap<string>;
    ReactHotModuleReplacement: boolean;
    EnvParam: any;
}

// Interface as defined in es6-promise
interface Thenable<T> {
    then<U>(onFulfilled?: (value: T) => U | Thenable<U>, onRejected?: (error: any) => U | Thenable<U>): Thenable<U>;
    then<U>(onFulfilled?: (value: T) => U | Thenable<U>, onRejected?: (error: any) => void): Thenable<U>;
}

// We support these four kinds of webpack.config.js export
type WebpackConfigOrArray = webpack.Configuration | webpack.Configuration[];
type WebpackConfigOrArrayOrThenable = WebpackConfigOrArray | Thenable<WebpackConfigOrArray>;
interface WebpackConfigFunc {
    (env?: any): WebpackConfigOrArrayOrThenable;
}
type WebpackConfigExport = WebpackConfigOrArrayOrThenable | WebpackConfigFunc;
type WebpackConfigModuleExports = WebpackConfigExport | EsModuleExports<WebpackConfigExport>;

function isThenable<T>(obj: any): obj is Thenable<T> {
    return obj && typeof (<Thenable<any>>obj).then === 'function';
}

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
        stats: webpackConfig.stats,
        publicPath: ensureLeadingSlash(webpackConfig.output.publicPath),
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
    const copy = stats => copyRecursiveToRealFsSync(compiler.outputFileSystem, '/', [/\.hot-update\.(js|json|js\.map)$/]);
    if (compiler.hooks) {
        compiler.hooks.done.tap('aspnet-webpack', copy);
    } else {
        compiler.plugin('done', copy);
    }

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

function ensureLeadingSlash(value: string) {
    if (value !== null && value.substring(0, 1) !== '/') {
        value = '/' + value;
    }

    return value;
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

    // Enable TypeScript loading if the webpack config is authored in TypeScript
    if (path.extname(options.webpackConfigPath) === '.ts') {
        try {
            require('ts-node/register');
        } catch (ex) {
            throw new Error('Error while attempting to enable support for Webpack config file written in TypeScript. Make sure your project depends on the "ts-node" NPM package. The underlying error was: ' + ex.stack);
        }
    }

    // See the large comment in WebpackTestPermissions.ts for details about this
    if (!hasSufficientPermissions()) {
        console.log('WARNING: Webpack dev middleware is not enabled because the server process does not have sufficient permissions. You should either remove the UseWebpackDevMiddleware call from your code, or to make it work, give your server process user account permission to write to your application directory and to read all ancestor-level directories.');
        callback(null, {
            Port: 0,
            PublicPaths: []
        });
        return;
    }

    // Read the webpack config's export, and normalize it into the more general 'array of configs' format
    const webpackConfigModuleExports: WebpackConfigModuleExports = requireNewCopy(options.webpackConfigPath);
    let webpackConfigExport = (webpackConfigModuleExports as EsModuleExports<{}>).__esModule === true
        ? (webpackConfigModuleExports as EsModuleExports<WebpackConfigExport>).default
        : (webpackConfigModuleExports as WebpackConfigExport);

    if (webpackConfigExport instanceof Function) {
        // If you export a function, then Webpack convention is that it takes zero or one param,
        // and that param is called `env` and reflects the `--env.*` args you can specify on
        // the command line (e.g., `--env.prod`).
        // When invoking it via WebpackDevMiddleware, we let you configure the `env` param in
        // your Startup.cs.
        webpackConfigExport = webpackConfigExport(options.suppliedOptions.EnvParam);
    }

    const webpackConfigThenable = isThenable<WebpackConfigOrArray>(webpackConfigExport)
        ? webpackConfigExport
        : { then: callback => callback(webpackConfigExport) } as Thenable<WebpackConfigOrArray>;

    webpackConfigThenable.then(webpackConfigResolved => {
        const webpackConfigArray = webpackConfigResolved instanceof Array ? webpackConfigResolved : [webpackConfigResolved];

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
                        const publicPathNoTrailingSlash = removeTrailingSlash(publicPath);
                        normalizedPublicPaths.push(publicPathNoTrailingSlash);

                        // This is the URL the client will connect to, except that since it's a relative URL
                        // (no leading slash), Webpack will resolve it against the runtime <base href> URL
                        // plus it also adds the publicPath
                        const hmrClientEndpoint = removeLeadingSlash(options.hotModuleReplacementEndpointUrl);

                        // This is the URL inside the Webpack middleware Node server that we'll proxy to.
                        // We have to prefix with the public path because Webpack will add the publicPath
                        // when it resolves hmrClientEndpoint as a relative URL.
                        const hmrServerEndpoint = ensureLeadingSlash(publicPathNoTrailingSlash + options.hotModuleReplacementEndpointUrl);

                        // We always overwrite the 'path' option as it needs to match what the .NET side is expecting
                        const hmrClientOptions = options.suppliedOptions.HotModuleReplacementClientOptions || <StringMap<string>>{};
                        hmrClientOptions['path'] = hmrClientEndpoint;

                        const dynamicPublicPathKey = 'dynamicPublicPath';
                        if (!(dynamicPublicPathKey in hmrClientOptions)) {
                            // dynamicPublicPath default to true, so we can work with nonempty pathbases (virtual directories)
                            hmrClientOptions[dynamicPublicPathKey] = true;
                        } else {
                            // ... but you can set it to any other value explicitly if you want (e.g., false)
                            hmrClientOptions[dynamicPublicPathKey] = JSON.parse(hmrClientOptions[dynamicPublicPathKey]);
                        }

                        attachWebpackDevMiddleware(app, webpackConfig, enableHotModuleReplacement, enableReactHotModuleReplacement, hmrClientOptions, hmrServerEndpoint);
                    }
                });

                // Tell the ASP.NET app what addresses we're listening on, so that it can proxy requests here
                callback(null, {
                    Port: listener.address().port,
                    PublicPaths: normalizedPublicPaths
                });
            } catch (ex) {
                callback(ex.stack, null);
            }
        });
        },
        err => callback(err.stack, null)
    );
}

function removeLeadingSlash(str: string) {
    if (str.indexOf('/') === 0) {
        str = str.substring(1);
    }

    return str;
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
