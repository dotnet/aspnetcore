// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Threading.Tasks;

namespace Microsoft.AspNetCore.Mvc.Formatters
{
    /// <summary>
    /// Writes an object to the output stream.
    /// </summary>
    public interface IOutputFormatter
    {
        /// <summary>
        /// Determines whether this <see cref="IOutputFormatter"/> can serialize
        /// an object of the specified type.
        /// </summary>
        /// <param name="context">The formatter context associated with the call.</param>
        /// <returns>Returns <c>true</c> if the formatter can write the response; <c>false</c> otherwise.</returns>
        bool CanWriteResult(OutputFormatterCanWriteContext context);

        /// <summary>
        /// Writes the object represented by <paramref name="context"/>'s Object property.
        /// </summary>
        /// <param name="context">The formatter context associated with the call.</param>
        /// <returns>A Task that serializes the value to the <paramref name="context"/>'s response message.</returns>
        Task WriteAsync(OutputFormatterWriteContext context);
    }
}
