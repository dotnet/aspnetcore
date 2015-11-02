var babelCore = require('babel-core');

module.exports = function(cb, fileContents) {
    var result = babelCore.transform(fileContents, {});
    cb(null, result.code);
}
