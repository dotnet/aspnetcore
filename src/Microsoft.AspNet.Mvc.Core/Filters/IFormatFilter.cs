// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.Net.Http.Headers;
using System;

namespace Microsoft.AspNet.Mvc
{
    /// <summary>
    /// Implement this interface if you want to have your own implementation of FormatFilter
    /// </summary>
    public interface IFormatFilter : IFilter
    {
        MediaTypeHeaderValue GetContentTypeForCurrentRequest(FilterContext context);
    }
}