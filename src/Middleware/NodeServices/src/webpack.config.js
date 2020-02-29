const path = require('path');

module.exports = {
    target: 'node',
    resolve: {
        extensions: [ '.ts' ]
    },
    module: {
        rules: [
            { test: /\.ts$/, use: 'ts-loader' },
        ]
    },
    entry: {
        'entrypoint-http': ['./TypeScript/HttpNodeInstanceEntryPoint']
    },
    output: {
        libraryTarget: 'commonjs',
        path: path.join(__dirname, 'Content', 'Node'),
        filename: '[name].js'
    },
    optimization: {
        minimize: false
    }
};
