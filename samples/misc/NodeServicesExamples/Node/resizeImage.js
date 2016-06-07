var sharp = require('sharp');

module.exports = function(result, physicalPath, mimeType, maxWidth, maxHeight) {
    sharp(physicalPath)
        .resize(maxWidth > 0 ? maxWidth : null, maxHeight > 0 ? maxHeight : null)
        .pipe(result.stream);
}
