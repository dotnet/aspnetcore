export async function getNextChunk(data: ArrayBufferView | Blob, position: number, nextChunkSize: number): Promise<Uint8Array> {
    if (data instanceof Blob) {
        return await getChunkFromBlob(data, position, nextChunkSize);
    } else {
        return getChunkFromArrayBufferView(data, position, nextChunkSize);
    }
}

async function getChunkFromBlob(data: Blob, position: number, nextChunkSize: number): Promise<Uint8Array> {
    const chunkBlob = data.slice(position, Math.min(position + nextChunkSize, data.size));
    const arrayBuffer = await chunkBlob.arrayBuffer();
    const nextChunkData = new Uint8Array(arrayBuffer);
    return nextChunkData;
}

function getChunkFromArrayBufferView(data: ArrayBufferView, position: number, nextChunkSize: number) {
    const nextChunkData = new Uint8Array(data.buffer, data.byteOffset + position, nextChunkSize);
    return nextChunkData;
}

export async function sendJSDataStreamUsingObjectReference(
        send: (streamId: number, chunkId: number, nextChunkData: Uint8Array | null, error: string | null) => Promise<boolean>,
        data: ArrayBufferView | Blob,
        streamId: number,
        chunkSize: number) {

    // Run the rest in the background, without delaying the completion of the call to sendJSDataStreamUsingObjectReference
    // otherwise we'll deadlock (.NET can't begin reading until this completes, but it won't complete
    // because nobody's reading the pipe)
    setTimeout(async () => {
        try {
            const byteLength = data instanceof Blob ? data.size : data.byteLength;
            let position = 0;
            let chunkId = 0;

            while (position < byteLength) {
                const nextChunkSize = Math.min(chunkSize, byteLength - position);
                let nextChunkData = await getNextChunk(data, position, nextChunkSize);

                const streamIsAlive = await send(streamId, chunkId, nextChunkData, null);

                // Checks to see if we should continue streaming or if the stream has been cancelled/disposed.
                if (!streamIsAlive) {
                    break;
                }

                position += nextChunkSize;
                chunkId++;
            }
        } catch (error) {
            send(streamId, -1, null, error);
        }
    }, 0);
};
