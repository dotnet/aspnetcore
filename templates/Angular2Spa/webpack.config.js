var path = require('path');
var webpack = require('webpack');
var merge = require('extendify')({ isDeep: true, arrays: 'concat' });
var ExtractTextPlugin = require('extract-text-webpack-plugin');
var extractSiteCSS = new ExtractTextPlugin('styles.css');
var extractVendorCSS = new ExtractTextPlugin('vendor.css');
var devConfig = require('./webpack.config.dev');
var prodConfig = require('./webpack.config.prod');
var isDevelopment = process.env.ASPNET_ENV === 'Development';

module.exports = merge({
    resolve: {
        extensions: [ '', '.js', '.ts' ]
    },
    module: {
        loaders: [
            { test: /\.ts$/, include: /ClientApp/, loader: 'ts-loader' },
            { test: /\.(png|woff|woff2|eot|ttf|svg)$/, loader: 'url-loader?limit=100000' },
            { test: /\.html$/, loader: 'raw-loader' },
            { test: /\.css/, include: /bootstrap/, loader: extractVendorCSS.extract(['css']) },
            { test: /\.css/, exclude: /bootstrap/, loader: extractSiteCSS.extract(['css']) },
        ]
    },
    entry: {
        main: ['./ClientApp/boot-client.ts'],
        vendor: ['angular2/bundles/angular2-polyfills.js', 'bootstrap', 'style-loader', 'jquery', 'angular2/core', 'angular2/common', 'angular2/http', 'angular2/router', 'angular2/platform/browser']
    },
    output: {
        path: path.join(__dirname, 'wwwroot', 'dist'),
        filename: '[name].js',
        publicPath: '/dist/'
    },
    plugins: [
        new webpack.ProvidePlugin({ $: 'jquery', jQuery: 'jquery' }), // Maps these identifiers to the jQuery package (because Bootstrap expects it to be a global variable)
        new webpack.optimize.OccurenceOrderPlugin(),
        new webpack.optimize.CommonsChunkPlugin('vendor', 'vendor.js'), // Moves vendor content out of other bundles
        extractSiteCSS,
        extractVendorCSS
    ]
}, isDevelopment ? devConfig : prodConfig);
