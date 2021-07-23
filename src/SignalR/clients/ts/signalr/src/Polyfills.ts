// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// Not exported from index

/** @private */
export type EventSourceConstructor = new(url: string, eventSourceInitDict?: EventSourceInit) => EventSource;

/** @private */
export interface WebSocketConstructor {
    new(url: string, protocols?: string | string[], options?: any): WebSocket;
    readonly CLOSED: number;
    readonly CLOSING: number;
    readonly CONNECTING: number;
    readonly OPEN: number;
}
