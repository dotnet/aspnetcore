module.exports = {
    devtool: 'inline-source-map',
    module: {
        loaders: [
            { test: /\.css/, exclude: /ClientApp/, loader: 'style!css' }
        ]
    }
};
