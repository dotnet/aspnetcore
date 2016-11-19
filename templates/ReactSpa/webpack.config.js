var isDevBuild = process.argv.indexOf('--env.prod') < 0;
var path = require('path');
var webpack = require('webpack');
var ExtractTextPlugin = require('extract-text-webpack-plugin');

var bundleOutputDir = './wwwroot/dist';
module.exports = {
    devtool: isDevBuild ? 'inline-source-map' : null,
    entry: { 'main': './ClientApp/boot.tsx' },
    resolve: { extensions: [ '', '.js', '.jsx', '.ts', '.tsx' ] },
    output: {
        path: path.join(__dirname, bundleOutputDir),
        filename: '[name].js',
        publicPath: '/dist/'
    },
    module: {
        loaders: [
            { test: /\.ts(x?)$/, include: /ClientApp/, loader: 'babel-loader' },
            { test: /\.tsx?$/, include: /ClientApp/, loader: 'ts-loader', query: { silent: true } },
            { test: /\.css$/, loader: isDevBuild ? 'style-loader!css-loader' : ExtractTextPlugin.extract(['css-loader']) },
            { test: /\.(png|jpg|jpeg|gif|svg)$/, loader: 'url-loader', query: { limit: 25000 } }
        ]
    },
    plugins: [
        new webpack.DllReferencePlugin({
            context: __dirname,
            manifest: require('./wwwroot/dist/vendor-manifest.json')
        })
    ].concat(isDevBuild ? [
        // Plugins that apply in development builds only
        new webpack.SourceMapDevToolPlugin({
            filename: '[file].map', // Remove this line if you prefer inline source maps
            moduleFilenameTemplate: path.relative(bundleOutputDir, '[resourcePath]') // Point sourcemap entries to the original file locations on disk
        })
    ] : [
        // Plugins that apply in production builds only
        new webpack.optimize.OccurenceOrderPlugin(),
        new webpack.optimize.UglifyJsPlugin({ compress: { warnings: false } }),
        new ExtractTextPlugin('site.css')
    ])
};
