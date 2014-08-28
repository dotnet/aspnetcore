// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNet.Http;
using Microsoft.AspNet.Mvc.Core;
using Microsoft.AspNet.Mvc.HeaderValueAbstractions;

namespace Microsoft.AspNet.Mvc
{
    /// <summary>
    /// Writes an object to the output stream.
    /// </summary>
    public interface IOutputFormatter
    {
        /// <summary>
        /// Gets a filtered list of content types which are supported by this formatter 
        /// for the <paramref name="dataType"/> and <paramref name="contentType"/>.
        /// </summary>
        /// <param name="dataType">Type for which the supported content types are desired.</param>
        /// <param name="contentType">Content type for which the supported content types are desired.</param>
        /// <returns>Content types which can are supported by this formatter.</returns>
        IReadOnlyList<MediaTypeHeaderValue> GetSupportedContentTypes(Type dataType, MediaTypeHeaderValue contentType);

        /// <summary>
        /// Determines whether this <see cref="IOutputFormatter"/> can serialize
        /// an object of the specified type.
        /// </summary>
        /// <param name="context">The formatter context associated with the call.</param>
        /// <param name="contentType">The desired contentType on the response.</param>
        /// <returns>True if this <see cref="IOutputFormatter"/> supports the passed in 
        /// <paramref name="contentType"/> and is able to serialize the object
        /// represent by <paramref name="context"/>'s Object property.
        /// False otherwise.</returns>
        bool CanWriteResult(OutputFormatterContext context, MediaTypeHeaderValue contentType);

        /// <summary>
        /// Writes the object represented by <paramref name="context"/>'s Object property.
        /// </summary>
        /// <param name="context">The formatter context associated with the call.</param>
        /// <returns>A Task that serializes the value to the <paramref name="context"/>'s response message.</returns>
        Task WriteAsync(OutputFormatterContext context);
    }
}
