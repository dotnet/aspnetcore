var path = require('path');
var webpack = require('webpack');
var ExtractTextPlugin = require('extract-text-webpack-plugin');

module.exports = {
    devtool: 'inline-source-map',
    resolve: {
        extensions: [ '', '.js', '.jsx', '.ts', '.tsx' ]
    },
    module: {
        loaders: [
            { test: /\.ts(x?)$/, include: /ReactApp/, exclude: /node_modules/, loader: 'babel-loader' },
            { test: /\.ts(x?)$/, include: /ReactApp/, exclude: /node_modules/, loader: 'ts-loader' },
            { test: /\.css$/, loader: ExtractTextPlugin.extract('style-loader', 'css-loader') },
            { test: /\.(png|woff|woff2|eot|ttf|svg)$/, loader: 'url-loader?limit=100000' }
        ]
    },
    entry: {
        main: ['./ReactApp/boot.tsx'],
        vendor: ['react']
    },
    output: {
        path: path.join(__dirname, 'wwwroot', 'dist'),
        filename: '[name].js',
        publicPath: '/dist/'
    },
    plugins: [
        new webpack.optimize.OccurenceOrderPlugin(),
        new ExtractTextPlugin('main.css'),
        new webpack.optimize.CommonsChunkPlugin('vendor', 'vendor.bundle.js') // Moves vendor content out of other bundles
    ]
};
