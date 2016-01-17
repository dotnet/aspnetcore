var ExtractTextPlugin = require('extract-text-webpack-plugin');

module.exports = {
    entry: './ReactApp/boot-client.jsx',
    output: {
        path: './wwwroot',
        filename: 'bundle.js'
    },
    module: {
        loaders: [
            { test: /\.jsx?$/, loader: 'babel-loader', exclude: /node_modules/, query: { presets: ['es2015', 'react'] } },
            { test: /\.css$/, loader: ExtractTextPlugin.extract('style-loader', 'css-loader') },
            { test: /\.(png|woff|woff2|eot|ttf|svg)$/, loader: 'url-loader?limit=100000' }
        ]
    },
    plugins: [
        new ExtractTextPlugin('main.css')
    ]
};
