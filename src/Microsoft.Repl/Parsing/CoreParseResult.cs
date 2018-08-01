// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;

namespace Microsoft.Repl.Parsing
{
    public class CoreParseResult : ICoreParseResult
    {
        public CoreParseResult(int caretPositionWithinCommandText, int caretPositionWithinSelectedSection, string commandText, IReadOnlyList<string> sections, int selectedSection, IReadOnlyDictionary<int, int> sectionStartLookup, HashSet<int> quotedSections)
        {
            CaretPositionWithinCommandText = caretPositionWithinCommandText;
            CaretPositionWithinSelectedSection = caretPositionWithinSelectedSection;
            CommandText = commandText;
            Sections = sections;
            SelectedSection = selectedSection;
            SectionStartLookup = sectionStartLookup;
            _quotedSections = quotedSections;
        }

        public int CaretPositionWithinCommandText { get; }

        public int CaretPositionWithinSelectedSection { get; }

        public string CommandText { get; }

        public IReadOnlyList<string> Sections { get; }

        public int SelectedSection { get; }

        public IReadOnlyDictionary<int, int> SectionStartLookup { get; }

        private readonly HashSet<int> _quotedSections;

        public bool IsQuotedSection(int index)
        {
            return _quotedSections.Contains(index);
        }

        public virtual ICoreParseResult Slice(int numberOfLeadingSectionsToRemove)
        {
            if (numberOfLeadingSectionsToRemove == 0)
            {
                return this;
            }

            if (numberOfLeadingSectionsToRemove >= Sections.Count)
            {
                return new CoreParseResult(0, 0, string.Empty, new[] { string.Empty }, 0, new Dictionary<int, int> { { 0, 0 } }, new HashSet<int>());
            }

            string commandText = CommandText.Substring(SectionStartLookup[numberOfLeadingSectionsToRemove]);
            int caretPositionWithinCommandText = CaretPositionWithinCommandText - SectionStartLookup[numberOfLeadingSectionsToRemove];

            if (caretPositionWithinCommandText < 0)
            {
                caretPositionWithinCommandText = 0;
            }

            Dictionary<int, int> sectionStartLookup = new Dictionary<int, int>();
            List<string> sections = new List<string>();
            for (int i = 0; i < Sections.Count - numberOfLeadingSectionsToRemove; ++i)
            {
                sectionStartLookup[i] = SectionStartLookup[numberOfLeadingSectionsToRemove + i] - SectionStartLookup[numberOfLeadingSectionsToRemove];
                sections.Add(Sections[numberOfLeadingSectionsToRemove + i]);
            }

            int selectedSection = SelectedSection - numberOfLeadingSectionsToRemove;

            if (selectedSection < 0)
            {
                selectedSection = 0;
            }

            HashSet<int> quotedSections = new HashSet<int>(_quotedSections.Where(x => x > 0).Select(x => x - 1));
            return new CoreParseResult(caretPositionWithinCommandText, CaretPositionWithinSelectedSection, commandText, sections, selectedSection, sectionStartLookup, quotedSections);
        }
    }
}
