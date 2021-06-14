// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.Mvc.Infrastructure
{
    /// <summary>
    /// Options for the <see cref="ProblemDetailsMiddleware"/>.
    /// </summary>
    public class ProblemDetailsOptions
    {
        /// <summary>
        /// Create an instance with the default options settings.
        /// </summary>
        public ProblemDetailsOptions()
        {
            ShowStackTrace = false;
        }

        /// <summary>
        /// Determines how many lines of code to include before and after the line of code
        /// present in an exception's stack frame. Only applies when symbols are available and
        /// source code referenced by the exception stack trace is present on the server.
        /// </summary>
        public bool ShowStackTrace { get; set; }
    }
}