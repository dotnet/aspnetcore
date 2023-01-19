// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
import { DotNet } from '@microsoft/dotnet-js-interop';
export async function getNextChunk(data, position, nextChunkSize) {
    if (data instanceof Blob) {
        return await getChunkFromBlob(data, position, nextChunkSize);
    }
    else {
        return getChunkFromArrayBufferView(data, position, nextChunkSize);
    }
}
async function getChunkFromBlob(data, position, nextChunkSize) {
    const chunkBlob = data.slice(position, position + nextChunkSize);
    const arrayBuffer = await chunkBlob.arrayBuffer();
    const nextChunkData = new Uint8Array(arrayBuffer);
    return nextChunkData;
}
function getChunkFromArrayBufferView(data, position, nextChunkSize) {
    const nextChunkData = new Uint8Array(data.buffer, data.byteOffset + position, nextChunkSize);
    return nextChunkData;
}
const transmittingDotNetToJSStreams = new Map();
export function receiveDotNetDataStream(streamId, data, bytesRead, errorMessage) {
    let streamController = transmittingDotNetToJSStreams.get(streamId);
    if (!streamController) {
        const readableStream = new ReadableStream({
            start(controller) {
                transmittingDotNetToJSStreams.set(streamId, controller);
                streamController = controller;
            },
        });
        DotNet.jsCallDispatcher.supplyDotNetStream(streamId, readableStream);
    }
    if (errorMessage) {
        streamController.error(errorMessage);
        transmittingDotNetToJSStreams.delete(streamId);
    }
    else if (bytesRead === 0) {
        streamController.close();
        transmittingDotNetToJSStreams.delete(streamId);
    }
    else {
        streamController.enqueue(data.length === bytesRead ? data : data.subarray(0, bytesRead));
    }
}
//# sourceMappingURL=StreamingInterop.js.map