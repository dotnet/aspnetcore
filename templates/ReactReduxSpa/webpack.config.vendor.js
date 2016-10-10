var isDevBuild = process.argv.indexOf('--env.prod') < 0;
var path = require('path');
var webpack = require('webpack');
var ExtractTextPlugin = require('extract-text-webpack-plugin');
var extractCSS = new ExtractTextPlugin('vendor.css');

module.exports = {
    resolve: {
        extensions: [ '', '.js' ]
    },
    module: {
        loaders: [
            { test: /\.(png|woff|woff2|eot|ttf|svg)(\?|$)/, loader: 'url-loader?limit=100000' },
            { test: /\.css(\?|$)/, loader: extractCSS.extract(['css']) }
        ]
    },
    entry: {
        vendor: ['bootstrap', 'bootstrap/dist/css/bootstrap.css', 'domain-task', 'event-source-polyfill', 'react', 'react-dom', 'react-router', 'redux', 'redux-thunk', 'react-router-redux', 'redux-typed', 'style-loader', 'jquery'],
    },
    output: {
        path: path.join(__dirname, 'wwwroot', 'dist'),
        filename: '[name].js',
        library: '[name]_[hash]',
    },
    plugins: [
        extractCSS,
        new webpack.ProvidePlugin({ $: 'jquery', jQuery: 'jquery' }), // Maps these identifiers to the jQuery package (because Bootstrap expects it to be a global variable)
        new webpack.optimize.OccurenceOrderPlugin(),
        new webpack.DllPlugin({
            path: path.join(__dirname, 'wwwroot', 'dist', '[name]-manifest.json'),
            name: '[name]_[hash]'
        }),
        new webpack.DefinePlugin({
            'process.env.NODE_ENV': isDevBuild ? '"development"' : '"production"'
        })
    ].concat(isDevBuild ? [] : [
        new webpack.optimize.UglifyJsPlugin({ compress: { warnings: false } })
    ])
};
