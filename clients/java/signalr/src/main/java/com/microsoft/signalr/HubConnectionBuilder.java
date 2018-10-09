// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

package com.microsoft.signalr;

public class HubConnectionBuilder {
    private String url;
    private Transport transport;
    private Logger logger;
    private HttpConnectionOptions options = null;

    private HubConnectionBuilder(String url) {
        this.url = url;
    }

    public static HubConnectionBuilder create(String url) {
        if (url == null || url.isEmpty()) {
            throw new IllegalArgumentException("A valid url is required.");
        }
        return new HubConnectionBuilder(url);
    }

    public HubConnectionBuilder withTransport(Transport transport) {
        this.transport = transport;
        return this;
    }

    public HubConnectionBuilder withOptions(HttpConnectionOptions options) {
        this.options = options;
        return this;
    }

    public HubConnectionBuilder configureLogging(LogLevel logLevel) {
        this.logger = new ConsoleLogger(logLevel);
        return this;
    }

    public HubConnectionBuilder configureLogging(Logger logger) {
        this.logger = logger;
        return this;
    }

    public HubConnection build() {
        if (options == null) {
            options = new HttpConnectionOptions();
        }
        if (options.getTransport() == null && this.transport != null) {
            options.setTransport(this.transport);
        }
        if (options.getLogger() == null && options.getLoglevel() != null) {
            options.setLogger(new ConsoleLogger(options.getLoglevel()));
        }
        if (options.getLogger() == null && this.logger != null) {
            options.setLogger(this.logger);
        }

        return new HubConnection(url, options);
    }
}