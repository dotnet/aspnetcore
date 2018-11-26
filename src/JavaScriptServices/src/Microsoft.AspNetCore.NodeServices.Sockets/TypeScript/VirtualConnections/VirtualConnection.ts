import { Duplex } from 'stream';

export type EndWriteCallback = (error?: any) => void;
export type BeginWriteCallback = (data: Buffer, callback: EndWriteCallback) => void;

/**
 * Represents a virtual connection. Multiple virtual connections may be multiplexed over a single physical socket connection.
 */
export class VirtualConnection extends Duplex {
    private _flowing = false;
    private _receivedDataQueue: Buffer[] = [];

    constructor(private _beginWriteCallback: BeginWriteCallback) {
        super();
    }

    public _read() {
        this._flowing = true;

        // Keep pushing data until we run out, or the underlying framework asks us to stop.
        // When we finish, the 'flowing' state is detemined by whether more data is still being requested.
        while (this._flowing && this._receivedDataQueue.length > 0) {
            const nextChunk = this._receivedDataQueue.shift();
            this._flowing = this.push(nextChunk);
        }
    }

    public _write(chunk: Buffer | string, encodingIfString: string, callback: EndWriteCallback) {
        if (typeof chunk === 'string') {
            chunk = new Buffer(chunk as string, encodingIfString);
        }

        this._beginWriteCallback(chunk as Buffer, callback);
    }

    public onReceivedData(dataOrNullToSignalEOF: Buffer) {
        if (this._flowing) {
            this._flowing = this.push(dataOrNullToSignalEOF);
        } else {
            this._receivedDataQueue.push(dataOrNullToSignalEOF);
        }
    }
}
