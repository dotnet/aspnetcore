// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

package com.microsoft.signalr;

/**
 * An exception thrown when the server fails to invoke a Hub method.
 */
public class HubException extends RuntimeException {
    private static final long serialVersionUID = -572019264269821519L;

    /**
     * Initializes a new instance of the {@link HubException} class.
     */
    public HubException() {
    }

    /**
     * Initializes a new instance of the {@link HubException} class with a specified error message.
     *
     * @param errorMessage The error message that explains the reason for the exception.
     */
    public HubException(String errorMessage) {
        super(errorMessage);
    }

    /**
     * Initializes a new instance of the {@link HubException} class with a specified error message and a reference
     * to the exception that is the cause of this exception.
     *
     * @param errorMessage The error message that explains the reason for the exception.
     * @param innerException The exception that is the cause of the current exception, or null if no inner exception is specified.
     */
    public HubException(String errorMessage, Exception innerException) {
        super(errorMessage, innerException);
    }
}
