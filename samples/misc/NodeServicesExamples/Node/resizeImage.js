var Jimp = require('jimp');

module.exports = function(cb, physicalPath, mimeType, maxWidth, maxHeight) {
    Jimp.read(physicalPath, function (err, loadedImage) {
        if (err) {
            cb(err);
        }

        loadedImage
            .contain(maxWidth > 0 ? maxWidth : Jimp.AUTO, maxHeight > 0 ? maxHeight : Jimp.AUTO)
            .getBuffer(mimeType, function(err, buffer) {
                cb(err, { base64: buffer && buffer.toString('base64') });
            });
    });
}
