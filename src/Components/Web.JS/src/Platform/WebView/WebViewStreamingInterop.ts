import { getNextChunk } from '../../StreamingInterop';

export function sendJSDataStreamWebView(send: (messageType: string, ...args: any[]) => void, data: ArrayBufferView | Blob, streamId: string, chunkSize: number, jsDataStream: any) {
    // Run the rest in the background, without delaying the completion of the call to sendJSDataStream
    // otherwise we'll deadlock (.NET can't begin reading until this completes, but it won't complete
    // because nobody's reading the pipe)
    setTimeout(async () => {
        try {
            const byteLength = data instanceof Blob ? data.size : data.byteLength;
            let position = 0;
            let chunkId = 0;

            while (position < byteLength) {
                const nextChunkSize = Math.min(chunkSize, byteLength - position);
                const nextChunkByteArray = await getNextChunk(data, position, nextChunkSize);
                const nextChunkData = btoa(String.fromCharCode.apply(null, nextChunkByteArray as unknown as number[]));

                const streamIsAlive = await jsDataStream.invokeMethodAsync('ReceiveData', chunkId, nextChunkData, null);

                // Checks to see if we should continue streaming or if the stream has been cancelled/disposed.
                if (!streamIsAlive) {
                    break;
                }

                // send('ReceiveJSDataChunk', streamId, chunkId, nextChunkData, null);

                position += nextChunkSize;
                chunkId++;
            }
        } catch (error) {
            await jsDataStream.invokeMethodAsync('ReceiveData', streamId, '', error.toString());
            // send('ReceiveJSDataChunk', streamId, -1, null, error.toString());
        }
    }, 0);
};
