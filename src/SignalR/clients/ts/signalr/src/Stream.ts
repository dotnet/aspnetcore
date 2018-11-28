// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

// This is an API that is similar to Observable, but we don't want users to confuse it for that so we rename things. Someone could
// easily adapt it into the Rx interface if they wanted to. Unlike in C#, we can't just implement an "interface" and get extension
// methods for free. The methods have to actually be added to the object (there are no extension methods in JS!). We don't want to
// depend on RxJS in the core library, so instead we duplicate the minimum logic needed and then users can easily adapt these into
// proper RxJS observables if they want.

/** Defines the expected type for a receiver of results streamed by the server.
 *
 * @typeparam T The type of the items being sent by the server.
 */
export interface IStreamSubscriber<T> {
    /** A boolean that will be set by the {@link @aspnet/signalr.IStreamResult} when the stream is closed. */
    closed?: boolean;
    /** Called by the framework when a new item is available. */
    next(value: T): void;
    /** Called by the framework when an error has occurred.
     *
     * After this method is called, no additional methods on the {@link @aspnet/signalr.IStreamSubscriber} will be called.
     */
    error(err: any): void;
    /** Called by the framework when the end of the stream is reached.
     *
     * After this method is called, no additional methods on the {@link @aspnet/signalr.IStreamSubscriber} will be called.
     */
    complete(): void;
}

/** Defines the result of a streaming hub method.
 *
 * @typeparam T The type of the items being sent by the server.
 */
export interface IStreamResult<T> {
    /** Attaches a {@link @aspnet/signalr.IStreamSubscriber}, which will be invoked when new items are available from the stream.
     *
     * @param {IStreamSubscriber<T>} observer The subscriber to attach.
     * @returns {ISubscription<T>} A subscription that can be disposed to terminate the stream and stop calling methods on the {@link @aspnet/signalr.IStreamSubscriber}.
     */
    subscribe(subscriber: IStreamSubscriber<T>): ISubscription<T>;
}

/** An interface that allows an {@link @aspnet/signalr.IStreamSubscriber} to be disconnected from a stream.
 *
 * @typeparam T The type of the items being sent by the server.
 */
// @ts-ignore: We can't remove this, it's a breaking change, but it's not used.
export interface ISubscription<T> {
    /** Disconnects the {@link @aspnet/signalr.IStreamSubscriber} associated with this subscription from the stream. */
    dispose(): void;
}
