// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import { PromiseSource } from "./Utils";

export class TestWebSocket {
    public binaryType: "blob" | "arraybuffer" = "blob";
    public bufferedAmount: number = 0;
    public extensions: string = "";
    public onerror!: ((this: WebSocket, ev: Event) => any);
    public onmessage!: ((this: WebSocket, ev: MessageEvent) => any);
    public protocol: string;
    public readyState: number = 1;
    public url: string;
    public options?: any;
    public closed: boolean = false;

    public static webSocketSet: PromiseSource;
    public static webSocket: TestWebSocket;
    public receivedData: (string | ArrayBuffer | Blob | ArrayBufferView)[];

    private _onopen?: (this: WebSocket, evt: Event) => any;
    public openSet: PromiseSource = new PromiseSource();
    public set onopen(value: (this: WebSocket, evt: Event) => any) {
        this._onopen = value;
        this.openSet.resolve();
    }

    public get onopen(): (this: WebSocket, evt: Event) => any {
        return (e) => {
            this._onopen!(e);
            this.readyState = this.OPEN;
        };
    }

    private _onclose?: (this: WebSocket, evt: Event) => any;
    public closeSet: PromiseSource = new PromiseSource();
    public set onclose(value: (this: WebSocket, evt: Event) => any) {
        this._onclose = value;
        this.closeSet.resolve();
    }

    public get onclose(): (this: WebSocket, evt: Event) => any {
        return (e) => {
            this._onclose!(e);
            this.readyState = this.CLOSED;
        };
    }

    public close(code?: number | undefined, reason?: string | undefined): void {
        this.closed = true;
        const closeEvent = new TestCloseEvent();
        closeEvent.code = code || 1000;
        closeEvent.reason = reason!;
        closeEvent.wasClean = closeEvent.code === 1000;
        this.readyState = this.CLOSED;
        this.onclose(closeEvent);
    }

    public send(data: string | ArrayBuffer | Blob | ArrayBufferView): void {
        if (this.closed) {
            throw new Error(`cannot send from a closed transport: '${data}'`);
        }
        this.receivedData.push(data);
    }

    public addEventListener<K extends "close" | "error" | "message" | "open">(type: K, listener: (this: WebSocket, ev: WebSocketEventMap[K]) => any, options?: boolean | AddEventListenerOptions | undefined): void;
    public addEventListener(type: string, listener: EventListenerOrEventListenerObject, options?: boolean | AddEventListenerOptions | undefined): void;
    public addEventListener(type: any, listener: any, options?: any): void {
        throw new Error("Method not implemented.");
    }
    public removeEventListener<K extends "close" | "error" | "message" | "open">(type: K, listener: (this: WebSocket, ev: WebSocketEventMap[K]) => any, options?: boolean | EventListenerOptions | undefined): void;
    public removeEventListener(type: string, listener: EventListenerOrEventListenerObject, options?: boolean | EventListenerOptions | undefined): void;
    public removeEventListener(type: any, listener: any, options?: any): void {
        throw new Error("Method not implemented.");
    }
    public dispatchEvent(evt: Event): boolean {
        throw new Error("Method not implemented.");
    }

    constructor(url: string, protocols?: string | string[], options?: any) {
        this.url = url;
        this.protocol = protocols ? (typeof protocols === "string" ? protocols : protocols[0]) : "";
        this.receivedData = [];
        this.options = options;

        TestWebSocket.webSocket = this;

        if (TestWebSocket.webSocketSet) {
            TestWebSocket.webSocketSet.resolve();
        }
    }

    public readonly CLOSED: number = 1;
    public static readonly CLOSED: number = 1;
    public readonly CLOSING: number = 2;
    public static readonly CLOSING: number = 2;
    public readonly CONNECTING: number = 3;
    public static readonly CONNECTING: number = 3;
    public readonly OPEN: number = 4;
    public static readonly OPEN: number = 4;
}

export class TestEvent implements Event {
    public composed: boolean = false;
    public composedPath(): EventTarget[];
    public composedPath(): any[] {
        throw new Error("Method not implemented.");
    }
    public bubbles: boolean = false;
    public cancelBubble: boolean = false;
    public cancelable: boolean = false;
    public currentTarget!: EventTarget;
    public defaultPrevented: boolean = false;
    public eventPhase: number = 0;
    public isTrusted: boolean = false;
    public returnValue: boolean = false;
    public scoped: boolean = false;
    public srcElement!: Element | null;
    public target!: EventTarget;
    public timeStamp: number = 0;
    public type: string = "";
    public deepPath(): EventTarget[] {
        throw new Error("Method not implemented.");
    }
    public initEvent(type: string, bubbles?: boolean | undefined, cancelable?: boolean | undefined): void {
        throw new Error("Method not implemented.");
    }
    public preventDefault(): void {
        throw new Error("Method not implemented.");
    }
    public stopImmediatePropagation(): void {
        throw new Error("Method not implemented.");
    }
    public stopPropagation(): void {
        throw new Error("Method not implemented.");
    }
    public AT_TARGET: number = 0;
    public BUBBLING_PHASE: number = 0;
    public CAPTURING_PHASE: number = 0;
    public NONE: number = 0;
}

export class TestErrorEvent {
    public colno: number = 0;
    public error: any;
    public filename: string = "";
    public lineno: number = 0;
    public message: string = "";
    public initErrorEvent(typeArg: string, canBubbleArg: boolean, cancelableArg: boolean, messageArg: string, filenameArg: string, linenoArg: number): void {
        throw new Error("Method not implemented.");
    }
    public bubbles: boolean = false;
    public cancelBubble: boolean = false;
    public cancelable: boolean = false;
    public currentTarget!: EventTarget | null;
    public defaultPrevented: boolean = false;
    public eventPhase: number = 0;
    public isTrusted: boolean = false;
    public returnValue: boolean = false;
    public scoped: boolean = false;
    public srcElement!: Element | null;
    public target!: EventTarget | null;
    public timeStamp: number = 0;
    public type: string = "";
    public deepPath(): EventTarget[] {
        throw new Error("Method not implemented.");
    }
    public initEvent(type: string, bubbles?: boolean | undefined, cancelable?: boolean | undefined): void {
        throw new Error("Method not implemented.");
    }
    public preventDefault(): void {
        throw new Error("Method not implemented.");
    }
    public stopImmediatePropagation(): void {
        throw new Error("Method not implemented.");
    }
    public stopPropagation(): void {
        throw new Error("Method not implemented.");
    }
    public AT_TARGET: number = 0;
    public BUBBLING_PHASE: number = 0;
    public CAPTURING_PHASE: number = 0;
    public NONE: number = 0;
}

export class TestCloseEvent implements Event {
    public composed: boolean = false;
    public composedPath(): EventTarget[];
    public composedPath(): any[] {
        throw new Error("Method not implemented.");
    }
    public code: number = 0;
    public reason: string = "";
    public wasClean: boolean = false;
    public initCloseEvent(typeArg: string, canBubbleArg: boolean, cancelableArg: boolean, wasCleanArg: boolean, codeArg: number, reasonArg: string): void {
        throw new Error("Method not implemented.");
    }
    public bubbles: boolean = false;
    public cancelBubble: boolean = false;
    public cancelable: boolean = false;
    public currentTarget!: EventTarget;
    public defaultPrevented: boolean = false;
    public eventPhase: number = 0;
    public isTrusted: boolean = false;
    public returnValue: boolean = false;
    public scoped: boolean = false;
    public srcElement!: Element | null;
    public target!: EventTarget;
    public timeStamp: number = 0;
    public type: string = "";
    public deepPath(): EventTarget[] {
        throw new Error("Method not implemented.");
    }
    public initEvent(type: string, bubbles?: boolean | undefined, cancelable?: boolean | undefined): void {
        throw new Error("Method not implemented.");
    }
    public preventDefault(): void {
        throw new Error("Method not implemented.");
    }
    public stopImmediatePropagation(): void {
        throw new Error("Method not implemented.");
    }
    public stopPropagation(): void {
        throw new Error("Method not implemented.");
    }
    public AT_TARGET: number = 0;
    public BUBBLING_PHASE: number = 0;
    public CAPTURING_PHASE: number = 0;
    public NONE: number = 0;
}

export class TestMessageEvent implements MessageEvent {
    constructor(data: any) {
        this.data = data;
    }
    initMessageEvent(type: string, bubbles?: boolean | undefined, cancelable?: boolean | undefined, data?: any, origin?: string | undefined, lastEventId?: string | undefined, source?: MessageEventSource | null | undefined, ports?: MessagePort[] | undefined): void {
        throw new Error("Method not implemented.");
    }
    public data: any;
    public lastEventId: string = "";
    public origin: string = "";
    public ports: readonly MessagePort[] = [];
    public source: MessagePort | Window | ServiceWorker | null = null;
    public composed: boolean = false;
    public composedPath(): EventTarget[];
    public composedPath(): any[] {
        throw new Error("Method not implemented.");
    }
    public code: number = 0;
    public reason: string = "";
    public wasClean: boolean = false;
    public initCloseEvent(typeArg: string, canBubbleArg: boolean, cancelableArg: boolean, wasCleanArg: boolean, codeArg: number, reasonArg: string): void {
        throw new Error("Method not implemented.");
    }
    public bubbles: boolean = false;
    public cancelBubble: boolean = false;
    public cancelable: boolean = false;
    public currentTarget!: EventTarget;
    public defaultPrevented: boolean = false;
    public eventPhase: number = 0;
    public isTrusted: boolean = false;
    public returnValue: boolean = false;
    public scoped: boolean = false;
    public srcElement!: Element | null;
    public target!: EventTarget;
    public timeStamp: number = 0;
    public type: string = "";
    public deepPath(): EventTarget[] {
        throw new Error("Method not implemented.");
    }
    public initEvent(type: string, bubbles?: boolean | undefined, cancelable?: boolean | undefined): void {
        throw new Error("Method not implemented.");
    }
    public preventDefault(): void {
        throw new Error("Method not implemented.");
    }
    public stopImmediatePropagation(): void {
        throw new Error("Method not implemented.");
    }
    public stopPropagation(): void {
        throw new Error("Method not implemented.");
    }
    public AT_TARGET: number = 0;
    public BUBBLING_PHASE: number = 0;
    public CAPTURING_PHASE: number = 0;
    public NONE: number = 0;
}
