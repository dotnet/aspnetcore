/** Defines the expected type for a receiver of results streamed by the server.
 *
 * @typeparam T The type of the items being sent by the server.
 */
export interface IStreamSubscriber<T> {
    /** A boolean that will be set by the {@link @microsoft/signalr.IStreamResult} when the stream is closed. */
    closed?: boolean;
    /** Called by the framework when a new item is available. */
    next(value: T): void;
    /** Called by the framework when an error has occurred.
     *
     * After this method is called, no additional methods on the {@link @microsoft/signalr.IStreamSubscriber} will be called.
     */
    error(err: any): void;
    /** Called by the framework when the end of the stream is reached.
     *
     * After this method is called, no additional methods on the {@link @microsoft/signalr.IStreamSubscriber} will be called.
     */
    complete(): void;
}
/** Defines the result of a streaming hub method.
 *
 * @typeparam T The type of the items being sent by the server.
 */
export interface IStreamResult<T> {
    /** Attaches a {@link @microsoft/signalr.IStreamSubscriber}, which will be invoked when new items are available from the stream.
     *
     * @param {IStreamSubscriber<T>} observer The subscriber to attach.
     * @returns {ISubscription<T>} A subscription that can be disposed to terminate the stream and stop calling methods on the {@link @microsoft/signalr.IStreamSubscriber}.
     */
    subscribe(subscriber: IStreamSubscriber<T>): ISubscription<T>;
}
/** An interface that allows an {@link @microsoft/signalr.IStreamSubscriber} to be disconnected from a stream.
 *
 * @typeparam T The type of the items being sent by the server.
 */
export interface ISubscription<T> {
    /** Disconnects the {@link @microsoft/signalr.IStreamSubscriber} associated with this subscription from the stream. */
    dispose(): void;
}
//# sourceMappingURL=Stream.d.ts.map