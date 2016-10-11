var SourceMapDevToolPlugin = require('aspnet-webpack').SourceMapDevToolPlugin;

module.exports = {
    plugins: [
        new SourceMapDevToolPlugin({ moduleFilenameTemplate: '../../[resourcePath]' }) // Compiled output is at './wwwroot/dist/', but sources are relative to './'
    ]
};
