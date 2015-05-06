// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Linq;
using System.Text;
using Microsoft.Framework.Internal;

namespace Microsoft.AspNet.Razor.Parser
{
    internal static class TextReaderExtensions
    {
        public static string ReadUntil([NotNull] this TextReader reader, char terminator)
        {
            return ReadUntil(reader, terminator, inclusive: false);
        }

        public static string ReadUntil([NotNull] this TextReader reader, char terminator, bool inclusive)
        {
            // Rather not allocate an array to use ReadUntil(TextReader, params char[]) so we'll just call the predicate version directly
            return ReadUntil(reader, c => c == terminator, inclusive);
        }

        public static string ReadUntil([NotNull] this TextReader reader, [NotNull] params char[] terminators)
        {
            // NOTE: Using named parameters would be difficult here, hence the inline comment
            return ReadUntil(reader, inclusive: false, terminators: terminators);
        }

        public static string ReadUntil(
            [NotNull] this TextReader reader,
            bool inclusive,
            [NotNull] params char[] terminators)
        {
            return ReadUntil(reader, c => terminators.Any(tc => tc == c), inclusive: inclusive);
        }

        public static string ReadUntil([NotNull] this TextReader reader, [NotNull] Predicate<char> condition)
        {
            return ReadUntil(reader, condition, inclusive: false);
        }

        public static string ReadUntil(
            [NotNull] this TextReader reader,
            [NotNull] Predicate<char> condition,
            bool inclusive)
        {
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

        public static string ReadWhile([NotNull] this TextReader reader, [NotNull] Predicate<char> condition)
        {
            return ReadWhile(reader, condition, inclusive: false);
        }

        public static string ReadWhile(
            [NotNull] this TextReader reader,
            [NotNull] Predicate<char> condition,
            bool inclusive)
        {
            return ReadUntil(reader, ch => !condition(ch), inclusive);
        }

        public static string ReadWhiteSpace([NotNull] this TextReader reader)
        {
            return ReadWhile(reader, c => Char.IsWhiteSpace(c));
        }

        public static string ReadUntilWhiteSpace([NotNull] this TextReader reader)
        {
            return ReadUntil(reader, c => Char.IsWhiteSpace(c));
        }
    }
}
