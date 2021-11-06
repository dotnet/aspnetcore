// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using System.Linq;
using System.Text;

namespace Microsoft.AspNetCore.Razor.Language.Legacy;

internal static class TextReaderExtensions
{
    public static string ReadUntil(this TextReader reader, char terminator)
    {
        if (reader == null)
        {
            throw new ArgumentNullException(nameof(reader));
        }

        return ReadUntil(reader, terminator, inclusive: false);
    }

    public static string ReadUntil(this TextReader reader, char terminator, bool inclusive)
    {
        if (reader == null)
        {
            throw new ArgumentNullException(nameof(reader));
        }

        // Rather not allocate an array to use ReadUntil(TextReader, params char[]) so we'll just call the predicate version directly
        return ReadUntil(reader, c => c == terminator, inclusive);
    }

    public static string ReadUntil(this TextReader reader, params char[] terminators)
    {
        if (reader == null)
        {
            throw new ArgumentNullException(nameof(reader));
        }

        if (terminators == null)
        {
            throw new ArgumentNullException(nameof(terminators));
        }

        // NOTE: Using named parameters would be difficult here, hence the inline comment
        return ReadUntil(reader, inclusive: false, terminators: terminators);
    }

    public static string ReadUntil(
        this TextReader reader,
        bool inclusive,
        params char[] terminators)
    {
        if (reader == null)
        {
            throw new ArgumentNullException(nameof(reader));
        }

        if (terminators == null)
        {
            throw new ArgumentNullException(nameof(terminators));
        }

        return ReadUntil(reader, c => terminators.Any(tc => tc == c), inclusive: inclusive);
    }

    public static string ReadUntil(this TextReader reader, Predicate<char> condition)
    {
        if (reader == null)
        {
            throw new ArgumentNullException(nameof(reader));
        }

        if (condition == null)
        {
            throw new ArgumentNullException(nameof(condition));
        }

        return ReadUntil(reader, condition, inclusive: false);
    }

    public static string ReadUntil(
        this TextReader reader,
        Predicate<char> condition,
        bool inclusive)
    {
        if (reader == null)
        {
            throw new ArgumentNullException(nameof(reader));
        }

        if (condition == null)
        {
            throw new ArgumentNullException(nameof(condition));
        }

        var builder = new StringBuilder();
        var ch = -1;
        while ((ch = reader.Peek()) != -1 && !condition((char)ch))
        {
            reader.Read(); // Advance the reader
            builder.Append((char)ch);
        }

        if (inclusive && reader.Peek() != -1)
        {
            builder.Append((char)reader.Read());
        }

        return builder.ToString();
    }

    public static string ReadWhile(this TextReader reader, Predicate<char> condition)
    {
        if (reader == null)
        {
            throw new ArgumentNullException(nameof(reader));
        }

        if (condition == null)
        {
            throw new ArgumentNullException(nameof(condition));
        }

        return ReadWhile(reader, condition, inclusive: false);
    }

    public static string ReadWhile(
        this TextReader reader,
        Predicate<char> condition,
        bool inclusive)
    {
        if (reader == null)
        {
            throw new ArgumentNullException(nameof(reader));
        }

        if (condition == null)
        {
            throw new ArgumentNullException(nameof(condition));
        }

        return ReadUntil(reader, ch => !condition(ch), inclusive);
    }

    public static string ReadWhiteSpace(this TextReader reader)
    {
        if (reader == null)
        {
            throw new ArgumentNullException(nameof(reader));
        }

        return ReadWhile(reader, c => Char.IsWhiteSpace(c));
    }

    public static string ReadUntilWhiteSpace(this TextReader reader)
    {
        if (reader == null)
        {
            throw new ArgumentNullException(nameof(reader));
        }

        return ReadUntil(reader, c => Char.IsWhiteSpace(c));
    }
}
