import { DataReceived, ConnectionClosed } from "./Common"
import { TransportType } from  "./Transports"

export interface IConnection {
    start(transportType: TransportType): Promise<void>;
    send(data: any): Promise<void>;
    stop(): void;

    onDataReceived: DataReceived;
    onClosed: ConnectionClosed;
}