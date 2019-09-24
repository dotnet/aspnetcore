// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

// Rough polyfill of https://developer.mozilla.org/en-US/docs/Web/API/AbortController
// We don't actually ever use the API being polyfilled, we always use the polyfill because
// it's a very new API right now.

// Not exported from index.
/** @private */
export class AbortController implements AbortSignal {
    private isAborted: boolean = false;
    public onabort: (() => void) | null = null;

    public abort() {
        if (!this.isAborted) {
            this.isAborted = true;
            if (this.onabort) {
                this.onabort();
            }
        }
    }

    get signal(): AbortSignal {
        return this;
    }

    get aborted(): boolean {
        return this.isAborted;
    }
}

/** Represents a signal that can be monitored to determine if a request has been aborted. */
export interface AbortSignal {
    /** Indicates if the request has been aborted. */
    aborted: boolean;
    /** Set this to a handler that will be invoked when the request is aborted. */
    onabort: (() => void) | null;
}
