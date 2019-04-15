// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

/** An abstraction that controls when the client attempts to reconnect and how many times it does so. */
export interface IReconnectPolicy {
    /** Called after the transport loses the connection.
     *
     * @param {number} previousRetryCount The number of consecutive failed reconnect attempts so far.
     *
     * @param {number} elapsedMilliseconds The amount of time in milliseconds spent reconnecting so far.
     *
     * @returns {number | null} The amount of time in milliseconds to wait before the next reconnect attempt. `null` tells the client to stop retrying and close.
     */
    nextRetryDelayInMilliseconds(previousRetryCount: number, elapsedMilliseconds: number): number | null;
}
