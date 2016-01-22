// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Mvc.Formatters;

namespace Microsoft.AspNetCore.Mvc.ApiExplorer
{
    /// <summary>
    /// Represents a possible format for the body of a response.
    /// </summary>
    public class ApiResponseFormat
    {
        /// <summary>
        /// The formatter used to output this response.
        /// </summary>
        public IOutputFormatter Formatter { get; set; }

        /// <summary>
        /// The media type of the response.
        /// </summary>
        public string MediaType { get; set; }
    }
}