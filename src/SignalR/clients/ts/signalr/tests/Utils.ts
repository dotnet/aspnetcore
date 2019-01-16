// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

jasmine.DEFAULT_TIMEOUT_INTERVAL = 20000;

export function registerUnhandledRejectionHandler(): void {
    process.on("unhandledRejection", (error) => {
        if (error && error.stack) {
            console.error(error.stack);
        } else {
            console.error(error);
        }
    });
}

export function delay(durationInMilliseconds: number): Promise<void> {
    const source = new PromiseSource<void>();
    setTimeout(() => source.resolve(), durationInMilliseconds);
    return source.promise;
}

export class PromiseSource<T = void> implements Promise<T> {
    public promise: Promise<T>;

    private resolver!: (value?: T | PromiseLike<T>) => void;
    private rejecter!: (reason?: any) => void;

    constructor() {
        this.promise = new Promise<T>((resolve, reject) => {
            this.resolver = resolve;
            this.rejecter = reject;
        });
    }

    public resolve(value?: T | PromiseLike<T>) {
        this.resolver(value);
    }

    public reject(reason?: any) {
        this.rejecter(reason);
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
    private atSyncPoint: PromiseSource;
    private continueFromSyncPoint: PromiseSource;

    constructor() {
        this.atSyncPoint = new PromiseSource();
        this.continueFromSyncPoint = new PromiseSource();
    }

    public waitForSyncPoint(): Promise<void> {
        return this.atSyncPoint.promise;
    }

    public continue() {
        this.continueFromSyncPoint.resolve();
    }

    public waitToContinue(): Promise<void> {
        this.atSyncPoint.resolve();
        return this.continueFromSyncPoint.promise;
    }
}
