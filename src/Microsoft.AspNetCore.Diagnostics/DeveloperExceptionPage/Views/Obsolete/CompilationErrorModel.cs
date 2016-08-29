// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Builder;

namespace Microsoft.AspNetCore.Diagnostics.Views
{
    /// <summary>
    /// Holds data to be displayed on the compilation error page.
    /// </summary>
    [Obsolete("This type is for internal use only and will be removed in a future version.")]
    public class CompilationErrorPageModel
    {
        /// <summary>
        /// Options for what output to display.
        /// </summary>
        public DeveloperExceptionPageOptions Options { get; set; }

        /// <summary>
        /// Detailed information about each parse or compilation error.
        /// </summary>
        public IList<ErrorDetails> ErrorDetails { get; } = new List<ErrorDetails>();
    }
}