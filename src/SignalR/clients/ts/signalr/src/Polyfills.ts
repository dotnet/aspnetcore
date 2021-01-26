// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

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
