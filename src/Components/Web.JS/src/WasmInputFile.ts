import { monoPlatform } from './Platform/Mono/MonoPlatform';
import { System_Array } from './Platform/Platform';
import { InputFile, InputElement, getFileById } from './InputFile';

export const WasmInputFile = {
    ...InputFile,
    readFileDataSharedMemory,
};

function readFileDataSharedMemory(readRequest: any): number {
    const inputFileElementReferenceId = monoPlatform.readStringField(readRequest, 0);
    const inputFileElement = document.querySelector(`[_bl_${inputFileElementReferenceId}]`);
    const fileId = monoPlatform.readInt32Field(readRequest, 8);
    const sourceOffset = monoPlatform.readUint64Field(readRequest, 12);
    const destination = monoPlatform.readInt32Field(readRequest, 24) as unknown as System_Array<number>;
    const destinationOffset = monoPlatform.readInt32Field(readRequest, 32);
    const maxBytes = monoPlatform.readInt32Field(readRequest, 36);

    const chunk = getFileById(inputFileElement as InputElement, fileId).chunk;
    if (chunk === undefined) {
        throw new Error(`Chunk for file ${fileId} with source offset ${sourceOffset} was undefined, unable to read further.`)
    }

    const destinationUint8Array = monoPlatform.toUint8Array(destination);
    destinationUint8Array.set(chunk, destinationOffset);

    return chunk.length;
}
