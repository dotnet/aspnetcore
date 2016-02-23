var path = require('path');
var webpack = require('webpack');
var merge = require('extendify')({ isDeep: true, arrays: 'concat' });
var devConfig = require('./webpack.config.dev');
var prodConfig = require('./webpack.config.prod');
var isDevelopment = process.env.ASPNET_ENV === 'Development';

module.exports = merge({
    resolve: {
        extensions: [ '', '.js', '.ts' ]
    },
    module: {
        loaders: [
            { test: /\.ts(x?)$/, include: /ClientApp/, loader: 'babel-loader' },
            { test: /\.ts(x?)$/, include: /ClientApp/, loader: 'ts-loader' },
            { test: /\.(png|woff|woff2|eot|ttf|svg)$/, loader: 'url-loader?limit=100000' },
            { test: /\.css$/, include: /ClientApp/, loader: 'raw-loader' },
            { test: /\.html$/, loader: 'raw-loader' }
        ]
    },
    entry: {
        main: ['./ClientApp/boot.ts'],
        vendor: ['angular2/bundles/angular2-polyfills.js', 'bootstrap', 'bootstrap/dist/css/bootstrap.css', 'style-loader', 'jquery', 'angular2/core', 'angular2/common', 'angular2/http', 'angular2/router', 'angular2/platform/browser']
    },
    output: {
        path: path.join(__dirname, 'wwwroot', 'dist'),
        filename: '[name].js',
        publicPath: '/dist/'
    },
    plugins: [
        new webpack.ProvidePlugin({ $: 'jquery', jQuery: 'jquery' }), // Maps these identifiers to the jQuery package (because Bootstrap expects it to be a global variable)
        new webpack.optimize.OccurenceOrderPlugin(),
        new webpack.optimize.CommonsChunkPlugin('vendor', 'vendor.js') // Moves vendor content out of other bundles
    ]
}, isDevelopment ? devConfig : prodConfig);
