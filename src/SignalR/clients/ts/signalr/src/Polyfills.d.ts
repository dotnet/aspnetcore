/** @private */
export type EventSourceConstructor = new (url: string, eventSourceInitDict?: EventSourceInit) => EventSource;
/** @private */
export interface WebSocketConstructor {
    new (url: string, protocols?: string | string[], options?: any): WebSocket;
    readonly CLOSED: number;
    readonly CLOSING: number;
    readonly CONNECTING: number;
    readonly OPEN: number;
}
//# sourceMappingURL=Polyfills.d.ts.map