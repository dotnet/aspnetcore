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
