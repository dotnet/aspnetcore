// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.Net.Http.Headers;
using System;

namespace Microsoft.AspNet.Mvc
{
    /// <summary>
    /// Implement this interface if you want to have your own implementation of FormatFilter. A FormatFilter decides 
    /// what content type to use if the format is present in the Url. 
    /// </summary>
    public interface IFormatFilter : IFilter
    {
        /// <summary>
        /// Get the <see cref="MediaTypeHeaderValue"/> registered fot the format in the request.
        /// </summary>
        MediaTypeHeaderValue GetContentTypeForCurrentRequest(FilterContext context);
    }
}