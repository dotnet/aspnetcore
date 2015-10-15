// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNet.Http;

namespace Microsoft.AspNet.Authentication
{
    /// <summary>
    /// Provides error context information to middleware providers.
    /// </summary>
    public class ErrorContext : BaseControlContext
    {
        public ErrorContext(HttpContext context, Exception error)
            : base(context)
        {
            Error = error;
        }

        /// <summary>
        /// User friendly error message for the error.
        /// </summary>
        public Exception Error { get; set; }
    }
}
