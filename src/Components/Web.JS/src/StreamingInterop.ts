export async function getNextChunk(data: ArrayBufferView | Blob, position: number, nextChunkSize: number): Promise<Uint8Array> {
    if (data instanceof Blob) {
        return await getChunkFromBlob(data, position, nextChunkSize);
    } else {
        return getChunkFromArrayBufferView(data, position, nextChunkSize);
    }
}

async function getChunkFromBlob(data: Blob, position: number, nextChunkSize: number): Promise<Uint8Array> {
    const chunkBlob = data.slice(position, position + nextChunkSize);
    const arrayBuffer = await chunkBlob.arrayBuffer();
    const nextChunkData = new Uint8Array(arrayBuffer);
    return nextChunkData;
}

function getChunkFromArrayBufferView(data: ArrayBufferView, position: number, nextChunkSize: number) {
    const nextChunkData = new Uint8Array(data.buffer, data.byteOffset + position, nextChunkSize);
    return nextChunkData;
}
