export declare const InputFile: {
    init: typeof init;
    toImageFile: typeof toImageFile;
    readFileData: typeof readFileData;
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
    _blazorFilesById: {
        [id: number]: BrowserFile;
    };
}
declare function init(callbackWrapper: any, elem: InputElement): void;
declare function toImageFile(elem: InputElement, fileId: number, format: string, maxWidth: number, maxHeight: number): Promise<BrowserFile>;
declare function readFileData(elem: InputElement, fileId: number): Promise<Blob>;
export declare function getFileById(elem: InputElement, fileId: number): BrowserFile;
export {};
