// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import { HubConnection } from '@microsoft/signalr';
import { getNextChunk } from '../../StreamingInterop';

const enum RemoteJSDataStreamResult {
  StreamDisposed,
  ChunkAccepted,
  ChunkRejectedDueToBackpressure,
}

export function sendJSDataStream(connection: HubConnection, data: ArrayBufferView | Blob, streamId: number, chunkSize: number, onComplete?: () => void): void {
  // Run the rest in the background, without delaying the completion of the call to sendJSDataStream
  // otherwise we'll deadlock (.NET can't begin reading until this completes, but it won't complete
  // because nobody's reading the pipe)
  setTimeout(async () => {
    const initialBackoffMilliseconds = 100;
    const maxBackoffMilliseconds = 1000;
    let backoffMilliseconds = initialBackoffMilliseconds;
    try {
      const byteLength = data instanceof Blob ? data.size : data.byteLength;
      let position = 0;
      let chunkId = 0;

      while (position < byteLength) {
        const nextChunkSize = Math.min(chunkSize, byteLength - position);
        const nextChunkData = await getNextChunk(data, position, nextChunkSize);

        const result = await connection.invoke<RemoteJSDataStreamResult>('ReceiveJSDataChunk', streamId, chunkId, nextChunkData, null);
        if (result === RemoteJSDataStreamResult.StreamDisposed) {
          break;
        }

        if (result === RemoteJSDataStreamResult.ChunkRejectedDueToBackpressure) {
          await new Promise(resolve => setTimeout(resolve, backoffMilliseconds));
          backoffMilliseconds = Math.min(maxBackoffMilliseconds, backoffMilliseconds * 2);
          continue;
        }

        if (result !== RemoteJSDataStreamResult.ChunkAccepted) {
          throw new Error(`Invalid stream response: ${result}`);
        }

        position += nextChunkSize;
        chunkId++;
        backoffMilliseconds = initialBackoffMilliseconds;
      }
    } catch (error) {
      try {
        await connection.send('ReceiveJSDataChunk', streamId, -1, null, (error as Error).toString());
      } catch {
        // The connection may have closed while the stream operation was in progress.
      }
    } finally {
      onComplete?.();
    }
  }, 0);
}
