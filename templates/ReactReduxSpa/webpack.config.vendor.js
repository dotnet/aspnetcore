var isDevBuild = process.argv.indexOf('--env.prod') < 0;
var path = require('path');
var webpack = require('webpack');
var ExtractTextPlugin = require('extract-text-webpack-plugin');
var merge = require('webpack-merge');
var extractCSS = new ExtractTextPlugin('vendor.css');

var sharedConfig = {
    resolve: { extensions: [ '', '.js' ] },
    module: {
        loaders: [
            { test: /\.json$/, loader: require.resolve('json-loader') },
            { test: /\.(png|woff|woff2|eot|ttf|svg)(\?|$)/, loader: 'url-loader?limit=100000' }
        ]
    },
    entry: {
        vendor: [
            'bootstrap',
            'bootstrap/dist/css/bootstrap.css',
            'domain-task',
            'event-source-polyfill',
            'react',
            'react-dom',
            'react-router',
            'react-redux',
            'redux',
            'redux-thunk',
            'react-router-redux',
            'style-loader',
            'jquery'
        ],
    },
    output: {
        publicPath: '/dist/',
        filename: '[name].js',
        library: '[name]_[hash]',
    },
    plugins: [
        new webpack.ProvidePlugin({ $: 'jquery', jQuery: 'jquery' }), // Maps these identifiers to the jQuery package (because Bootstrap expects it to be a global variable)
        new webpack.NormalModuleReplacementPlugin(/\/iconv-loader$/, require.resolve('node-noop')), // Workaround for https://github.com/andris9/encoding/issues/16
        new webpack.DefinePlugin({
            'process.env.NODE_ENV': isDevBuild ? '"development"' : '"production"'
        })
    ]
};

var clientBundleConfig = merge(sharedConfig, {
    output: { path: path.join(__dirname, 'wwwroot', 'dist') },
    module: {
        loaders: [
            { test: /\.css(\?|$)/, loader: extractCSS.extract(['css-loader']) }
        ]
    },
    plugins: [
        extractCSS,
        new webpack.DllPlugin({
            path: path.join(__dirname, 'wwwroot', 'dist', '[name]-manifest.json'),
            name: '[name]_[hash]'
        })
    ].concat(isDevBuild ? [] : [
        new webpack.optimize.OccurenceOrderPlugin(),
        new webpack.optimize.UglifyJsPlugin({ compress: { warnings: false } })
    ])
});

var serverBundleConfig = merge(sharedConfig, {
    target: 'node',
    resolve: { packageMains: ['main'] },
    output: {
        path: path.join(__dirname, 'ClientApp', 'dist'),
        libraryTarget: 'commonjs2',
    },
    module: {
        loaders: [ { test: /\.css(\?|$)/, loader: 'css-loader' } ]
    },
    entry: { vendor: ['aspnet-prerendering', 'react-dom/server'] },
    plugins: [
        new webpack.DllPlugin({
            path: path.join(__dirname, 'ClientApp', 'dist', '[name]-manifest.json'),
            name: '[name]_[hash]'
        })
    ]
});

module.exports = [clientBundleConfig, serverBundleConfig];
