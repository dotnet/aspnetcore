// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.Net.Http.Headers;
using System;

namespace Microsoft.AspNet.Mvc
{
    /// <summary>
    ///  A filter which produces a desired content type for the current request. 
    /// </summary>
    /// <remarks>A FormatFilter decides what content type to use if a format value is present in the URL. 
    /// </remarks>
    public interface IFormatFilter : IFilter
    {
        /// <summary>
        /// Returns true if the filter is going to be executed for the current request.
        /// </summary>
        /// <param name="context">The <see cref="FilterContext"/></param>
        /// <returns>If the filter will execute</returns>
        bool IsActive(FilterContext context);
    }
}