module.exports = {
    target: 'node',
    externals: ['fs', 'net', 'events', 'readline', 'stream'],
    resolve: {
        extensions: [ '.ts' ]
    },
    module: {
        loaders: [
            { test: /\.ts$/, loader: 'ts-loader' },
        ]
    },
    entry: {
        'entrypoint-socket': ['./TypeScript/SocketNodeInstanceEntryPoint'],
    },
    output: {
        libraryTarget: 'commonjs',
        path: './Content/Node',
        filename: '[name].js'
    }
};
