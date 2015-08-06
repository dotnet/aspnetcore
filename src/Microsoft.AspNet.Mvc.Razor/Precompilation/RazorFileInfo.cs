// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNet.Mvc.Razor.Precompilation
{
    /// <summary>
    /// Metadata for precompiled files.
    /// </summary>
    public class RazorFileInfo
    {
        /// <summary>
        /// Type name including namespace.
        /// </summary>
        public string FullTypeName { get; set; }

        /// <summary>
        /// Path to to the file relative to the application base.
        /// </summary>
        public string RelativePath { get; set; }
    }
}