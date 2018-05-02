// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

// This will be treated as a bit flag in the future, so we keep it using power-of-two values.
/** Specifies a specific HTTP transport type. */
export enum HttpTransportType {
    /** Specifies no transport preference. */
    None = 0,
    /** Specifies the WebSockets transport. */
    WebSockets = 1,
    /** Specifies the Server-Sent Events transport. */
    ServerSentEvents = 2,
    /** Specifies the Long Polling transport. */
    LongPolling = 4,
}

/** Specifies the transfer format for a connection. */
export enum TransferFormat {
    /** Specifies that only text data will be transmitted over the connection. */
    Text = 1,
    /** Specifies that binary data will be transmitted over the connection. */
    Binary,
}

/** An abstraction over the behavior of transports. This is designed to support the framework and not intended for use by applications. */
export interface ITransport {
    connect(url: string, transferFormat: TransferFormat): Promise<void>;
    send(data: any): Promise<void>;
    stop(): Promise<void>;
    onreceive: (data: string | ArrayBuffer) => void;
    onclose: (error?: Error) => void;
}
