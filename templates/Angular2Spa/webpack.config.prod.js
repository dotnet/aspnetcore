var webpack = require('webpack');

module.exports = {
    plugins: [
        new webpack.optimize.OccurenceOrderPlugin(),
        new webpack.optimize.UglifyJsPlugin({
            compress: { warnings: false },
            minimize: true,
            mangle: false // Due to https://github.com/angular/angular/issues/6678
        })
    ]
};
