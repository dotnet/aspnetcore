// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

jasmine.DEFAULT_TIMEOUT_INTERVAL = 20000;

export function registerUnhandledRejectionHandler(): void {
    process.on("unhandledRejection", (error) => {
        if (error && (error as Error).stack) {
            console.error((error as Error).stack);
        } else {
            console.error(error);
        }
    });
}

export function delayUntil(timeoutInMilliseconds: number, condition?: () => boolean): Promise<void> {
    const source = new PromiseSource<void>();
    let timeWait: number = 0;
    const interval = setInterval(() => {
        timeWait += 10;
        if (condition) {
            if (condition() === true) {
                source.resolve();
                clearInterval(interval);
            } else if (timeoutInMilliseconds <= timeWait) {
                source.reject(new Error("Timed out waiting for condition"));
                clearInterval(interval);
            }
        } else if (timeoutInMilliseconds <= timeWait) {
            source.resolve();
            clearInterval(interval);
        }
    }, 10);
    return source.promise;
}

export class PromiseSource<T = void> implements Promise<T> {
    public promise: Promise<T>;

    private _resolver!: (value: T | PromiseLike<T>) => void;
    private _rejecter!: (reason?: any) => void;

    constructor() {
        this.promise = new Promise<T>((resolve, reject) => {
            this._resolver = resolve;
            this._rejecter = reject;
        });
    }

    public [Symbol.toStringTag]: string;

    // @ts-ignore: onfinally not used
    public finally(onfinally?: (() => void) | null): Promise<T> {
        throw new Error("Method not implemented.");
    }

    public resolve(value: T | PromiseLike<T>) {
        this._resolver(value);
    }

    public reject(reason?: any) {
        this._rejecter(reason);
    }

    // Look like a promise so we can be awaited directly;
    public then<TResult1 = T, TResult2 = never>(onfulfilled?: (value: T) => TResult1 | PromiseLike<TResult1>, onrejected?: (reason: any) => TResult2 | PromiseLike<TResult2>): Promise<TResult1 | TResult2> {
        return this.promise.then(onfulfilled, onrejected);
    }
    public catch<TResult = never>(onrejected?: (reason: any) => TResult | PromiseLike<TResult>): Promise<T | TResult> {
        return this.promise.catch(onrejected);
    }
}

export class SyncPoint {
    private _atSyncPoint: PromiseSource;
    private _continueFromSyncPoint: PromiseSource;

    constructor() {
        this._atSyncPoint = new PromiseSource();
        this._continueFromSyncPoint = new PromiseSource();
    }

    public waitForSyncPoint(): Promise<void> {
        return this._atSyncPoint.promise;
    }

    public continue() {
        this._continueFromSyncPoint.resolve();
    }

    public waitToContinue(): Promise<void> {
        this._atSyncPoint.resolve();
        return this._continueFromSyncPoint.promise;
    }
}
