import { IStreamResult, IStreamSubscriber, ISubscription } from "./Stream";
/** Stream implementation to stream items to the server. */
export declare class Subject<T> implements IStreamResult<T> {
    constructor();
    next(item: T): void;
    error(err: any): void;
    complete(): void;
    subscribe(observer: IStreamSubscriber<T>): ISubscription<T>;
}
//# sourceMappingURL=Subject.d.ts.map