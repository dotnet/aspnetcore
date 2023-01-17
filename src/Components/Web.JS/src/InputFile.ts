// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

export const InputFile = {
  init,
  toImageFile,
  readFileData,
};

interface BrowserFile {
  id: number;
  lastModified: string;
  name: string;
  size: number;
  contentType: string;
  blob: Blob;
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

    const fileList = Array.prototype.map.call(elem.files, function(file: File): BrowserFile {
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

async function toImageFile(elem: InputElement, fileId: number, format: string, maxWidth: number, maxHeight: number): Promise<BrowserFile> {
  const originalFile = getFileById(elem, fileId);

  const loadedImage = await new Promise(function(resolve: (loadedImage: HTMLImageElement) => void): void {
    const originalFileImage = new Image();
    originalFileImage.onload = function(): void {
      URL.revokeObjectURL(originalFileImage.src);
      resolve(originalFileImage);
    };
    originalFileImage.onerror = function(): void {
      originalFileImage.onerror = null;
      URL.revokeObjectURL(originalFileImage.src);
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
    blob: resizedImageBlob ? resizedImageBlob : originalFile.blob,
  };

  elem._blazorFilesById[result.id] = result;

  return result;
}

async function readFileData(elem: InputElement, fileId: number): Promise<Blob> {
  const file = getFileById(elem, fileId);
  return file.blob;
}

export function getFileById(elem: InputElement, fileId: number): BrowserFile {
  const file = elem._blazorFilesById[fileId];

  if (!file) {
    throw new Error(`There is no file with ID ${fileId}. The file list may have changed. See https://aka.ms/aspnet/blazor-input-file-multiple-selections.`);
  }

  return file;
}
