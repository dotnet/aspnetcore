import { sendJSDataStreamUsingObjectReference } from "../../StreamingInterop";

export async function sendJSDataStreamWebView(data: ArrayBufferView | Blob, streamId: number, chunkSize: number, dotnetStreamReference: any) {
    async function send(streamId: number, chunkId: number, nextChunkData: Uint8Array | null, error: string | null): Promise<boolean> {
        if (error) {
            await dotnetStreamReference.invokeMethodAsync('ReceiveJSDataChunk', streamId, -1, '', error.toString());
            return false;
        }

        const nextChunkDataStr = btoa(String.fromCharCode.apply(null, nextChunkData as unknown as number[]));
        return await dotnetStreamReference.invokeMethodAsync('ReceiveJSDataChunk', streamId, chunkId, nextChunkDataStr, null);
    }

    sendJSDataStreamUsingObjectReference(send, data, streamId, chunkSize);
};
