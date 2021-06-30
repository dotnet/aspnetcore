import { DotNet } from '../../../../JSInterop/Microsoft.JSInterop.JS/src/dist/Microsoft.JSInterop';
import { setByteArrayBeingTransferred } from './Mono/MonoPlatform';

export async function sendJSDataStreamWASM(data: ArrayBufferView, streamId: string, chunkSize: number) {
    // Run the rest in the background, without delaying the completion of the call to sendJSDataStreamWASM
    // otherwise we'll deadlock (.NET can't begin reading until this completes, but it won't complete
    // because nobody's reading the pipe)
    setTimeout(async () => {
        try {
            let position = 0;
            let chunkId = 0;

            while (position < data.byteLength) {
                const nextChunkSize = Math.min(chunkSize, data.byteLength - position);
                const nextChunkData = new Uint8Array(data.buffer, data.byteOffset + position, nextChunkSize);

                setByteArrayBeingTransferred(nextChunkData);
                await DotNet.invokeMethodAsync('Microsoft.AspNetCore.Components.WebAssembly', 'NotifyJSStreamDataChunkAvailable', streamId, chunkId, null);

                position += nextChunkSize;
                chunkId++;
            }
        } catch (error) {
            await DotNet.invokeMethodAsync('Microsoft.AspNetCore.Components.WebAssembly', 'NotifyJSStreamDataChunkAvailable', streamId, -1, error);
        }
    }, 0);
};
