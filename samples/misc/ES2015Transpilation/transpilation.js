var fs = require('fs');
var babelCore = require('babel-core');

module.exports = function(cb, physicalPath, requestPath) {
    var originalContents = fs.readFileSync(physicalPath);
    var result = babelCore.transform(originalContents, {
    	sourceMaps: 'inline',
    	sourceFileName: '/sourcemapped' + requestPath
    });
    cb(null, result.code);
}
