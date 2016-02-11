module.exports = {
    devtool: 'inline-source-map',
    module: {
        loaders: [
            { test: /\.less$/, loader: 'style!css!less' }
        ]
    }
};
