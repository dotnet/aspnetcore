// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

// TODO: Seamless RxJs integration
// From RxJs: https://github.com/ReactiveX/rxjs/blob/master/src/Observer.ts
export interface Observer<T> {
    closed?: boolean;
    next: (value: T) => void;
    error: (err: any) => void;
    complete: () => void;
}

export interface Observable<T> {
    // TODO: Return a Subscription so the caller can unsubscribe? IDisposable in System.IObservable
    subscribe(observer: Observer<T>): void;
}

export class Subject<T> implements Observable<T> {
    observers: Observer<T>[];

    constructor() {
        this.observers = [];
    }

    public next(item: T): void {
        for (let observer of this.observers) {
            observer.next(item);
        }
    }

    public error(err: any): void {
        for (let observer of this.observers) {
            observer.error(err);
        }
    }

    public complete(): void {
        for (let observer of this.observers) {
            observer.complete();
        }
    }

    public subscribe(observer: Observer<T>): void {
        this.observers.push(observer);
    }
}
