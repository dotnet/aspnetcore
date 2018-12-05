// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Razor.Language;
using Microsoft.VisualStudio.Text;

namespace Microsoft.VisualStudio.Test
{
    public class TestEdit
    {
        public TestEdit(SourceChange  change, ITextSnapshot oldSnapshot, ITextSnapshot newSnapshot)
        {
            Change = change;
            OldSnapshot = oldSnapshot;
            NewSnapshot = newSnapshot;
        }

        public TestEdit(int position, int oldLength, ITextSnapshot oldSnapshot, int newLength, ITextSnapshot newSnapshot, string newText)
        {
            Change = new SourceChange(position, oldLength, newText);
            OldSnapshot = oldSnapshot;
            NewSnapshot = newSnapshot;
        }

        public SourceChange Change { get; }

        public ITextSnapshot OldSnapshot { get; }

        public ITextSnapshot NewSnapshot { get; }
    }
}
