// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.Net.Http.Headers;

namespace Microsoft.AspNet.Mvc.ApiExplorer
{
    /// <summary>
    /// Provides metadata information about the response format to an <c>IApiDescriptionProvider</c>.
    /// </summary>
    /// <remarks>
    /// An <see cref="IOutputFormatter"/> should implement this interface to expose metadata information
    /// to an <c>IApiDescriptionProvider</c>.
    /// </remarks>
    public interface IApiResponseFormatMetadataProvider
    {
        /// <summary>
        /// Gets a filtered list of content types which are supported by the <see cref="IOutputFormatter"/>
        /// for the <paramref name="declaredType"/> and <paramref name="contentType"/>.
        /// </summary>
        /// <param name="declaredType">The declared type for which the supported content types are desired.</param>
        /// <param name="runtimeType">The runtime type for which the supported content types are desired.</param>
        /// <param name="contentType">
        /// The content type for which the supported content types are desired, or <c>null</c> if any content
        /// type can be used.
        /// </param>
        /// <returns>Content types which are supported by the <see cref="IOutputFormatter"/>.</returns>
        IReadOnlyList<MediaTypeHeaderValue> GetSupportedContentTypes(
            Type declaredType,
            Type runtimeType,
            MediaTypeHeaderValue contentType);
    }
}