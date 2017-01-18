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