// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

/** Specifies a specific HTTP transport type. */
export enum HttpTransportType {
    /** Specifies the WebSockets transport. */
    WebSockets,
    /** Specifies the Server-Sent Events transport. */
    ServerSentEvents,
    /** Specifies the Long Polling transport. */
    LongPolling,
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
