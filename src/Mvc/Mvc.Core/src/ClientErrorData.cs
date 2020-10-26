// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.Mvc
{
    /// <summary>
    /// Information for producing client errors. This type is used to configure client errors
    /// produced by consumers of <see cref="ApiBehaviorOptions.ClientErrorMapping"/>.
    /// </summary>
    public class ClientErrorData
    {
        /// <summary>
        /// Gets or sets a link (URI) that describes the client error.
        /// </summary>
        /// <remarks>
        /// By default, this maps to <see cref="ProblemDetails.Type"/>.
        /// </remarks>
        public string Link { get; set; }

        /// <summary>
        /// Gets or sets the summary of the client error.
        /// </summary>
        /// <remarks>
        /// By default, this maps to <see cref="ProblemDetails.Title"/> and should not change
        /// between multiple occurrences of the same error.
        /// </remarks>
        public string Title { get; set; }
    }
}
