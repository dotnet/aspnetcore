// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Threading.Tasks;

namespace Microsoft.AspNet.Mvc
{
    /// <summary>
    /// Reads an object from the request body.
    /// </summary>
    public interface IInputFormatter
    {
        /// <summary>
        /// Determines whether this <see cref="IInputFormatter"/> can de-serialize
        /// an object of the specified type.
        /// </summary>
        /// <param name="context">Input formatter context associated with this call.</param>
        /// <returns>True if this <see cref="IInputFormatter"/> supports the passed in
        /// request's content-type and is able to de-serialize the request body.
        /// False otherwise.</returns>
        bool CanRead(InputFormatterContext context);

        /// <summary>
        /// Called during deserialization to read an object from the request.
        /// </summary>
        /// <param name="context">Input formatter context associated with this call.</param>
        /// <returns>A task that deserializes the request body.</returns>
        Task<object> ReadAsync(InputFormatterContext context);
    }
}
