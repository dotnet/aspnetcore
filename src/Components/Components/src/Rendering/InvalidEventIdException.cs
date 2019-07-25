// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.AspNetCore.Components.Rendering
{
    /// <summary>
    /// Thrown when the renderer receives an invalid event ID that it can't dispatch.
    /// </summary>
    public class InvalidEventIdException : Exception
    {
        /// <summary>
        /// Initializes a new instance of <see cref="InvalidEventIdException"/>.
        /// </summary>
        /// <param name="message">The message explaining the reason for the error.</param>
        /// <param name="exception">The original exception that caused the issue.</param>
        public InvalidEventIdException(string message, Exception exception) : base(message, exception)
        {
        }

        /// <summary>
        /// Initializes a new instance of <see cref="InvalidEventIdException"/>.
        /// </summary>
        /// <param name="message">The message explaining the reason for the error.</param>
        public InvalidEventIdException(string message) : base(message)
        {
        }
    }
}
