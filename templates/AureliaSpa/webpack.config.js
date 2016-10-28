var isDevBuild = process.argv.indexOf('--env.prod') < 0;
var path = require('path');
var webpack = require('webpack');
var AureliaWebpackPlugin = require('aurelia-webpack-plugin');
var srcDir = path.resolve('./ClientApp');
var rootDir = path.resolve();
var outDir = path.resolve('./wwwroot/dist');
var baseUrl = '/';
var project = require('./package.json');
var aureliaModules = Object.keys(project.dependencies).filter(dep => dep.startsWith('aurelia-'));

// Configuration for client-side bundle suitable for running in browsers
var clientBundleConfig = {
     resolve: { extensions: [ '.js', '.ts' ] },
     devtool: isDevBuild ? 'inline-source-map' : null,
     entry: {
        'app': [], // <-- this array will be filled by the aurelia-webpack-plugin
        'aurelia-modules': aureliaModules
    },
    output: {
        path: outDir,
        publicPath: '/dist',
        filename: '[name]-bundle.js'
    },
    module: {
        loaders: [
            {
                test: /\.ts$/,
                include: /ClientApp/,
                loader: 'ts',
                query: {silent: true}
            }, {
                test: /\.html$/,
                exclude: /index\.html$/,
                loader: 'html-loader'
            }, {
                test: /\.css$/,
                loaders: ['style-loader', 'css-loader']
            }, {
                test: /\.(png|woff|woff2|eot|ttf|svg)$/,
                loader: 'url-loader?limit=100000'
            }
        ]
    },
    plugins: [
        new webpack.ProvidePlugin({
            $: 'jquery', // because 'bootstrap' by Twitter depends on this
            jQuery: 'jquery'
        }),
        new AureliaWebpackPlugin({
            root: rootDir,
            src: srcDir,
            baseUrl: baseUrl
        }),
        new webpack.optimize.CommonsChunkPlugin({
            name: ['aurelia-modules']
        }),
    ].concat(isDevBuild ? [] : [
        // Plugins that apply in production builds only
        new webpack.optimize.UglifyJsPlugin()
    ])
};

module.exports = [clientBundleConfig];
