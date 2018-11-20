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
        'entrypoint-http': ['./TypeScript/HttpNodeInstanceEntryPoint']
    },
    output: {
        libraryTarget: 'commonjs',
        path: './Content/Node',
        filename: '[name].js'
    }
};
