var webpack = require('webpack');

module.exports = {
    plugins: [
        new webpack.optimize.OccurenceOrderPlugin(),
        new webpack.optimize.UglifyJsPlugin({ compress: { warnings: false } }),
        new webpack.DefinePlugin({ 'process.env.NODE_ENV': '"production"' })
    ]
};
