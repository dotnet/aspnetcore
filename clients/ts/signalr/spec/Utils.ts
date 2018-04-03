// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

jasmine.DEFAULT_TIMEOUT_INTERVAL = 20000;

export function asyncit(expectation: string, assertion?: () => Promise<any> | void, timeout?: number): void {
    let testFunction: (done: DoneFn) => void;
    if (assertion) {
        testFunction = (done) => {
            const promise = assertion();
            if (promise) {
                promise.then(() => done())
                    .catch((err) => {
                        fail(err);
                        done();
                    });
            } else {
                done();
            }
        };
    }

    it(expectation, testFunction, timeout);
}

export async function captureException(fn: () => Promise<any>): Promise<Error> {
    try {
        await fn();
        return null;
    } catch (e) {
        return e;
    }
}

export function delay(durationInMilliseconds: number): Promise<void> {
    const source = new PromiseSource<void>();
    setTimeout(() => source.resolve(), durationInMilliseconds);
    return source.promise;
}

export class PromiseSource<T> {
    public promise: Promise<T>;

    private resolver: (value?: T | PromiseLike<T>) => void;
    private rejecter: (reason?: any) => void;

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
}