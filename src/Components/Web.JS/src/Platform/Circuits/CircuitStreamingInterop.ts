import { HubConnection } from '@microsoft/signalr';

export function sendJSDataStream(connection: HubConnection, data: ArrayBufferView, streamId: string, chunkSize: number) {
    // Run the rest in the background, without delaying the completion of the call to sendJSDataStream
    // otherwise we'll deadlock (.NET can't begin reading until this completes, but it won't complete
    // because nobody's reading the pipe)
    setTimeout(async () => {
        const maxMillisecondsBetweenAcks = 500;
        let numChunksUntilNextAck = 5;
        let lastAckTime = new Date().valueOf();
        try {
            let position = 0;

            // Note: The server-side `StreamBufferCapacity` option (defaults to 10) can be configured to limit how many
            // stream items from the client (per stream) will be stored before reading any more stream items (thus applying backpressure).
            while (position < data.byteLength) {
                const nextChunkSize = Math.min(chunkSize, data.byteLength - position);
                const nextChunkData = new Uint8Array(data.buffer, data.byteOffset + position, nextChunkSize);

                numChunksUntilNextAck--;
                if (numChunksUntilNextAck > 1) {
                    // Most of the time just send and buffer within the network layer
                    await connection.send('ReceiveJSDataChunk', streamId, nextChunkData, null);
                } else {
                    // But regularly, wait for an ACK, so other events can be interleaved
                    // The use of "invoke" (not "send") here is what prevents the JS side from queuing up chunks
                    // faster than the .NET side can receive them. It means that if there are other user interactions
                    // while the transfer is in progress, they would get inserted in the middle, so it would be
                    // possible to navigate away or cancel without first waiting for all the remaining chunks.
                    const streamIsAlive = await connection.invoke<boolean>('ReceiveJSDataChunk', streamId, nextChunkData, null);

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
            }
        } catch (error) {
            await connection.send('ReceiveJSDataChunk', streamId, null, error.toString());
        }
    }, 0);
};
