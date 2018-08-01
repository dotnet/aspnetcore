// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.Repl.Commanding
{
    public class InputElement
    {
        public CommandInputLocation Location { get; }

        public string Text { get; }

        public string NormalizedText { get; }

        public InputElement Owner { get; }

        public int ParseResultSectionIndex { get; }

        public InputElement(CommandInputLocation location, string text, string normalizedText, int sectionIndex)
            : this(null, location, text, normalizedText, sectionIndex)
        {
        }

        public InputElement(InputElement owner, CommandInputLocation location, string text, string normalizedText, int sectionIndex)
        {
            Owner = owner;
            Location = location;
            Text = text;
            NormalizedText = normalizedText;
            ParseResultSectionIndex = sectionIndex;
        }
    }
}
