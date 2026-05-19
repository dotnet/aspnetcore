// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import { IStreamResult, IStreamSubscriber, ISubscription } from "./Stream";
import { SubjectSubscription } from "./Utils";

/** Stream implementation to stream items to the server. */
export class Subject<T> implements IStreamResult<T> {
    /** @internal */
    public observers: IStreamSubscriber<T>[];

    /** @internal */
    public cancelCallback?: () => Promise<void>;

    constructor() {
        this.observers = [];
    }

    public next(item: T): void {
        for (const observer of this.observers) {
            observer.next(item);
        }
    }

    public error(err: any): void {
        for (const observer of this.observers) {
            if (observer.error) {
                observer.error(err);
            }
        }
    }

    public complete(): void {
        for (const observer of this.observers) {
            if (observer.complete) {
                observer.complete();
            }
        }
    }

    public subscribe(observer: IStreamSubscriber<T>): ISubscription<T> {
        this.observers.push(observer);
        return new SubjectSubscription(this, observer);
    }
}
