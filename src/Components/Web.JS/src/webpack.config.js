const path = require('path');
const webpack = require('webpack');
const TerserJsPlugin = require("terser-webpack-plugin");
const { DuplicatesPlugin } = require("inspectpack/plugin");

module.exports = (env, args) => ({
    resolve: { 
        extensions: ['.ts', '.js'],
    },
    devtool: args.mode === 'development' ? 'source-map' : undefined,
    module: {
        rules: [{ test: /\.ts?$/, loader: 'ts-loader' }]
    },
    entry: {
        'blazor.webassembly': './Boot.WebAssembly.ts',
        'blazor.server': './Boot.Server.ts',
    },
    output: { path: path.join(__dirname, '/..', '/dist', args.mode == 'development' ? '/Debug' : '/Release'), filename: '[name].js' },
    performance: {
        maxAssetSize: 276000,
    },
    optimization: {
        sideEffects: true,
        concatenateModules: true,
        providedExports: true,
        usedExports: true,
        innerGraph: true,
        minimize: true,
        minimizer: [new TerserJsPlugin({        
            terserOptions: {
                ecma: 2019,
                compress: { 
                    passes: 3
                },
                mangle: {
                },
                module: false,
                format: {
                    ecma: 2019
                },
                keep_classnames: false,
                keep_fnames: false,
                toplevel: true
          }
        })]
    },    
    plugins: [
        new webpack.DefinePlugin({
            'process.env.NODE_DEBUG': false,
            'Platform.isNode': false
        }),
        new DuplicatesPlugin({
            emitErrors: false,
            emitHandler: undefined,
            ignoredPackages: undefined,
            verbose: false
        })
    ],
    stats: {
        //all: true,
        warnings: true,
        errors: true,
        performance: true,
        optimizationBailout: true
    }
});
