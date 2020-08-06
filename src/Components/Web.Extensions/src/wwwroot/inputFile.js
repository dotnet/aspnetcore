(function () {

    // Exported functions

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
                    type: file.type,
                    relativePath: file.webkitRelativePath,
                };

                elem._blazorFilesById[result.id] = result;

                // Attach the blob data itself as a non-enumerable property so it doesn't appear in the JSON.
                Object.defineProperty(result, 'blob', { value: file });

                return result;
            });

            callbackWrapper.invokeMethodAsync('NotifyChange', fileList);
        });
    }

    function ensureArrayBufferReadyForSharedMemoryInterop(elem, fileId) {
        return getArrayBufferFromFileAsync(elem, fileId).then(function (arrayBuffer) {
            getFileById(elem, fileId).arrayBuffer = arrayBuffer;
        });
    }

    function readFileDataSharedMemory(readRequest) {
        const inputFileElementReferenceId = Blazor.platform.readStringField(readRequest, 0);
        const inputFileElement = document.querySelector(`[_bl_${inputFileElementReferenceId}]`);
        const fileId = Blazor.platform.readInt32Field(readRequest, 4);
        const sourceOffset = Blazor.platform.readUint64Field(readRequest, 8);
        const destination = Blazor.platform.readInt32Field(readRequest, 16);
        const destinationOffset = Blazor.platform.readInt32Field(readRequest, 20);
        const maxBytes = Blazor.platform.readInt32Field(readRequest, 24);

        const sourceArrayBuffer = getFileById(inputFileElement, fileId).arrayBuffer;
        const bytesToRead = Math.min(maxBytes, sourceArrayBuffer.byteLength - sourceOffset);
        const sourceUint8Array = new Uint8Array(sourceArrayBuffer, sourceOffset, bytesToRead);

        const destinationUint8Array = Blazor.platform.toUint8Array(destination);
        destinationUint8Array.set(sourceUint8Array, destinationOffset);

        return bytesToRead;
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
