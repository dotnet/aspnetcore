// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.Extensions.ApiDescription.Client
{
    internal interface ILogWrapper
    {
        /// <summary>
        /// Logs specified informational <paramref name="message"/>. Implementations should be thread safe.
        /// </summary>
        /// <param name="message">The message to log.</param>
        /// <param name="messageArgs">Optional arguments for formatting the <paramref name="message"/> string.</param>
        /// <exception cref="ArgumentNullException">
        /// Thrown when <paramref name="message"/> is <see langword="null"/>.
        /// </exception>
        void LogInformational(string message, params object[] messageArgs);

        /// <summary>
        /// Logs a warning with the specified <paramref name="message"/>. Implementations should be thread safe.
        /// </summary>
        /// <param name="message">The message to log.</param>
        /// <param name="messageArgs">Optional arguments for formatting the <paramref name="message"/> string.</param>
        /// <exception cref="ArgumentNullException">
        /// Thrown when <paramref name="message"/> is <see langword="null"/>.
        /// </exception>
        void LogWarning(string message, params object[] messageArgs);

        /// <summary>
        /// Logs an error with the specified <paramref name="message"/>. Implementations should be thread safe.
        /// </summary>
        /// <param name="message">The message to log.</param>
        /// <param name="messageArgs">Optional arguments for formatting the <paramref name="message"/> string.</param>
        /// <exception cref="ArgumentNullException">
        /// Thrown when <paramref name="message"/> is <see langword="null"/>.
        /// </exception>
        void LogError(string message, params object[] messageArgs);

        /// <summary>
        /// Logs an error with the message and (optionally) the stack trace of the given <paramref name="exception"/>.
        /// Implementations should be thread safe.
        /// </summary>
        /// <param name="exception">The <see cref="Exception"/> to log.</param>
        /// <param name="showStackTrace">
        /// If <see langword="true"/>, append stack trace to <paramref name="exception"/>'s message.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// Thrown when <paramref name="exception"/> is <see langword="null"/>.
        /// </exception>
        void LogError(Exception exception, bool showStackTrace);
    }
}
