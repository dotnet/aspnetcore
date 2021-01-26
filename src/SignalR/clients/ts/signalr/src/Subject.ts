// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

import { IStreamResult, IStreamSubscriber, ISubscription } from "./Stream";
import { SubjectSubscription } from "./Utils";

/** Stream implementation to stream items to the server. */
export class Subject<T> implements IStreamResult<T> {
    /** @internal */
    public observers: Array<IStreamSubscriber<T>>;

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
