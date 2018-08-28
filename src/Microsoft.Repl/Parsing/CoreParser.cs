// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;

namespace Microsoft.Repl.Parsing
{
    public class CoreParser : IParser<ICoreParseResult>
    {
        public ICoreParseResult Parse(string commandText, int caretPosition)
        {
            List<string> sections = commandText.Split(' ').ToList();
            Dictionary<int, int> sectionStartLookup = new Dictionary<int, int>();
            HashSet<int> quotedSections = new HashSet<int>();
            int runningIndex = 0;
            int selectedSection = -1;
            int caretPositionWithinSelectedSection = 0;
            bool isInQuotedSection = false;

            for (int i = 0; i < sections.Count; ++i)
            {
                int thisSectionLength = sections[i].Length;
                bool isLastSection = i == sections.Count - 1;

                //If currently in a quoted section, combine with the previous section, check to see if this section closes the quotes
                if (isInQuotedSection)
                {
                    //Combine with the previous section
                    sections[i - 1] += " " + sections[i];
                    sections.RemoveAt(i--);

                    //Check for the closing quote
                    int sectionLength = sections[i].Length;
                    if (sections[i][sectionLength - 1] == '"')
                    {
                        if (sectionLength > 1 && sections[i][sectionLength - 2] != '\\')
                        {
                            isInQuotedSection = false;
                        }
                    }
                }
                //Not in a quoted section, check to see if we're starting one
                else
                {
                    sectionStartLookup[i] = runningIndex;

                    if (sections[i].Length > 0)
                    {
                        if (sections[i][0] == '"')
                        {
                            isInQuotedSection = true;
                        }
                    }
                }

                //Update the running index, adding one for all but the last element to account for the spaces between the sections
                runningIndex += thisSectionLength + (isLastSection ? 0 : 1);

                //If the selected section hasn't been determined yet, and the end of the text is past the caret, set the selected
                //  section to the current section and set the initial value for the caret position within the selected section.
                //  Note that the caret position within the selected section, unlike the other positions, accounts for escape
                //  sequences and must be fixed up when escape sequences are removed
                if (selectedSection == -1 && runningIndex > caretPosition)
                {
                    selectedSection = i;
                    caretPositionWithinSelectedSection = caretPosition - sectionStartLookup[i];
                }
            }

            //Unescape the sections
            //  Note that this isn't combined with the above loop to avoid additional complexity in the quoted section case
            for (int i = 0; i < sections.Count; ++i)
            {
                string s = sections[i];

                //Trim quotes if needed
                if (s.Length > 1)
                {
                    if (s[0] == s[s.Length - 1] && s[0] == '"')
                    {
                        s = s.Substring(1, s.Length - 2);
                        quotedSections.Add(i);

                        //Fix up the caret position in the text
                        if (selectedSection == i)
                        {
                            //If the caret was on the closing quote, back up to the last character of the section
                            if (caretPositionWithinSelectedSection == s.Length - 1)
                            {
                                caretPositionWithinSelectedSection -= 2;
                            }
                            //If the caret was after the opening quote, back up one
                            else if (caretPositionWithinSelectedSection > 0)
                            {
                                --caretPositionWithinSelectedSection;
                            }
                        }
                    }
                }

                for (int j = 0; j < s.Length - 1; ++j)
                {
                    if (s[j] == '\\')
                    {
                        if (s[j + 1] == '\\' || s[j + 1] == '"')
                        {
                            s = s.Substring(0, j) + s.Substring(j + 1);

                            //If we're changing the selected section, and we're removing a character
                            //  from before the caret position, back the caret position up to account for it
                            if (selectedSection == i && j < caretPositionWithinSelectedSection)
                            {
                                --caretPositionWithinSelectedSection;
                            }
                        }
                    }
                }

                sections[i] = s;
            }

            if (selectedSection == -1)
            {
                selectedSection = sections.Count - 1;
                caretPositionWithinSelectedSection = sections[selectedSection].Length;
            }

            return new CoreParseResult(caretPosition, caretPositionWithinSelectedSection, commandText, sections, selectedSection, sectionStartLookup, quotedSections);
        }
    }
}
