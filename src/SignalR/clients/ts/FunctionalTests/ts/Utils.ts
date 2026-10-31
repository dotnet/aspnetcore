// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

export function getParameterByName(name: string): string | null {
    const url = window.location.href;
    // eslint-disable-next-line no-useless-escape
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

    public resolve(value: T | PromiseLike<T>): void {
        this._resolver(value);
    }

    public reject(reason?: any): void {
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
