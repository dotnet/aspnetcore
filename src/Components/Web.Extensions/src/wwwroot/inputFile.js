(function () {

    // Exported functions

    function init(elem, callbackWrapper) {
        elem._blazorInputFileNextFileId = 0;

        elem.addEventListener('change', function (event) {
            // Reduce to purely serializable data, plus an index by ID.
            elem._blazorFilesById = {};

            const fileList = Array.prototype.map.call(elem.files, function (file) {
                const result = {
                    ...file,
                    lastModified: new Date(file.lastModified).toISOString(),
                    id: ++elem._blazorInputFileNextFileId,
                };

                elem._blazorFilesById[result.id] = result;

                // Attach the blob data itself as a non-enumerable property so it doesn't appear in the JSON.
                Object.defineProperty(result, 'blob', { value: file });

                return result;
            });

            callbackWrapper.invokeMethodAsync('NotifyChange', fileList);
            // TODO: Handle the case where a file is re-uploaded.
        });
    }

    function ensureArrayBufferReadyForSharedMemoryInterop(elem, fileId) {
        return getArrayBufferFromFileAsync(elem, fileId).then(function (arrayBuffer) {
            getFileById(elem, fileId).arrayBuffer = arrayBuffer;
        });
    }

    function readFileDataSharedMemory(readRequest) {
        // TODO
    }

    // Local helpers

    function getFileById(elem, fileId) {
        const file = elem._blazorFilesById[fileId];

        if (!file) {
            throw new Error(`There is no file with ID ${fileId}. The file list may have changed.`);
        }

        return file;
    }

    function getArrayBufferFromFileAsync(elem, fileId) {
        const file = getFileById(elem, fileId);

        // On the first read, convert the FileReader into a Promise<ArrayBuffer>.
        if (!file.readPromise) {
            file.readPromise = new Promise(function (resolve, reject) {
                const reader = new FileReader();
                reader.onload = function () { resolve(reader.result); };
                reader.onerror = function (err) { reject(err); };
                reader.readAsArrayBuffer(file.blob);
            });
        }

        return file.readPromise;
    }

    window._blazorInputFile = {
        init,
        ensureArrayBufferReadyForSharedMemoryInterop,
        readFileDataSharedMemory,
    };
})();
