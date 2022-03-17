// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

package com.microsoft.signalr;

/**
 * An exception thrown when an http request fails with an unexpected status code.
 */
public class HttpRequestException extends RuntimeException {
    private static final long serialVersionUID = 1L;
    private final int statusCode;

    /**
     * Initializes a new instance of the {@link HttpRequestException} class with a specified error message and http status code.
     *
     * @param message The error message that explains the reason for the exception.
     * @param statusCode The http status code.
     */
    public HttpRequestException(String message, int statusCode) {
        super(message);
        this.statusCode = statusCode;
    }

    /**
     * Gets the status code from an http request.
     *
     * @return The status code from an http request.
     */
    public int getStatusCode() {
        return statusCode;
    }
}