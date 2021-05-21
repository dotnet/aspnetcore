// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

#nullable enable

using System;

namespace Microsoft.AspNetCore.Mvc.ModelBinding
{
    /// <summary>
    /// The <see cref="Exception"/> that is added to model state when a model binder for the body of the request is
    /// unable to understand the request content type header.
    /// </summary>
    public class UnsupportedContentTypeException : Exception
    {
        /// <summary>
        /// Creates a new instance of <see cref="UnsupportedContentTypeException"/> with the specified
        /// exception <paramref name="message"/>.
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        public UnsupportedContentTypeException(string? message)
            : base(message)
        {
        }
    }
}
