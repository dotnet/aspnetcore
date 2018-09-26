// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

package com.microsoft.aspnet.signalr;

public class HttpConnectionOptions {
    private String url;
    private Transport transport;
    private LogLevel loglevel;

    private Logger logger;
    private boolean skipNegotiate;

    public HttpConnectionOptions() {}

    public HttpConnectionOptions(String url, Transport transport, LogLevel logLevel, boolean skipNegotiate) {
        this.url = url;
        this.transport = transport;
        this.skipNegotiate = skipNegotiate;
        this.loglevel = logLevel;
    }

    public HttpConnectionOptions(String url, Transport transport, Logger logger, boolean skipNegotiate) {
        this.url = url;
        this.transport = transport;
        this.skipNegotiate = skipNegotiate;
        this.logger = logger;
    }
    public void setUrl(String url) {
        this.url = url;
    }

    public void setTransport(Transport transport) {
        this.transport = transport;
    }

    public void setLoglevel(LogLevel loglevel) {
        this.loglevel = loglevel;
    }

    public void setSkipNegotiate(boolean skipNegotiate) {
        this.skipNegotiate = skipNegotiate;
    }

    public String getUrl() {
        return url;
    }

    public Transport getTransport() {
        return transport;
    }

    public LogLevel getLoglevel() {
        return loglevel;
    }

    public boolean getSkipNegotiate() {
        return skipNegotiate;
    }

    public Logger getLogger() {
        return logger;
    }

    public void setLogger(Logger logger) {
        this.logger = logger;
    }
}
