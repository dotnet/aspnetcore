var isDevBuild = process.argv.indexOf('--env.prod') < 0;
var path = require('path');
var webpack = require('webpack');
var merge = require('webpack-merge');

// Configuration in common to both client-side and server-side bundles
var sharedConfig = {
    context: __dirname,
    resolve: { extensions: [ '', '.js', '.ts' ] },
    output: {
        filename: '[name].js',
        publicPath: '/dist/' // Webpack dev middleware, if enabled, handles requests for this URL prefix
    },
    module: {
        loaders: [
            { test: /\.ts$/, include: /ClientApp/, loaders: ['ts-loader?silent=true', 'angular2-template-loader'] },
            { test: /\.html$/, loader: 'html-loader?minimize=false' },
            { test: /\.css$/, loader: 'to-string-loader!css-loader' },
            { test: /\.(png|jpg|jpeg|gif|svg)$/, loader: 'url-loader', query: { limit: 25000 } },
            { test: /\.json$/, loader: 'json-loader' }
        ]
    }
};

// Configuration for client-side bundle suitable for running in browsers
var clientBundleOutputDir = './wwwroot/dist';
var clientBundleConfig = merge(sharedConfig, {
    entry: { 'main-client': './ClientApp/boot-client.ts' },
    output: { path: path.join(__dirname, clientBundleOutputDir) },
    plugins: [
        new webpack.DllReferencePlugin({
            context: __dirname,
            manifest: require('./wwwroot/dist/vendor-manifest.json')
        })
    ].concat(isDevBuild ? [
        // Plugins that apply in development builds only
        new webpack.SourceMapDevToolPlugin({
            filename: '[file].map', // Remove this line if you prefer inline source maps
            moduleFilenameTemplate: path.relative(clientBundleOutputDir, '[resourcePath]') // Point sourcemap entries to the original file locations on disk
        })
    ] : [
        // Plugins that apply in production builds only
        new webpack.optimize.OccurenceOrderPlugin(),
        new webpack.optimize.UglifyJsPlugin()
    ])
});

// Configuration for server-side (prerendering) bundle suitable for running in Node
var serverBundleConfig = merge(sharedConfig, {
    resolve: { packageMains: ['main'] },
    entry: { 'main-server': './ClientApp/boot-server.ts' },
    plugins: [
        new webpack.DllReferencePlugin({
            context: __dirname,
            manifest: require('./ClientApp/dist/vendor-manifest.json'),
            sourceType: 'commonjs2',
            name: './vendor'
        })
    ],
    output: {
        libraryTarget: 'commonjs',
        path: path.join(__dirname, './ClientApp/dist')
    },
    target: 'node',
    devtool: 'inline-source-map'
});

module.exports = [clientBundleConfig, serverBundleConfig];
