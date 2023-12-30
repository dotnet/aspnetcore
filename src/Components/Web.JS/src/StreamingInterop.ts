// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import { DotNet } from '@microsoft/dotnet-js-interop';

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

function getChunkFromArrayBufferView(data: ArrayBufferView, position: number, nextChunkSize: number): Uint8Array {
  const nextChunkData = new Uint8Array(data.buffer, data.byteOffset + position, nextChunkSize);
  return nextChunkData;
}

const transmittingDotNetToJSStreams = new Map<number, ReadableStreamController<any>>();
export function receiveDotNetDataStream(dispatcher: DotNet.ICallDispatcher, streamId: number, data: Uint8Array, bytesRead: number, errorMessage: string): void {
  let streamController = transmittingDotNetToJSStreams.get(streamId);
  if (!streamController) {
    const readableStream = new ReadableStream({
      start(controller) {
        transmittingDotNetToJSStreams.set(streamId, controller);
        streamController = controller;
      },
    });

    dispatcher.supplyDotNetStream(streamId, readableStream);
  }

  if (errorMessage) {
    streamController!.error(errorMessage);
    transmittingDotNetToJSStreams.delete(streamId);
  } else if (bytesRead === 0) {
    streamController!.close();
    transmittingDotNetToJSStreams.delete(streamId);
  } else {
    streamController!.enqueue(data.length === bytesRead ? data : data.subarray(0, bytesRead));
  }
}
