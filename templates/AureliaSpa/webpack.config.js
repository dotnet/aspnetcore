var isDevBuild = process.argv.indexOf('--env.prod') < 0;
var path = require('path');
var webpack = require('webpack');
var AureliaWebpackPlugin = require('aurelia-webpack-plugin');

module.exports = {
     resolve: { extensions: [ '.js', '.ts' ] },
     devtool: isDevBuild ? 'inline-source-map' : null,
     entry: {
        'app': [], // <-- this array will be filled by the aurelia-webpack-plugin
        'aurelia-modules': [
            'aurelia-bootstrapper-webpack',
            'aurelia-event-aggregator',
            'aurelia-fetch-client',
            'aurelia-framework',
            'aurelia-history-browser',
            'aurelia-loader-webpack',
            'aurelia-logging-console',
            'aurelia-pal-browser',
            'aurelia-polyfills',
            'aurelia-route-recognizer',
            'aurelia-router',
            'aurelia-templating-binding',
            'aurelia-templating-resources',
            'aurelia-templating-router'
        ]
    },
    output: {
        path: path.resolve('./wwwroot/dist'),
        publicPath: '/dist',
        filename: '[name]-bundle.js'
    },
    module: {
        loaders: [
            { test: /\.ts$/, include: /ClientApp/, loader: 'ts', query: {silent: true} },
            { test: /\.html$/, loader: 'html-loader' },
            { test: /\.css$/, loaders: ['style-loader', 'css-loader'] },
            { test: /\.(png|woff|woff2|eot|ttf|svg)$/, loader: 'url-loader?limit=100000' }
        ]
    },
    plugins: [
        new webpack.ProvidePlugin({ $: 'jquery', jQuery: 'jquery' }), // because Bootstrap expects $ and jQuery to be globals
        new AureliaWebpackPlugin({
            root: path.resolve('./'),
            src: path.resolve('./ClientApp'),
            baseUrl: '/'
        }),
        new webpack.optimize.CommonsChunkPlugin({
            name: ['aurelia-modules']
        }),
    ].concat(isDevBuild ? [] : [
        // Plugins that apply in production builds only
        new webpack.optimize.UglifyJsPlugin()
    ])
};
