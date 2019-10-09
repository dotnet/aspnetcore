// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Mvc.Formatters;

namespace Microsoft.AspNetCore.Mvc.ApiExplorer
{
    /// <summary>
    /// A possible format for the body of a request.
    /// </summary>
    public class ApiRequestFormat
    {
        /// <summary>
        /// The formatter used to read this request.
        /// </summary>
        public IInputFormatter Formatter { get; set; }

        /// <summary>
        /// The media type of the request.
        /// </summary>
        public string MediaType { get; set; }
    }
}