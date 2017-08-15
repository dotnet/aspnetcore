// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.VisualStudio.Text;

namespace Microsoft.VisualStudio.LanguageServices.Razor
{
    /// <summary>
    /// Arguments for the <see cref="RazorEditorParser.DocumentParseComplete"/> event in <see cref="RazorEditorParser"/>.
    /// </summary>
    public sealed class DocumentParseCompleteEventArgs : EventArgs
    {
        public DocumentParseCompleteEventArgs(
            SourceChange change,
            ITextSnapshot buffer,
            bool treeStructureChanged,
            RazorCodeDocument codeDocument)
        {
            SourceChange = change;
            Buffer = buffer;
            TreeStructureChanged = treeStructureChanged;
            CodeDocument = codeDocument;
        }

        /// <summary>
        /// The <see cref="AspNetCore.Razor.Language.SourceChange"/> which triggered the re-parse.
        /// </summary>
        public SourceChange SourceChange { get; }

        /// <summary>
        /// The text snapshot used in the re-parse.
        /// </summary>
        public ITextSnapshot Buffer { get; }

        /// <summary>
        /// Indicates if the tree structure has actually changed since the previous re-parse.
        /// </summary>
        public bool TreeStructureChanged { get; }

        /// <summary>
        /// The result of the parsing and code generation.
        /// </summary>
        public RazorCodeDocument CodeDocument { get; }
    }
}