import { DataReceived, ConnectionClosed } from "./Common"

export interface IConnection {
    start(transportName: string): Promise<void>;
    send(data: any): Promise<void>;
    stop(): void;

    onDataReceived: DataReceived;
    onClosed: ConnectionClosed;
}