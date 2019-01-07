var webpack = require('webpack');
var ExtractTextPlugin = require('extract-text-webpack-plugin');
var extractLESS = new ExtractTextPlugin('my-styles.css');

module.exports = {
    module: {
        loaders: [
            { test: /\.less$/, loader: extractLESS.extract(['css-loader', 'less-loader']) },
        ]
    },
    plugins: [
        extractLESS,
        new webpack.optimize.UglifyJsPlugin({ minimize: true, compressor: { warnings: false } })
    ]
};
