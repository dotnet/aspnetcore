import { HubConnection } from '@microsoft/signalr';
export declare function sendJSDataStream(connection: HubConnection, data: ArrayBufferView | Blob, streamId: number, chunkSize: number): void;
