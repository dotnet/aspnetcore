export declare function trySerializeMessage(messageType: string, args: any[]): string | null;
export declare function tryDeserializeMessage(message: string): IpcMessage | null;
export declare function setApplicationIsTerminated(): void;
interface IpcMessage {
    messageType: string;
    args: unknown[];
}
export {};
