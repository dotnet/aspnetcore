// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Microsoft.AspNetCore.Mvc.ApiExplorer
{
    /// <summary>
    /// Possible type of the response body which is formatted by <see cref="ApiResponseFormats"/>.
    /// </summary>
    public class ApiResponseType
    {
        /// <summary>
        /// Gets or sets the response formats supported by this type.
        /// </summary>
        public IList<ApiResponseFormat> ApiResponseFormats { get; set; } = new List<ApiResponseFormat>();

        /// <summary>
        /// Gets or sets <see cref="ModelBinding.ModelMetadata"/> for the <see cref="Type"/> or null.
        /// </summary>
        /// <remarks>
        /// Will be null if <see cref="Type"/> is null or void.
        /// </remarks>
        public ModelMetadata ModelMetadata { get; set; }

        /// <summary>
        /// Gets or sets the CLR data type of the response or null.
        /// </summary>
        /// <remarks>
        /// Will be null if the action returns no response, or if the response type is unclear. Use
        /// <c>Microsoft.AspNetCore.Mvc.ProducesAttribute</c> or <c>Microsoft.AspNetCore.Mvc.ProducesResponseTypeAttribute</c> on an action method
        /// to specify a response type.
        /// </remarks>
        public Type Type { get; set; }

        /// <summary>
        /// Gets or sets the HTTP response status code.
        /// </summary>
        public int StatusCode { get; set; }
    }
}