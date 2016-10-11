var webpack = require('webpack');

module.exports = {
    plugins: [
        new webpack.SourceMapDevToolPlugin({ moduleFilenameTemplate: '../../[resourcePath]' }) // Compiled output is at './wwwroot/dist/', but sources are relative to './'
    ]
};
