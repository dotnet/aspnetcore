// When you're using Webpack, it's often convenient to be able to require modules from regular JavaScript
// and have them transformed by Webpack. This is especially useful when doing ASP.NET server-side prerendering,
// because it means your boot module can use whatever source language you like (e.g., TypeScript), and means
// that your loader plugins (e.g., require('./mystyles.less')) work in exactly the same way on the server as
// on the client.
import 'es6-promise';
import * as path from 'path';
import * as webpack from 'webpack';
import { requireNewCopy } from './RequireNewCopy';

// Strange import syntax to work around https://github.com/Microsoft/TypeScript/issues/2719
import { requirefromstring } from './typings/require-from-string';
import { memoryfs } from './typings/memory-fs';
const nodeExternals = require('webpack-node-externals');
const requireFromString = require('require-from-string') as typeof requirefromstring.requireFromString;
const MemoryFS = require('memory-fs') as typeof memoryfs.MemoryFS;

// Ensure we only go through the compile process once per [config, module] pair
const loadViaWebpackPromisesCache: { [key: string]: any } = {};

export interface LoadViaWebpackCallback<T> {
    (error: any, result: T): void;
}

export function loadViaWebpack<T>(webpackConfigPath: string, modulePath: string, callback: LoadViaWebpackCallback<T>) {
    const cacheKey = JSON.stringify(webpackConfigPath) + JSON.stringify(modulePath);
    if (!(cacheKey in loadViaWebpackPromisesCache)) {
        loadViaWebpackPromisesCache[cacheKey] = loadViaWebpackNoCache(webpackConfigPath, modulePath);
    }
    loadViaWebpackPromisesCache[cacheKey].then(result => {
        callback(null, result);
    }, error => {
        callback(error, null);
    })
}

function setExtension(filePath: string, newExtension: string) {
    const oldExtensionIfAny = path.extname(filePath);
    const basenameWithoutExtension = path.basename(filePath, oldExtensionIfAny);
    return path.join(path.dirname(filePath), basenameWithoutExtension) + newExtension;
}

function loadViaWebpackNoCache<T>(webpackConfigPath: string, modulePath: string) {
    return new Promise<T>((resolve, reject) => {
        // Load the Webpack config and make alterations needed for loading the output into Node
        const webpackConfig: webpack.Configuration = requireNewCopy(webpackConfigPath);
        webpackConfig.entry = modulePath;
        webpackConfig.target = 'node';

        // Make sure we preserve the 'path' and 'publicPath' config values if specified, as these
        // can affect the build output (e.g., when using 'file' loader, the publicPath value gets
        // set as a prefix on output paths).
        webpackConfig.output = webpackConfig.output || {};
        webpackConfig.output.path = webpackConfig.output.path || '/';
        webpackConfig.output.filename = 'webpack-output.js';
        webpackConfig.output.libraryTarget = 'commonjs';
        const outputVirtualPath = path.join(webpackConfig.output.path, webpackConfig.output.filename);

        // In Node, we want any JavaScript modules under /node_modules/ to be loaded natively and not bundled into the
        // output (partly because it's faster, but also because otherwise there'd be different instances of modules
        // depending on how they were loaded, which could lead to errors).
        // ---
        // NOTE: We have to use webpack-node-externals rather than webpack-externals-plugin because
        // webpack-externals-plugin doesn't correctly resolve relative paths, which means you can't
        // use css-loader, since tries to require('./../../node_modules/css-loader/lib/css-base.js') (see #132)
        // ---
        // So, ensure that webpackConfig.externals is an array, and push WebpackNodeExternals into it:
        let externalsArray: any[] = (webpackConfig.externals as any[]) || [];
        if (!(externalsArray instanceof Array)) {
            externalsArray = [externalsArray];
        }
        webpackConfig.externals = externalsArray;
        externalsArray.push(nodeExternals({
            // However, we do *not* want to treat non-JS files under /node_modules/ as externals (i.e., things
            // that should be loaded via regular CommonJS 'require' statements). For example, if you reference
            // a .css file inside an NPM module (e.g., require('somepackage/somefile.css')), then we do need to
            // load that via Webpack rather than as a regular CommonJS module.
            //
            // So, configure webpack-externals-plugin to 'whitelist' (i.e., not treat as external) any file
            // that has an extension other than .js. Also, since some libraries such as font-awesome refer to
            // their own files with cache-busting querystrings (e.g., (url('./something.css?v=4.1.2'))), we
            // need to treat '?' as an alternative 'end of filename' marker.
            //
            // The complex, awkward regex can be eliminated once webpack-externals-plugin merges
            // https://github.com/liady/webpack-node-externals/pull/12
            //
            // This regex looks for at least one dot character that is *not* followed by "js<end-or-questionmark>", but
            // is followed by some series of non-dot characters followed by <end-or-questionmark>:
            whitelist: [/\.(?!js(\?|$))([^.]+(\?|$))/]
        }));

        // The CommonsChunkPlugin is not compatible with a CommonJS environment like Node, nor is it needed in that case
        const ChunkPlugin = webpack.optimize['CommonsChunkPlugin'];
        if (ChunkPlugin !== undefined) {
            webpackConfig.plugins = webpackConfig.plugins.filter(plugin => {
                return !(plugin instanceof ChunkPlugin);
            });
        }

        // The typical use case for DllReferencePlugin is for referencing vendor modules. In a Node
        // environment, it doesn't make sense to load them from a DLL bundle, nor would that even
        // work, because then you'd get different module instances depending on whether a module
        // was referenced via a normal CommonJS 'require' or via Webpack. So just remove any
        // DllReferencePlugin from the config.
        // If someone wanted to load their own DLL modules (not an NPM module) via DllReferencePlugin,
        // that scenario is not supported today. We would have to add some extra option to the
        // asp-prerender tag helper to let you specify a list of DLL bundles that should be evaluated
        // in this context. But even then you'd need special DLL builds for the Node environment so that
        // external dependencies were fetched via CommonJS requires, so it's unclear how that could work.
        // The ultimate escape hatch here is just prebuilding your code as part of the application build
        // and *not* using asp-prerender-webpack-config at all, then you can do anything you want.
        webpackConfig.plugins = webpackConfig.plugins.filter(plugin => {
            // DllReferencePlugin is missing from webpack.d.ts for some reason, hence referencing it
            // as a key-value object property
            return !(plugin instanceof webpack['DllReferencePlugin']);
        });

        // Create a compiler instance that stores its output in memory, then load its output
        const compiler = webpack(webpackConfig);
        compiler.outputFileSystem = new MemoryFS();
        compiler.run((err, stats) => {
            if (err) {
                reject(err);
            } else {
                // We're in a callback, so need an explicit try/catch to propagate any errors up the promise chain
                try {
                    if (stats.hasErrors()) {
                        throw new Error('Webpack compilation reported errors. Compiler output follows: '
                            + stats.toString({ chunks: false }));
                    }

                    // The dynamically-built module will only appear in node-inspector if it has some nonempty
                    // file path. The following value is arbitrary (since there's no real compiled file on disk)
                    // but is sufficient to enable debugging.
                    const fakeModulePath = setExtension(modulePath, '.js');

                    const fileContent = compiler.outputFileSystem.readFileSync(outputVirtualPath, 'utf8');
                    const moduleInstance = requireFromString<T>(fileContent, fakeModulePath);
                    resolve(moduleInstance);
                } catch(ex) {
                    reject(ex);
                }
            }
        });
    });
}
