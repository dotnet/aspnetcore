var sharp = require('sharp');

module.exports = function(cb, physicalPath, mimeType, maxWidth, maxHeight) {
    sharp(physicalPath)
        .resize(maxWidth > 0 ? maxWidth : null, maxHeight > 0 ? maxHeight : null)
        .toBuffer(function (err, buffer) {
            cb(err, { base64: buffer && buffer.toString('base64') });
        });
}
