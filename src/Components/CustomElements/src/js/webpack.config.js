const path = require('path');

module.exports = {
    entry: './BlazorCustomElements.ts',
    module: {
        rules: [{
            test: /\.tsx?$/,
            use: 'ts-loader',
            exclude: /node_modules/,
        }],
    },
    resolve: {
        extensions: ['.tsx', '.ts', '.js'],
    },
    output: {
        filename: 'BlazorCustomElements.js',
        path: path.resolve(__dirname, '..', 'wwwroot'),
    },
};
