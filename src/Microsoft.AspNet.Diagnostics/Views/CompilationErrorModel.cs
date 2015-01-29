// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;

namespace Microsoft.AspNet.Diagnostics.Views
{
    /// <summary>
    /// Holds data to be displayed on the compilation error page.
    /// </summary>
    public class CompilationErrorPageModel
    {
        /// <summary>
        /// Options for what output to display.
        /// </summary>
        public ErrorPageOptions Options { get; set; }

        /// <summary>
        /// Detailed information about each parse or compilation error.
        /// </summary>
        public ErrorDetails ErrorDetails { get; set; }
    }
}