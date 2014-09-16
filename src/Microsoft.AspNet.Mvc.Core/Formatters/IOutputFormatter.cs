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
        /// for the <paramref name="declaredType"/> and <paramref name="contentType"/>.
        /// </summary>
        /// <param name="declaredType">The declared type for which the supported content types are desired.</param>
        /// <param name="runtimeType">The runtime type for which the supported content types are desired.</param>
        /// <param name="contentType">
        /// The content type for which the supported content types are desired, or <c>null</c> if any content 
        /// type can be used.
        /// </param>
        /// <returns>Content types which are supported by this formatter.</returns>
        /// <remarks>
        /// If the value of <paramref name="contentType"/> is <c>null</c>, then the formatter should return a list
        /// of all content types that it can produce for the given data type. This may occur during content 
        /// negotiation when the HTTP Accept header is not specified, or when no match for the value in the Accept
        /// header can be found.
        /// 
        /// If the value of <paramref name="contentType"/> is not <c>null</c>, then the formatter should return
        /// a list of all content types that it can produce which match the given data type and content type.
        /// </remarks>
        IReadOnlyList<MediaTypeHeaderValue> GetSupportedContentTypes(
            Type declaredType, 
            Type runtimeType, 
            MediaTypeHeaderValue contentType);

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
