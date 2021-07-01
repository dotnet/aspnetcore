import { sendJSDataStreamUsingObjectReference } from '../StreamingInterop';
import { setByteArrayBeingTransferred } from './Mono/MonoPlatform';

export async function sendJSDataStreamWASM(data: ArrayBufferView | Blob, streamId: number, chunkSize: number, dotnetStreamReference: any) {
    async function send(streamId: number, chunkId: number, nextChunkData: Uint8Array | null, error: string | null): Promise<boolean> {
        if (error) {
            await dotnetStreamReference.invokeMethodAsync('ReceiveJSDataChunk', streamId, -1, error.toString());
            return false;
        }

        setByteArrayBeingTransferred(nextChunkData!);
        return await dotnetStreamReference.invokeMethodAsync('ReceiveJSDataChunk', streamId, chunkId, null);
    }

    sendJSDataStreamUsingObjectReference(send, data, streamId, chunkSize);
};
