// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import { IRetryPolicy, RetryContext } from "./IRetryPolicy";

// 0, 2, 10, 30 second delays before reconnect attempts.
const DEFAULT_RETRY_DELAYS_IN_MILLISECONDS = [0, 2000, 10000, 30000, null];

/** @private */
export class DefaultReconnectPolicy implements IRetryPolicy {
    private readonly _retryDelays: (number | null)[];

    constructor(retryDelays?: number[]) {
        this._retryDelays = retryDelays !== undefined ? [...retryDelays, null] : DEFAULT_RETRY_DELAYS_IN_MILLISECONDS;
    }

    public nextRetryDelayInMilliseconds(retryContext: RetryContext): number | null {
        return this._retryDelays[retryContext.previousRetryCount];
    }
}
