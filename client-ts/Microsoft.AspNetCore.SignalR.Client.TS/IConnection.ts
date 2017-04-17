import { DataReceived, ConnectionClosed } from "./Common"
import { TransportType, ITransport } from  "./Transports"

export interface IConnection {
    start(transportType: TransportType | ITransport): Promise<void>;
    send(data: any): Promise<void>;
    stop(): void;

    onDataReceived: DataReceived;
    onClosed: ConnectionClosed;
}