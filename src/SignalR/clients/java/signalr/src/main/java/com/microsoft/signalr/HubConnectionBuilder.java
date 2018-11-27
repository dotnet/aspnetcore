// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

package com.microsoft.signalr;

/**
 * A builder for configuring {@link HubConnection} instances.
 */
public abstract class HubConnectionBuilder {
    /**
     * Creates a new instance of {@link HttpHubConnectionBuilder}.
     *
     * @param url The URL of the SignalR hub to connect to.
     * @return An instance of {@link HttpHubConnectionBuilder}.
     */
    public static HttpHubConnectionBuilder create(String url) {
        if (url == null || url.isEmpty()) {
            throw new IllegalArgumentException("A valid url is required.");
        }
        return new HttpHubConnectionBuilder(url);
    }

    /**
     * Builds a new instance of {@link HubConnection}.
     *
     * @return A new instance of {@link HubConnection}.
     */
    public abstract HubConnection build();
}