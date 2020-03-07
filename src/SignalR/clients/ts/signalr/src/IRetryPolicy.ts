// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

/** An abstraction that controls when the client attempts to reconnect and how many times it does so. */
export interface IRetryPolicy {
    /** Called after the transport loses the connection.
     *
     * @param {RetryContext} retryContext Details related to the retry event to help determine how long to wait for the next retry.
     *
     * @returns {number | null} The amount of time in milliseconds to wait before the next retry. `null` tells the client to stop retrying.
     */
    nextRetryDelayInMilliseconds(retryContext: RetryContext): number | null;
}

export interface RetryContext {
    /**
     * The number of consecutive failed tries so far.
     */
    readonly previousRetryCount: number;

    /**
     * The amount of time in milliseconds spent retrying so far.
     */
    readonly elapsedMilliseconds: number;

    /**
     * The error that forced the upcoming retry.
     */
    readonly retryReason: Error;
}
