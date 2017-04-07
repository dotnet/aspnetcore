// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.Razor.Language
{
    /// <summary>
    /// Options for code generation in the <see cref="RazorTemplateEngine"/>.
    /// </summary>
    public class RazorTemplateEngineOptions
    {
        /// <summary>
        /// Gets or sets the file name of the imports file (e.g. _ViewImports.cshtml).
        /// </summary>
        public string ImportsFileName { get; set; }

        /// <summary>
        /// Gets or sets the default set of imports.
        /// </summary>
        public RazorSourceDocument DefaultImports { get; set; }
    }
}
