var path = require('path');
var webpack = require('webpack');

var isDevBuild = process.env.ASPNETCORE_ENVIRONMENT === 'Development';

module.exports = {
    devtool: isDevBuild ? 'inline-source-map' : null,
    resolve: { extensions: [ '', '.js', '.ts' ] },
    entry: { main: ['./ClientApp/boot-client.ts'] },
    module: {
        loaders: [
            { test: /\.ts$/, include: /ClientApp/, loader: 'ts', query: { silent: true } },
            { test: /\.html$/, include: /ClientApp/, loader: 'raw' },
            { test: /\.css/, include: /ClientApp/, loader: 'to-string!css' },
            { test: /\.(png|jpg|jpeg|gif|svg)$/, loader: 'url', query: { limit: 25000 } }
        ]
    },
    output: {
        path: path.join(__dirname, 'wwwroot', 'dist'),
        filename: '[name].js',
        publicPath: '/dist/'
    },
    plugins: [
        new webpack.DllReferencePlugin({
            context: __dirname,
            manifest: require('./wwwroot/dist/vendor-manifest.json')
        })
    ].concat(isDevBuild ? [] : [
        // Plugins that apply in production builds only
        new webpack.optimize.OccurenceOrderPlugin(),
        new webpack.optimize.UglifyJsPlugin()
    ])
};
