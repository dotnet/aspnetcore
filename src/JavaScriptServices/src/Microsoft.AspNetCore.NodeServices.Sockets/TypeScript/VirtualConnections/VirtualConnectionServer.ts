import { Server, Socket } from 'net';
import { EventEmitter } from 'events';
import { Duplex } from 'stream';
import { VirtualConnection, EndWriteCallback } from './VirtualConnection';

// Keep this in sync with the equivalent constant in the .NET code. Both sides split up their transmissions into frames with this max length,
// and both will reject longer frames.
const MaxFrameBodyLength = 16 * 1024;

/**
 * Accepts connections to a net.Server and adapts them to behave as multiplexed connections. That is, for each physical socket connection,
 * we track a list of 'virtual connections' whose API is a Duplex stream. The remote clients may open and close as many virtual connections
 * as they wish, reading and writing to them independently, without the overhead of establishing new physical connections each time.
 */
export function createInterface(server: Server): EventEmitter {
    const emitter = new EventEmitter();

    server.on('connection', (socket: Socket) => {
        // For each physical socket connection, maintain a set of virtual connections. Issue a notification whenever
        // a new virtual connections is opened.
        const childSockets = new VirtualConnectionsCollection(socket, virtualConnection => {
            emitter.emit('connection', virtualConnection);
        });
    });

    return emitter;
}

/**
 * Tracks the 'virtual connections' associated with a single physical socket connection.
 */
class VirtualConnectionsCollection {
    private _currentFrameHeader: FrameHeader = null;
    private _virtualConnections: { [id: string]: VirtualConnection } = {};

    constructor(private _socket: Socket, private _onVirtualConnectionCallback: (virtualConnection: Duplex) => void) {
        // If the remote end closes the physical socket, treat all the virtual connections as being closed remotely too
        this._socket.on('close', () => {
            Object.getOwnPropertyNames(this._virtualConnections).forEach(id => {
                // A 'null' frame signals that the connection was closed remotely
                this._virtualConnections[id].onReceivedData(null);
            });
        });

        this._socket.on('readable', this._onIncomingDataAvailable.bind(this));
    }

    /**
     * This is called whenever the underlying socket signals that it may have some data available to read. It will synchronously read as many
     * message frames as it can from the underlying socket, opens virtual connections as needed, and dispatches data to them.
     */
    private _onIncomingDataAvailable() {
        let exhaustedAllData = false;

        while (!exhaustedAllData) {
            // We might already have a pending frame header from the previous time this method ran, but if not, that's the next thing we need to read
            if (this._currentFrameHeader === null) {
                this._currentFrameHeader = this._readNextFrameHeader();
            }

            if (this._currentFrameHeader === null) {
                // There's not enough data to fill a frameheader, so wait until more arrives later
                // The next attempt to read from the socket will start from the same place this one did (incomplete reads don't consume any data)
                exhaustedAllData = true;
            } else {
                const frameBodyLength = this._currentFrameHeader.bodyLength;
                const frameBodyOrNull: Buffer = frameBodyLength > 0 ? this._socket.read(this._currentFrameHeader.bodyLength) : null;
                if (frameBodyOrNull !== null || frameBodyLength === 0) {
                    // We have a complete frame header+body pair, so we can now dispatch this to a virtual connection. We set _currentFrameHeader back to null
                    // so that the next thing we try to read is the next frame header.
                    const headerCopy = this._currentFrameHeader;
                    this._currentFrameHeader = null;
                    this._onReceivedCompleteFrame(headerCopy, frameBodyOrNull);
                } else {
                    // There's not enough data to fill the pending frame body, so wait until more arrives later
                    // The next attempt to read from the socket will start from the same place this one did (incomplete reads don't consume any data)
                    exhaustedAllData = true;
                }
            }
        }
    }

    private _onReceivedCompleteFrame(header: FrameHeader, bodyIfNotEmpty: Buffer) {
        // An incoming zero-length frame signals that there's no more data to read.
        // Signal this to the Node stream APIs by pushing a 'null' chunk to it.
        const virtualConnection = this._getOrOpenVirtualConnection(header);
        virtualConnection.onReceivedData(header.bodyLength > 0 ? bodyIfNotEmpty : null);
    }

    private _getOrOpenVirtualConnection(header: FrameHeader) {
        if (this._virtualConnections.hasOwnProperty(header.connectionIdString)) {
            // It's an existing virtual connection
            return this._virtualConnections[header.connectionIdString];
        } else {
            // It's a new one
            return this._openVirtualConnection(header);
        }
    }

    private _openVirtualConnection(header: FrameHeader) {
        const beginWriteCallback = (data, writeCompletedCallback) => {
            // Only send nonempty frames, since empty ones are a signal to close the virtual connection
            if (data.length > 0) {
                this._sendFrame(header.connectionIdBinary, data, writeCompletedCallback);
            }
        };

        const newVirtualConnection = new VirtualConnection(beginWriteCallback);
        newVirtualConnection.on('end', () => {
            // The virtual connection was closed remotely. Clean up locally.
            this._onVirtualConnectionWasClosed(header.connectionIdString);
        });
        newVirtualConnection.on('finish', () => {
            // The virtual connection was closed locally. Clean up locally, and notify the remote that we're done.
            this._onVirtualConnectionWasClosed(header.connectionIdString);
            this._sendFrame(header.connectionIdBinary, new Buffer(0));
        });

        this._virtualConnections[header.connectionIdString] = newVirtualConnection;
        this._onVirtualConnectionCallback(newVirtualConnection);
        return newVirtualConnection;
    }

    /**
     * Attempts to read a complete frame header, synchronously, from the underlying socket.
     * If not enough data is available synchronously, returns null without consuming any data from the socket.
     */
    private _readNextFrameHeader(): FrameHeader {
        const headerBuf: Buffer = this._socket.read(12);
        if (headerBuf !== null) {
            // We have enough data synchronously
            const connectionIdBinary = headerBuf.slice(0, 8);
            const connectionIdString = connectionIdBinary.toString('hex');
            const bodyLength = headerBuf.readInt32LE(8);
            if (bodyLength < 0 || bodyLength > MaxFrameBodyLength) {
                // Throwing here is going to bring down the whole process, so this cannot be allowed to happen in real use.
                // But it won't happen in real use, because this is only used with our .NET client, which doesn't violate this rule.
                throw new Error('Illegal frame body length: ' + bodyLength);
            }

            return { connectionIdBinary, connectionIdString, bodyLength };
        } else {
            // Not enough bytes are available synchronously, so none were consumed
            return null;
        }
    }

    private _sendFrame(connectionIdBinary: Buffer, data: Buffer, callback?: EndWriteCallback) {
        // For all sends other than the last one, only invoke the callback if it failed.
        // Also, only invoke the callback at most once.
        let hasInvokedCallback = false;
        const finalCallback: EndWriteCallback = callback && (error => {
            if (!hasInvokedCallback) {
                hasInvokedCallback = true;
                callback(error);
            }
        });
        const notFinalCallback: EndWriteCallback = callback && (error => {
            if (error) {
                finalCallback(error);
            }
        });

        // The amount of data we're writing might exceed MaxFrameBodyLength, so split into frames as needed.
        // Note that we always send at least one frame, even if it's empty (because that's the close-virtual-connection signal).
        // If needed, this could be changed to send frames asynchronously, so that large sends could proceed in parallel
        // (though that would involve making a clone of 'data', to avoid the risk of it being mutated during the send).
        let bytesSent = 0;
        do {
            const nextFrameBodyLength = Math.min(MaxFrameBodyLength, data.length - bytesSent);
            const isFinalChunk = (bytesSent + nextFrameBodyLength) === data.length;
            this._socket.write(connectionIdBinary, notFinalCallback);
            this._sendInt32LE(nextFrameBodyLength, notFinalCallback);
            this._socket.write(data.slice(bytesSent, bytesSent + nextFrameBodyLength), isFinalChunk ? finalCallback : notFinalCallback);
            bytesSent += nextFrameBodyLength;
        } while (bytesSent < data.length);
    }

    /**
     * Sends a number serialized in the correct format for .NET to receive as a System.Int32
     */
    private _sendInt32LE(value: number, callback?: EndWriteCallback) {
        const buf = new Buffer(4);
        buf.writeInt32LE(value, 0);
        this._socket.write(buf, callback);
    }

    private _onVirtualConnectionWasClosed(id: string) {
        if (this._virtualConnections.hasOwnProperty(id)) {
            delete this._virtualConnections[id];
        }
    }
}

interface FrameHeader {
    connectionIdBinary: Buffer;
    connectionIdString: string;
    bodyLength: number;
}
