// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

export function asyncit(expectation: string, assertion?: () => Promise<any> | void, timeout?: number): void {
    let testFunction: (done: DoneFn) => void;
    if (assertion) {
        testFunction = done => {
            let promise = assertion();
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
    let source = new PromiseSource<void>();
    setTimeout(() => source.resolve(), durationInMilliseconds);
    return source.promise;
}

export class PromiseSource<T> {
    public promise: Promise<T>

    private resolver: (value?: T | PromiseLike<T>) => void;
    private rejecter: (reason?: any) => void;

    constructor() {
        this.promise = new Promise<T>((resolve, reject) => {
            this.resolver = resolve;
            this.rejecter = reject;
        });
    }

    resolve(value?: T | PromiseLike<T>) {
        this.resolver(value);
    }

    reject(reason?: any) {
        this.rejecter(reason);
    }
}
