var webpack = require('webpack');

module.exports = {
    plugins: [
        // Plugins that apply in development builds only
        new webpack.SourceMapDevToolPlugin({
            filename: '[name].js.map', // Remove this line if you prefer inline source maps
            moduleFilenameTemplate: path.relative('./wwwroot/dist', '[resourcePath]') // Point sourcemap entries to the original file locations on disk
        })
    ]
};
