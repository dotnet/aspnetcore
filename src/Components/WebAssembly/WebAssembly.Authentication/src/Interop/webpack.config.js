const path = require('path');

module.exports = env => {

    return {
        entry: './AuthenticationService.ts',
        devtool: env && env.production ? 'none' : 'source-map',
        module: {
            rules: [
                {
                    test: /\.tsx?$/,
                    use: 'ts-loader',
                    exclude: /node_modules/,
                },
            ],
        },
        resolve: {
            extensions: ['.tsx', '.ts', '.js'],
        },
        output: {
            filename: 'AuthenticationService.js',
            path: path.resolve(__dirname, 'dist', env.configuration),
        },
    };
};
