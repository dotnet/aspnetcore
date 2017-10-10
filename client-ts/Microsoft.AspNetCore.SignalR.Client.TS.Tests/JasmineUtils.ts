// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

export function asyncit(expectation: string, assertion?: () => Promise<any>, timeout?: number): void {
    let testFunction: (done: DoneFn) => void;
    if (assertion) {
        testFunction = done => {
            assertion()
                .then(() => done())
                .catch((err) => {
                    fail(err);
                    done();
                });
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