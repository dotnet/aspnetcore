
export const InputFile = {
  init,
  toImageFile,
  ensureArrayBufferReadyForSharedMemoryInterop,
  readFileData,
};

interface BrowserFile {
  id: number;
  lastModified: string;
  name: string;
  size: number;
  contentType: string;
  readPromise: Promise<ArrayBuffer> | undefined;
  arrayBuffer: ArrayBuffer | undefined;
}

export interface InputElement extends HTMLInputElement {
  _blazorInputFileNextFileId: number;
  _blazorFilesById: { [id: number]: BrowserFile };
}

function init(callbackWrapper: any, elem: InputElement): void {
  elem._blazorInputFileNextFileId = 0;

  elem.addEventListener('click', function(): void {
    // Permits replacing an existing file with a new one of the same file name.
    elem.value = '';
  });

  elem.addEventListener('change', function(): void {
    // Reduce to purely serializable data, plus an index by ID.
    elem._blazorFilesById = {};

    const fileList = Array.prototype.map.call(elem.files, function(file): BrowserFile {
      const result = {
        id: ++elem._blazorInputFileNextFileId,
        lastModified: new Date(file.lastModified).toISOString(),
        name: file.name,
        size: file.size,
        contentType: file.type,
        readPromise: undefined,
        arrayBuffer: undefined,
      };

      elem._blazorFilesById[result.id] = result;

      // Attach the blob data itself as a non-enumerable property so it doesn't appear in the JSON.
      Object.defineProperty(result, 'blob', { value: file });

      return result;
    });

    callbackWrapper.invokeMethodAsync('NotifyChange', fileList);
  });
}

async function toImageFile(elem: InputElement, fileId: number, format: string, maxWidth: number, maxHeight: number): Promise<BrowserFile> {
  const originalFile = getFileById(elem, fileId);

  const loadedImage = await new Promise(function(resolve: (loadedImage: HTMLImageElement) => void): void {
    const originalFileImage = new Image();
    originalFileImage.onload = function(): void {
      resolve(originalFileImage);
    };
    originalFileImage.src = URL.createObjectURL(originalFile['blob']);
  });

  const resizedImageBlob = await new Promise(function(resolve: BlobCallback): void {
    const desiredWidthRatio = Math.min(1, maxWidth / loadedImage.width);
    const desiredHeightRatio = Math.min(1, maxHeight / loadedImage.height);
    const chosenSizeRatio = Math.min(desiredWidthRatio, desiredHeightRatio);

    const canvas = document.createElement('canvas');
    canvas.width = Math.round(loadedImage.width * chosenSizeRatio);
    canvas.height = Math.round(loadedImage.height * chosenSizeRatio);
    canvas.getContext('2d')?.drawImage(loadedImage, 0, 0, canvas.width, canvas.height);
    canvas.toBlob(resolve, format);
  });
  const result: BrowserFile = {
    id: ++elem._blazorInputFileNextFileId,
    lastModified: originalFile.lastModified,
    name: originalFile.name,
    size: resizedImageBlob?.size || 0,
    contentType: format,
    readPromise: undefined,
    arrayBuffer: undefined,
  };

  elem._blazorFilesById[result.id] = result;

  // Attach the blob data itself as a non-enumerable property so it doesn't appear in the JSON.
  Object.defineProperty(result, 'blob', { value: resizedImageBlob });

  return result;
}

async function ensureArrayBufferReadyForSharedMemoryInterop(elem: InputElement, fileId: number): Promise<void> {
  const arrayBuffer = await getArrayBufferFromFileAsync(elem, fileId);
  getFileById(elem, fileId).arrayBuffer = arrayBuffer;
}

async function readFileData(elem: InputElement, fileId: number, startOffset: number, count: number): Promise<string> {
  const arrayBuffer = await getArrayBufferFromFileAsync(elem, fileId);
  return btoa(String.fromCharCode.apply(null, new Uint8Array(arrayBuffer, startOffset, count) as unknown as number[]));
}

export function getFileById(elem: InputElement, fileId: number): BrowserFile {
  const file = elem._blazorFilesById[fileId];

  if (!file) {
    throw new Error(`There is no file with ID ${fileId}. The file list may have changed.`);
  }

  return file;
}

function getArrayBufferFromFileAsync(elem: InputElement, fileId: number): Promise<ArrayBuffer> {
  const file = getFileById(elem, fileId);

  // On the first read, convert the FileReader into a Promise<ArrayBuffer>.
  if (!file.readPromise) {
    file.readPromise = new Promise(function(resolve: (buffer: ArrayBuffer) => void, reject): void {
      const reader = new FileReader();
      reader.onload = function(): void {
        resolve(reader.result as ArrayBuffer);
      };
      reader.onerror = function(err): void {
        reject(err);
      };
      reader.readAsArrayBuffer(file['blob']);
    });
  }

  return file.readPromise;
}