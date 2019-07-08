const path = require('path');
const webpack = require('webpack');

module.exports = (env, args) => ({
    resolve: { extensions: ['.ts', '.js'] },
    devtool: args.mode === 'development' ? 'source-map' : 'none',
    module: {
        rules: [{ test: /\.ts?$/, loader: 'ts-loader' }]
    },
    entry: {
        'blazor.webassembly': './Boot.WebAssembly.ts',
        'blazor.server': './Boot.Server.ts',
    },
    output: { path: path.join(__dirname, '/..', '/dist', args.mode == 'development' ? '/Debug' : '/Release'), filename: '[name].js' }
});
