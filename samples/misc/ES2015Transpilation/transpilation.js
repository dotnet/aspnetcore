var babelCore = require('babel-core');

module.exports = function(cb, fileContents, url) {
    var result = babelCore.transform(fileContents, { 
    	sourceMaps: 'inline',
    	sourceFileName: '/sourcemapped/' + url
    });
    cb(null, result.code);
}
