import { HubConnection } from '@microsoft/signalr';
import { DotNet } from '../../../../../JSInterop/Microsoft.JSInterop.JS/src/dist/Microsoft.JSInterop';

export function sendJSDataStream(connection: HubConnection, data: ArrayBufferView | DotNet.StreamWithLength, streamId: string, chunkSize: number) {
    // Run the rest in the background, without delaying the completion of the call to sendJSDataStream
    // otherwise we'll deadlock (.NET can't begin reading until this completes, but it won't complete
    // because nobody's reading the pipe)
    setTimeout(async () => {
        const maxMillisecondsBetweenAcks = 500;
        let numChunksUntilNextAck = 5;
        let lastAckTime = new Date().valueOf();
        try {
            let position = 0;
            let chunkId = 0;
            let tmpData = new Uint8Array();
            let reader: ReadableStreamDefaultReader | undefined = data instanceof DotNet.StreamWithLength ? data.stream.getReader() : undefined;

            async function getNextChunk(): Promise<Uint8Array> {
                if (reader === undefined) {
                    throw new Error('Failed to get stream reader.');
                }

                let nextChunk: Uint8Array;

                if (tmpData.length < chunkSize) {
                    const readResult: ReadableStreamDefaultReadResult<any> = await reader.read();
                    const newData = readResult.value as Uint8Array;

                    if (newData === undefined) {
                        return tmpData;
                    }
                    const numberOfBytesFromNewChunk = Math.min(chunkSize - tmpData.length, newData.length);
                    const bytesFromNewChunk = newData.subarray(0, numberOfBytesFromNewChunk);
                    nextChunk = new Uint8Array(Math.min(chunkSize, tmpData.length + numberOfBytesFromNewChunk));
                    nextChunk.set(tmpData);
                    nextChunk.set(bytesFromNewChunk, tmpData.length);
                    tmpData = newData.subarray(numberOfBytesFromNewChunk);
                    return nextChunk;
                }

                nextChunk = tmpData.subarray(0, chunkSize);
                tmpData = tmpData.subarray(chunkSize);
                return nextChunk;
            }

            while (position < data.byteLength) {
                const nextChunkSize = Math.min(chunkSize, data.byteLength - position);
                let nextChunkData: Uint8Array;

                if (data instanceof DotNet.StreamWithLength) {
                    nextChunkData = await getNextChunk();
                } else {
                    nextChunkData = new Uint8Array(data.buffer, data.byteOffset + position, nextChunkSize);
                }

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
            await connection.send('ReceiveJSDataChunk', streamId, -1, null, error.toString());
        }
    }, 0);
};
