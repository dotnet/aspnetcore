var path = require('path');
var webpack = require('webpack');
var ExtractTextPlugin = require('extract-text-webpack-plugin');

module.exports = {
    devtool: 'eval-source-map',
    resolve: {
        extensions: [ '', '.js', '.jsx' ]
    },
    module: {
        loaders: [
            { test: /\.jsx?$/, loader: 'babel-loader', exclude: /node_modules/ },
            { test: /\.css$/, loader: ExtractTextPlugin.extract('style-loader', 'css-loader') },
            { test: /\.(png|woff|woff2|eot|ttf|svg)$/, loader: 'url-loader?limit=100000' }
        ]
    },
    entry: {
        main: ['./ReactApp/boot-client.jsx']
    },
    output: {
        path: path.join(__dirname, '/wwwroot/dist'),
        filename: '[name].js',
        publicPath: '/dist/' // Tells webpack-dev-middleware where to serve the dynamically compiled content from
    },
    plugins: [
        new ExtractTextPlugin('main.css')
    ]
};
