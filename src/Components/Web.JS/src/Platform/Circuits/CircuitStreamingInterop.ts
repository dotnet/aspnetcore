// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import { HubConnection, HubConnectionState } from '@microsoft/signalr';
import { getNextChunk } from '../../StreamingInterop';

type PendingChunkAck = {
  resolve: (streamIsAlive: boolean) => void;
  reject: (error: Error) => void;
};

const pendingAcksByConnection = new WeakMap<HubConnection, Map<string, PendingChunkAck>>();

function getPendingAcks(connection: HubConnection): Map<string, PendingChunkAck> {
  let pending = pendingAcksByConnection.get(connection);
  if (!pending) {
    pending = new Map<string, PendingChunkAck>();
    pendingAcksByConnection.set(connection, pending);
    connection.on('JS.EndReceiveJSDataChunk', (streamId: number, chunkId: number, streamIsAlive: boolean) => {
      const key = `${streamId}:${chunkId}`;
      const entry = pending?.get(key);
      if (entry) {
        pending?.delete(key);
        entry.resolve(streamIsAlive);
      }
    });
    connection.onclose(error => rejectPendingJSDataStreamAcks(connection, error ?? new Error('Connection closed.')));
  }
  return pending;
}

export function rejectPendingJSDataStreamAcks(connection: HubConnection, error: Error): void {
  const pending = pendingAcksByConnection.get(connection);
  if (!pending) {
    return;
  }

  for (const entry of pending.values()) {
    entry.reject(error);
  }
  pending.clear();
}

function waitForChunkAck(connection: HubConnection, streamId: number, chunkId: number): Promise<boolean> {
  const pending = getPendingAcks(connection);
  const key = `${streamId}:${chunkId}`;
  return new Promise<boolean>((resolve, reject) => {
    if (connection.state !== HubConnectionState.Connected) {
      reject(new Error('Cannot await data chunk acknowledgement when the connection is not connected.'));
      return;
    }

    pending.set(key, { resolve, reject });
  });
}

export function sendJSDataStream(connection: HubConnection, data: ArrayBufferView | Blob, streamId: number, chunkSize: number, onComplete?: () => void): void {
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
          await connection.send('ReceiveJSDataChunk', streamId, chunkId, nextChunkData, null, false);
        } else {
          // Regularly wait for server pipe-write completion via JS.EndReceiveJSDataChunk so backpressure
          // and cancellation remain intact without holding a SignalR hub invocation open.
          const ackPromise = waitForChunkAck(connection, streamId, chunkId);
          try {
            await connection.send('ReceiveJSDataChunk', streamId, chunkId, nextChunkData, null, true);
          } catch (sendError) {
            getPendingAcks(connection).delete(`${streamId}:${chunkId}`);
            throw sendError;
          }
          const streamIsAlive = await ackPromise;

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
      await connection.send('ReceiveJSDataChunk', streamId, -1, null, (error as Error).toString(), false);
    } finally {
      onComplete?.();
    }
  }, 0);
}
