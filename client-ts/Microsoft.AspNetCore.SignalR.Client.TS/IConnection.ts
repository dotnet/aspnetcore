import { DataReceived, ConnectionClosed } from "./Common"
import { TransportType, TransferMode, ITransport } from  "./Transports"

export interface IConnection {
    readonly features: any;

    start(): Promise<void>;
    send(data: any): Promise<void>;
    stop(): void;

    onDataReceived: DataReceived;
    onClosed: ConnectionClosed;
}