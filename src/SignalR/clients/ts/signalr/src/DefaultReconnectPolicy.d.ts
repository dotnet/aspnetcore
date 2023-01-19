import { IRetryPolicy, RetryContext } from "./IRetryPolicy";
/** @private */
export declare class DefaultReconnectPolicy implements IRetryPolicy {
    private readonly _retryDelays;
    constructor(retryDelays?: number[]);
    nextRetryDelayInMilliseconds(retryContext: RetryContext): number | null;
}
//# sourceMappingURL=DefaultReconnectPolicy.d.ts.map