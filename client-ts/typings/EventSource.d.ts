// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

// Hand-written EventSource typings. I couldn't find anything easy-to-consume out there. This is purely based on the API docs.
// -anurse

interface EventSourceOptions {
    withcredentials: boolean
}

declare class EventSource extends EventTarget {
    constructor(url: string);
    constructor(url: string, configuration: EventSourceOptions);

    readonly CLOSED: number;
    readonly CONNECTING: number;
    readonly OPEN: number;

    close(): void;

    onerror: (this: this, ev: ErrorEvent) => any;
    onmessage: (this: this, ev: MessageEvent) => any;
    onopen: (this: this, ev: Event) => any;
    addEventListener(type: "error", listener: (this: this, ev: ErrorEvent) => any, useCapture?: boolean): void;
    addEventListener(type: "message", listener: (this: this, ev: MessageEvent) => any, useCapture?: boolean): void;
    addEventListener(type: "open", listener: (this: this, ev: Event) => any, useCapture?: boolean): void;
    addEventListener(type: string, listener: EventListenerOrEventListenerObject, useCapture?: boolean): void;

    readonly readyState: number;
    readonly url: string;
}