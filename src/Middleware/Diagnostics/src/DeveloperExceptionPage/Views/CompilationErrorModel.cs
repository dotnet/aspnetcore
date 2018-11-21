// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.StackTrace.Sources;

namespace Microsoft.AspNetCore.Diagnostics.RazorViews
{
    /// <summary>
    /// Holds data to be displayed on the compilation error page.
    /// </summary>
    internal class CompilationErrorPageModel
    {
        /// <summary>
        /// Options for what output to display.
        /// </summary>
        public DeveloperExceptionPageOptions Options { get; set; }

        /// <summary>
        /// Detailed information about each parse or compilation error.
        /// </summary>
        public IList<ExceptionDetails> ErrorDetails { get; } = new List<ExceptionDetails>();

        /// <summary>
        /// Gets the generated content that produced the corresponding <see cref="ErrorDetails"/>.
        /// </summary>
        public IList<string> CompiledContent { get; } = new List<string>();
    }
}