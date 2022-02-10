// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import { HubConnection } from '@microsoft/signalr';
import { getNextChunk } from '../../StreamingInterop';

export function sendJSDataStream(connection: HubConnection, data: ArrayBufferView | Blob, streamId: number, chunkSize: number): void {
  // Run the rest in the background, without delaying the completion of the call to sendJSDataStream
  // otherwise we'll deadlock (.NET can't begin reading until this completes, but it won't complete
  // because nobody's reading the pipe)
  setTimeout(async () => {
    const maxMillisecondsBetweenAcks = 500;
    let numChunksUntilNextAck = 5;
    let lastAckTime = new Date().valueOf();
    try {
      const byteLength = data instanceof Blob ? data.size : data.byteLength;
      let position = 0;
      let chunkId = 0;

      while (position < byteLength) {
        const nextChunkSize = Math.min(chunkSize, byteLength - position);
        const nextChunkData = await getNextChunk(data, position, nextChunkSize);

        numChunksUntilNextAck--;
        if (numChunksUntilNextAck > 1) {
          // Most of the time just send and buffer within the network layer
          await connection.send('ReceiveJSDataChunk', streamId, chunkId, nextChunkData, null);
        } else {
          // But regularly, wait for an ACK, so other events can be interleaved
          // The use of "invoke" (not "send") here is what prevents the JS side from queuing up chunks
          // faster than the .NET side can receive them. It means that if there are other user interactions
          // while the transfer is in progress, they would get inserted in the middle, so it would be
          // possible to navigate away or cancel without first waiting for all the remaining chunks.
          const streamIsAlive = await connection.invoke<boolean>('ReceiveJSDataChunk', streamId, chunkId, nextChunkData, null);

          // Checks to see if we should continue streaming or if the stream has been cancelled/disposed.
          if (!streamIsAlive) {
            break;
          }

          // Estimate the number of chunks we should send before the next ack to achieve the desired
          // interactivity rate.
          const timeNow = new Date().valueOf();
          const msSinceAck = timeNow - lastAckTime;
          lastAckTime = timeNow;
          numChunksUntilNextAck = Math.max(1, Math.round(maxMillisecondsBetweenAcks / Math.max(1, msSinceAck)));
        }

        position += nextChunkSize;
        chunkId++;
      }
    } catch (error) {
      await connection.send('ReceiveJSDataChunk', streamId, -1, null, (error as Error).toString());
    }
  }, 0);
}
