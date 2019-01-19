const path = require('path');
const webpack = require('webpack');

module.exports = (env, args) => ({
    resolve: { extensions: ['.ts', '.js'] },
    devtool: args.mode === 'development' ? 'inline-source-map' : 'none',
    module: {
        rules: [{ test: /\.ts?$/, loader: 'ts-loader' }]
    },
    entry: {
        'components.webassembly': './Boot.WebAssembly.ts',
        'components.server': './Boot.Server.ts',
    },
    output: { path: path.join(__dirname, '/dist'), filename: '[name].js' }
});
