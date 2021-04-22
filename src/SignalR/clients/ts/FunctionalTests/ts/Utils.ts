// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

export function getParameterByName(name: string) {
    const url = window.location.href;
    name = name.replace(/[\[\]]/g, "\\$&");
    const regex = new RegExp("[?&]" + name + "(=([^&#]*)|&|#|$)");
    const results = regex.exec(url);
    if (!results) {
        return null;
    }
    if (!results[2]) {
        return "";
    }
    return decodeURIComponent(results[2].replace(/\+/g, " "));
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

    public finally(onfinally?: (() => void) | null): Promise<T> {
        return this.promise.finally(onfinally);
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

    // @ts-ignore: value never read
    public [Symbol.toStringTag]: "Promise";
}
