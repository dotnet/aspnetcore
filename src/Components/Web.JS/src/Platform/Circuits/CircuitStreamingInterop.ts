// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import { HubConnection } from '@microsoft/signalr';
import { getNextChunk } from '../../StreamingInterop';

const enum RemoteJSDataStreamResult {
  StreamDisposed,
  ChunkAccepted,
  ChunkRejectedDueToBackpressure,
}

export function sendJSDataStream(connection: HubConnection, data: ArrayBufferView | Blob, streamId: number, chunkSize: number, jsInteropCallTimeoutMilliseconds: number, onComplete?: () => void): void {
  // Run the rest in the background, without delaying the completion of the call to sendJSDataStream
  // otherwise we'll deadlock (.NET can't begin reading until this completes, but it won't complete
  // because nobody's reading the pipe)
  setTimeout(async () => {
    // Acknowledgements must stay frequent enough for navigation and other hub events to interleave.
    // The previous sender estimated its next window from elapsed acknowledgement time against this bound.
    const maxMillisecondsBetweenAcks = 500;
    const maxChunksInFlight = 100;
    let chunksInFlight = 5;
    let lastAcknowledgementTime = Date.now();
    const maxBackoffMilliseconds = jsInteropCallTimeoutMilliseconds > 0
      ? Math.max(1, Math.min(1000, Math.floor(jsInteropCallTimeoutMilliseconds / 4)))
      : 1000;
    const initialBackoffMilliseconds = Math.min(100, maxBackoffMilliseconds);
    let backoffMilliseconds = initialBackoffMilliseconds;
    try {
      const byteLength = data instanceof Blob ? data.size : data.byteLength;
      let position = 0;
      let chunkId = 0;

      while (position < byteLength) {
        const chunks: {
          data: Awaited<ReturnType<typeof getNextChunk>>;
          size: number;
          id: number;
        }[] = [];
        let batchPosition = position;
        for (let i = 0; i < chunksInFlight && batchPosition < byteLength; i++) {
          const nextChunkSize = Math.min(chunkSize, byteLength - batchPosition);
          chunks.push({
            data: await getNextChunk(data, batchPosition, nextChunkSize),
            size: nextChunkSize,
            id: chunkId + i,
          });
          batchPosition += nextChunkSize;
        }

        const acknowledgementStartTime = Date.now();
        const results = await Promise.all(chunks.map(chunk =>
          connection.invoke<RemoteJSDataStreamResult>('ReceiveJSDataChunk', streamId, chunk.id, chunk.data, null)));
        const acknowledgementLatency = Date.now() - acknowledgementStartTime;

        let acceptedChunks = 0;
        for (const result of results) {
          if (result === RemoteJSDataStreamResult.StreamDisposed) {
            return;
          }

          if (result === RemoteJSDataStreamResult.ChunkRejectedDueToBackpressure) {
            break;
          }

          if (result !== RemoteJSDataStreamResult.ChunkAccepted) {
            throw new Error(`Invalid stream response: ${result}`);
          }

          acceptedChunks++;
        }

        for (let i = acceptedChunks; i < results.length; i++) {
          if (results[i] !== RemoteJSDataStreamResult.ChunkRejectedDueToBackpressure) {
            throw new Error(`Invalid stream response after backpressure: ${results[i]}`);
          }
        }

        for (let i = 0; i < acceptedChunks; i++) {
          position += chunks[i].size;
          chunkId++;
        }

        if (acceptedChunks < chunks.length) {
          chunksInFlight = Math.max(1, Math.floor(chunksInFlight / 2));
          await new Promise(resolve => setTimeout(resolve, Math.max(0, backoffMilliseconds - acknowledgementLatency)));
          backoffMilliseconds = acceptedChunks > 0
            ? initialBackoffMilliseconds
            : Math.min(maxBackoffMilliseconds, backoffMilliseconds * 2);
          continue;
        }

        // Retain the previous sender's estimate: one chunk per acknowledgement interval, targeting
        // an acknowledgement about every 500ms. A slow interval therefore shrinks the next window
        // instead of letting accepted chunks grow it toward the maximum.
        const acknowledgementTime = Date.now();
        const elapsedMilliseconds = Math.max(1, acknowledgementTime - lastAcknowledgementTime);
        lastAcknowledgementTime = acknowledgementTime;
        chunksInFlight = Math.min(
          maxChunksInFlight,
          Math.max(1, Math.round(maxMillisecondsBetweenAcks / elapsedMilliseconds)),
        );
        backoffMilliseconds = initialBackoffMilliseconds;
      }
    } catch (error) {
      await connection.send('ReceiveJSDataChunk', streamId, -1, null, (error as Error).toString());
    } finally {
      onComplete?.();
    }
  }, 0);
}
