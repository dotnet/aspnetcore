// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.VisualStudio.Text;

namespace Microsoft.VisualStudio.Editor.Razor
{
    public sealed class DocumentStructureChangedEventArgs : EventArgs
    {
        public DocumentStructureChangedEventArgs(
            SourceChange change,
            ITextSnapshot snapshot,
            RazorCodeDocument codeDocument)
        {
            SourceChange = change;
            Snapshot = snapshot;
            CodeDocument = codeDocument;
        }

        /// <summary>
        /// The <see cref="AspNetCore.Razor.Language.SourceChange"/> which triggered the re-parse.
        /// </summary>
        public SourceChange SourceChange { get; }

        /// <summary>
        /// The text snapshot used in the re-parse.
        /// </summary>
        public ITextSnapshot Snapshot { get; }

        /// <summary>
        /// The result of the parsing and code generation.
        /// </summary>
        public RazorCodeDocument CodeDocument { get; }
    }
}
