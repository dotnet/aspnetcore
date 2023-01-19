// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
export const InputFile = {
    init,
    toImageFile,
    readFileData,
};
function init(callbackWrapper, elem) {
    elem._blazorInputFileNextFileId = 0;
    elem.addEventListener('click', function () {
        // Permits replacing an existing file with a new one of the same file name.
        elem.value = '';
    });
    elem.addEventListener('change', function () {
        // Reduce to purely serializable data, plus an index by ID.
        elem._blazorFilesById = {};
        const fileList = Array.prototype.map.call(elem.files, function (file) {
            const result = {
                id: ++elem._blazorInputFileNextFileId,
                lastModified: new Date(file.lastModified).toISOString(),
                name: file.name,
                size: file.size,
                contentType: file.type,
                readPromise: undefined,
                arrayBuffer: undefined,
                blob: file,
            };
            elem._blazorFilesById[result.id] = result;
            return result;
        });
        callbackWrapper.invokeMethodAsync('NotifyChange', fileList);
    });
}
async function toImageFile(elem, fileId, format, maxWidth, maxHeight) {
    const originalFile = getFileById(elem, fileId);
    const loadedImage = await new Promise(function (resolve) {
        const originalFileImage = new Image();
        originalFileImage.onload = function () {
            URL.revokeObjectURL(originalFileImage.src);
            resolve(originalFileImage);
        };
        originalFileImage.onerror = function () {
            originalFileImage.onerror = null;
            URL.revokeObjectURL(originalFileImage.src);
        };
        originalFileImage.src = URL.createObjectURL(originalFile['blob']);
    });
    const resizedImageBlob = await new Promise(function (resolve) {
        var _a;
        const desiredWidthRatio = Math.min(1, maxWidth / loadedImage.width);
        const desiredHeightRatio = Math.min(1, maxHeight / loadedImage.height);
        const chosenSizeRatio = Math.min(desiredWidthRatio, desiredHeightRatio);
        const canvas = document.createElement('canvas');
        canvas.width = Math.round(loadedImage.width * chosenSizeRatio);
        canvas.height = Math.round(loadedImage.height * chosenSizeRatio);
        (_a = canvas.getContext('2d')) === null || _a === void 0 ? void 0 : _a.drawImage(loadedImage, 0, 0, canvas.width, canvas.height);
        canvas.toBlob(resolve, format);
    });
    const result = {
        id: ++elem._blazorInputFileNextFileId,
        lastModified: originalFile.lastModified,
        name: originalFile.name,
        size: (resizedImageBlob === null || resizedImageBlob === void 0 ? void 0 : resizedImageBlob.size) || 0,
        contentType: format,
        blob: resizedImageBlob ? resizedImageBlob : originalFile.blob,
    };
    elem._blazorFilesById[result.id] = result;
    return result;
}
async function readFileData(elem, fileId) {
    const file = getFileById(elem, fileId);
    return file.blob;
}
export function getFileById(elem, fileId) {
    const file = elem._blazorFilesById[fileId];
    if (!file) {
        throw new Error(`There is no file with ID ${fileId}. The file list may have changed. See https://aka.ms/aspnet/blazor-input-file-multiple-selections.`);
    }
    return file;
}
//# sourceMappingURL=InputFile.js.map