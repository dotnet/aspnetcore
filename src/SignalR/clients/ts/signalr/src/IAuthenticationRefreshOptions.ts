// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import type { HubConnection } from "./HubConnection";

/** Options for configuring automatic authentication refresh on a {@link @microsoft/signalr.HubConnection}. */
export interface IAuthenticationRefreshOptions {
    /** Enables automatic refresh before the server-reported token expiration. The default is true when authentication refresh is configured. */
    enableAutoRefresh?: boolean;

    /** How far before the server-reported token expiration the client should refresh. The default is 300,000 milliseconds. */
    refreshBeforeExpirationInMilliseconds?: number;
}

/** Context passed to {@link @microsoft/signalr.HubConnection.onAuthenticationRefreshed}. */
export interface AuthenticationRefreshedContext {
    /** The connection whose authentication was refreshed. */
    connection: HubConnection;

    /** The new token lifetime reported by the server, in seconds, or undefined if not provided. */
    newTokenLifetimeInSeconds?: number;

    /** The time at which refresh completed. */
    refreshedAt: Date;
}

/** Context passed to {@link @microsoft/signalr.HubConnection.onAuthenticationRefreshFailed}. */
export interface AuthenticationRefreshFailedContext {
    /** The connection on which the refresh attempt failed. */
    connection: HubConnection;

    /** The error that caused refresh to fail. */
    error: Error;
}
