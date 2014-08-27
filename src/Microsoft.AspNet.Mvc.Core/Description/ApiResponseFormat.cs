// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNet.Mvc.HeaderValueAbstractions;

namespace Microsoft.AspNet.Mvc.Description
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
        public MediaTypeHeaderValue MediaType { get; set; }
    }
}