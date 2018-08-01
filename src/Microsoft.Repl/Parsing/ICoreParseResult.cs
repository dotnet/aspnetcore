// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;

namespace Microsoft.Repl.Parsing
{
    public interface ICoreParseResult
    {
        int CaretPositionWithinCommandText { get; }

        int CaretPositionWithinSelectedSection { get; }

        string CommandText { get; }

        IReadOnlyList<string> Sections { get; }

        bool IsQuotedSection(int index);

        int SelectedSection { get; }

        IReadOnlyDictionary<int, int> SectionStartLookup { get; }

        ICoreParseResult Slice(int numberOfLeadingSectionsToRemove);
    }
}
