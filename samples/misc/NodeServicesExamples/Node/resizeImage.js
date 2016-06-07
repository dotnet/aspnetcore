var sharp = require('sharp');

module.exports = function(result, physicalPath, mimeType, maxWidth, maxHeight) {
    // Invoke the 'sharp' NPM module, and have it pipe the resulting image data back to .NET
    sharp(physicalPath)
        .resize(maxWidth || null, maxHeight || null)
        .pipe(result.stream);
}
