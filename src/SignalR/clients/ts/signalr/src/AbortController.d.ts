/** @private */
export declare class AbortController implements AbortSignal {
    private _isAborted;
    onabort: (() => void) | null;
    abort(): void;
    get signal(): AbortSignal;
    get aborted(): boolean;
}
/** Represents a signal that can be monitored to determine if a request has been aborted. */
export interface AbortSignal {
    /** Indicates if the request has been aborted. */
    aborted: boolean;
    /** Set this to a handler that will be invoked when the request is aborted. */
    onabort: (() => void) | null;
}
//# sourceMappingURL=AbortController.d.ts.map