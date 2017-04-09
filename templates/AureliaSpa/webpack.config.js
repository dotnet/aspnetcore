const path = require('path');
const webpack = require('webpack');
const { AureliaPlugin } = require('aurelia-webpack-plugin');

module.exports = ({ prod } = {}) => {
    const isDevBuild = !prod;
    const isProdBuild = prod;
    const bundleOutputDir = './wwwroot/dist';

    return {
        resolve: {
            extensions: [".ts", ".js"],
            modules: ["ClientApp", "node_modules"],
        },
        entry: { 'app': 'aurelia-bootstrapper' },
        output: {
            path: path.resolve(bundleOutputDir),
            publicPath: "/dist/",
            filename: '[name].js'
        },
        module: {
            rules: [
                { test: /\.css$/i, use: [isDevBuild ? 'css-loader' : 'css-loader?minimize'] },
                { test: /\.html$/i, use: ["html-loader"] },
                { test: /\.ts$/i, loaders: ['ts-loader'], exclude: path.resolve(__dirname, 'node_modules') },
                { test: /\.json$/i, loader: 'json-loader', exclude: path.resolve(__dirname, 'node_modules') },
                { test: /\.(png|woff|woff2|eot|ttf|svg)$/, loader: 'url-loader', query: { limit: 8192 } }
            ]
        },
        plugins: [
            new webpack.DefinePlugin({ IS_DEV_BUILD: JSON.stringify(isDevBuild) }),
            new webpack.DllReferencePlugin({
                context: __dirname,
                manifest: require('./wwwroot/dist/vendor-manifest.json')
            }),
            new AureliaPlugin({ aureliaApp: "boot" }),
            ...when(isDevBuild, [
                new webpack.SourceMapDevToolPlugin({
                    filename: '[file].map',
                    moduleFilenameTemplate: path.relative(bundleOutputDir, '[resourcePath]')
                })
            ]),
            ...when(isProdBuild, [
                new webpack.optimize.UglifyJsPlugin()
            ])
        ]
    };
}

const ensureArray = (config) => config && (Array.isArray(config) ? config : [config]) || []
const when = (condition, config, negativeConfig) => condition ? ensureArray(config) : ensureArray(negativeConfig)