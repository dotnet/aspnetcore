// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.AspNetCore.Antiforgery
{
    /// <summary>
    /// The <see cref="Exception"/> that is thrown when the antiforgery token validation fails.
    /// </summary>
    public class AntiforgeryValidationException : Exception
    {
        /// <summary>
        /// Creates a new instance of <see cref="AntiforgeryValidationException"/> with the specified
        /// exception <paramref name="message"/>.
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        public AntiforgeryValidationException(string message)
            : base(message)
        {
        }
    }
}
