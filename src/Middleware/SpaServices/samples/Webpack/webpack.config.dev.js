module.exports = {
    devtool: 'inline-source-map',
    module: {
        loaders: [
            { test: /\.less$/, loader: 'style-loader!css-loader!less-loader' }
        ]
    }
};
