const path = require('path');

module.exports = {
    target: 'node',
    externals: [
        // These NPM modules are loaded dynamically at runtime, rather than being bundled into the Content/Node/*.js files
        // So, at runtime, they have to either be in node_modules or be built-in Node modules (e.g., 'fs')
        'aspnet-prerendering',
        'aspnet-webpack'
    ],
    resolve: {
        extensions: [ '.ts' ]
    },
    module: {
        rules: [
            { test: /\.ts$/, use: 'ts-loader' },
        ]
    },
    entry: {
        'prerenderer': ['./TypeScript/Prerenderer'],
        'webpack-dev-middleware': ['./TypeScript/WebpackDevMiddleware'],
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
