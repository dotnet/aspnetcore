/** Specifies a specific HTTP transport type. */
export declare enum HttpTransportType {
    /** Specifies no transport preference. */
    None = 0,
    /** Specifies the WebSockets transport. */
    WebSockets = 1,
    /** Specifies the Server-Sent Events transport. */
    ServerSentEvents = 2,
    /** Specifies the Long Polling transport. */
    LongPolling = 4
}
/** Specifies the transfer format for a connection. */
export declare enum TransferFormat {
    /** Specifies that only text data will be transmitted over the connection. */
    Text = 1,
    /** Specifies that binary data will be transmitted over the connection. */
    Binary = 2
}
/** An abstraction over the behavior of transports. This is designed to support the framework and not intended for use by applications. */
export interface ITransport {
    connect(url: string, transferFormat: TransferFormat): Promise<void>;
    send(data: any): Promise<void>;
    stop(): Promise<void>;
    onreceive: ((data: string | ArrayBuffer) => void) | null;
    onclose: ((error?: Error) => void) | null;
}
//# sourceMappingURL=ITransport.d.ts.map