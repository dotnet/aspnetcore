export function sendJSDataStreamWebView(send: (messageType: string, ...args: any[]) => void, data: ArrayBufferView, streamId: string, chunkSize: number) {
    // Run the rest in the background, without delaying the completion of the call to sendJSDataStream
    // otherwise we'll deadlock (.NET can't begin reading until this completes, but it won't complete
    // because nobody's reading the pipe)
    setTimeout(async () => {
        try {
            let position = 0;
            let chunkId = 0;

            while (position < data.byteLength) {
                const nextChunkSize = Math.min(chunkSize, data.byteLength - position);
                const nextChunkByteArray = new Uint8Array(data.buffer, data.byteOffset + position, nextChunkSize);
                const nextChunkData = btoa(String.fromCharCode.apply(null, nextChunkByteArray as unknown as number[]));

                send('ReceiveJSDataChunk', streamId, chunkId, nextChunkData, null);

                position += nextChunkSize;
                chunkId++;
            }
        } catch (error) {
            send('ReceiveJSDataChunk', streamId, -1, null, error.toString());
        }
    }, 0);
};
