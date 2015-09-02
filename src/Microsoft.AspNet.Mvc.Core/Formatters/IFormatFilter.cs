// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNet.Mvc.Filters;
using Microsoft.Net.Http.Headers;

namespace Microsoft.AspNet.Mvc.Formatters
{
    /// <summary>
    ///  A filter which produces a desired content type for the current request. 
    /// </summary>
    public interface IFormatFilter : IFilterMetadata
    {
        /// <summary>
        /// format value in the current request. <c>null</c> if format not present in the current request.
        /// </summary>
        string Format { get; }

        /// <summary>
        /// <see cref="MediaTypeHeaderValue"/> for the format value in the current request.
        /// </summary>
        MediaTypeHeaderValue ContentType { get; }

        /// <summary>
        /// <c>true</c> if the current <see cref="FormatFilter"/> is active and will execute. 
        /// </summary>
        bool IsActive { get; }
    }
}