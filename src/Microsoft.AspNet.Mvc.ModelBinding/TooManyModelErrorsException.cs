// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.AspNet.Mvc.ModelBinding
{
    /// <summary>
    /// The <see cref="Exception"/> that is thrown when too many model errors are encountered.
    /// </summary>
    public class TooManyModelErrorsException : Exception
    {
        /// <summary>
        /// Creates a new instance of <see cref="TooManyModelErrorsException"/> with the specified
        /// exception <paramref name="message"/>.
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        public TooManyModelErrorsException([NotNull] string message)
            : base(message)
        {
        }
    }
}