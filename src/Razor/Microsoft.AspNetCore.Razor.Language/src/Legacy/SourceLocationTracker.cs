// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Microsoft.AspNetCore.Razor.Language.Legacy;

internal static class SourceLocationTracker
{
    public static SourceLocation Advance(SourceLocation location, string text) =>
        Advance(location, new StringSegment(text));

    public static SourceLocation Advance(SourceLocation location, StringSegment text)
    {
        var absoluteIndex = location.AbsoluteIndex;
        var lineIndex = location.LineIndex;
        var characterIndex = location.CharacterIndex;
        for (var i = 0; i < text.Length; i++)
        {
            var nextCharacter = '\0';
            if (i < text.Length - 1)
            {
                nextCharacter = text[i + 1];
            }
            UpdateCharacterCore(text[i], nextCharacter, ref absoluteIndex, ref lineIndex, ref characterIndex);
        }

        return new SourceLocation(location.FilePath, absoluteIndex, lineIndex, characterIndex);
    }

    internal static void UpdateCharacterCore(char characterRead, char nextCharacter, ref int absoluteIndex, ref int lineIndex, ref int characterIndex)
    {
        absoluteIndex++;

        if (Environment.NewLine.Length == 1 && characterRead == Environment.NewLine[0] ||
            ParserHelpers.IsNewLine(characterRead) && (characterRead != '\r' || nextCharacter != '\n'))
        {
            lineIndex++;
            characterIndex = 0;
        }
        else
        {
            characterIndex++;
        }
    }
}
