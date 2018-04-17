// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

// This is an API that is similar to Observable, but we don't want users to confuse it for that so we rename things. Someone could
// easily adapt it into the Rx interface if they wanted to. Unlike in C#, we can't just implement an "interface" and get extension
// methods for free. The methods have to actually be added to the object (there are no extension methods in JS!). We don't want to
// depend on RxJS in the core library, so instead we duplicate the minimum logic needed and then users can easily adapt these into
// proper RxJS observables if they want.

export interface IStreamSubscriber<T> {
    closed?: boolean;
    next(value: T): void;
    error(err: any): void;
    complete(): void;
}

export interface IStreamResult<T> {
    subscribe(observer: IStreamSubscriber<T>): ISubscription<T>;
}

export interface ISubscription<T> {
    dispose(): void;
}
