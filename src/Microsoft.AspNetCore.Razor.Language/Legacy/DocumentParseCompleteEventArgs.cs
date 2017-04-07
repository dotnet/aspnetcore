// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.AspNetCore.Razor.Language.Legacy
{
    /// <summary>
    /// Arguments for the DocumentParseComplete event in RazorEditorParser
    /// </summary>
    public class DocumentParseCompleteEventArgs : EventArgs
    {
        /// <summary>
        /// Indicates if the tree structure has actually changed since the previous re-parse.
        /// </summary>
        public bool TreeStructureChanged { get; set; }

        /// <summary>
        /// The result of the parsing and code generation.
        /// </summary>
        public RazorCodeDocument GeneratorResults { get; set; }

        /// <summary>
        /// The TextChange which triggered the re-parse
        /// </summary>
        public TextChange SourceChange { get; set; }
    }
}
