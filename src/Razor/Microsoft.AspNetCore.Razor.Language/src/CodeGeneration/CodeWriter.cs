// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using System.Text;

namespace Microsoft.AspNetCore.Razor.Language.CodeGeneration;

public sealed class CodeWriter
{
    private static readonly char[] NewLineCharacters = { '\r', '\n' };

    private readonly StringBuilder _builder;

    private string _newLine;

    private int _absoluteIndex;
    private int _currentLineIndex;
    private int _currentLineCharacterIndex;

    public CodeWriter() : this(Environment.NewLine, RazorCodeGenerationOptions.CreateDefault())
    {
    }

    public CodeWriter(string newLine, RazorCodeGenerationOptions options)
    {
        NewLine = newLine;
        IndentWithTabs = options.IndentWithTabs;
        TabSize = options.IndentSize;
        _builder = new StringBuilder();
    }

    public int CurrentIndent { get; set; }

    public int Length => _builder.Length;

    public string NewLine
    {
        get => _newLine;
        set
        {
            if (value == null)
            {
                throw new ArgumentNullException(nameof(value));
            }

            if (value != "\r\n" && value != "\n")
            {
                throw new ArgumentException(Resources.FormatCodeWriter_InvalidNewLine(value), nameof(value));
            }

            _newLine = value;
        }
    }

    public bool IndentWithTabs { get; }

    public int TabSize { get; }

    public SourceLocation Location => new SourceLocation(_absoluteIndex, _currentLineIndex, _currentLineCharacterIndex);

    public char this[int index]
    {
        get
        {
            if (index < 0 || index >= _builder.Length)
            {
                throw new IndexOutOfRangeException(nameof(index));
            }

            return _builder[index];
        }
    }

    public CodeWriter Indent(int size)
    {
        if (size == 0 || (Length != 0 && this[Length - 1] != '\n'))
        {
            return this;
        }

        var actualSize = 0;
        if (IndentWithTabs)
        {
            // Avoid writing directly to the StringBuilder here, that will throw off the manual indexing
            // done by the base class.
            var tabs = size / TabSize;
            actualSize += tabs;
            _builder.Append('\t', tabs);

            var spaces = size % TabSize;
            actualSize += spaces;
            _builder.Append(' ', spaces);
        }
        else
        {
            actualSize = size;
            _builder.Append(' ', size);
        }

        _currentLineCharacterIndex += actualSize;
        _absoluteIndex += actualSize;

        return this;
    }

    public CodeWriter Write(string value)
    {
        if (value == null)
        {
            throw new ArgumentNullException(nameof(value));
        }

        return Write(value, 0, value.Length);
    }

    internal CodeWriter Write(StringSegment value)
    {
        return WriteCore(value.Buffer, value.Offset, value.Length);
    }

    public CodeWriter Write(string value, int startIndex, int count)
    {
        if (value == null)
        {
            throw new ArgumentNullException(nameof(value));
        }

        if (startIndex < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(startIndex));
        }

        if (count < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(count));
        }

        if (startIndex > value.Length - count)
        {
            throw new ArgumentOutOfRangeException(nameof(startIndex));
        }

        return WriteCore(value, startIndex, count);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal CodeWriter WriteCore(string value, int startIndex, int count)
    {
        if (count == 0)
        {
            return this;
        }

        Indent(CurrentIndent);

        _builder.Append(value, startIndex, count);

        _absoluteIndex += count;

        // The data string might contain a partial newline where the previously
        // written string has part of the newline.
        var i = startIndex;
        int? trailingPartStart = null;

        if (
            // Check the last character of the previous write operation.
            _builder.Length - count - 1 >= 0 &&
            _builder[_builder.Length - count - 1] == '\r' &&

            // Check the first character of the current write operation.
            _builder[_builder.Length - count] == '\n')
        {
            // This is newline that's spread across two writes. Skip the first character of the
            // current write operation.
            //
            // We don't need to increment our newline counter because we already did that when we
            // saw the \r.
            i += 1;
            trailingPartStart = 1;
        }

        // Iterate the string, stopping at each occurrence of a newline character. This lets us count the
        // newline occurrences and keep the index of the last one.
        while ((i = value.IndexOfAny(NewLineCharacters, i)) >= 0)
        {
            // Newline found.
            _currentLineIndex++;
            _currentLineCharacterIndex = 0;

            i++;

            // We might have stopped at a \r, so check if it's followed by \n and then advance the index to
            // start the next search after it.
            if (count > i &&
                value[i - 1] == '\r' &&
                value[i] == '\n')
            {
                i++;
            }

            // The 'suffix' of the current line starts after this newline token.
            trailingPartStart = i;
        }

        if (trailingPartStart == null)
        {
            // No newlines, just add the length of the data buffer
            _currentLineCharacterIndex += count;
        }
        else
        {
            // Newlines found, add the trailing part of 'data'
            _currentLineCharacterIndex += (count - trailingPartStart.Value);
        }

        return this;
    }

    public CodeWriter WriteLine()
    {
        _builder.Append(NewLine);

        _currentLineIndex++;
        _currentLineCharacterIndex = 0;
        _absoluteIndex += NewLine.Length;

        return this;
    }

    public CodeWriter WriteLine(string value)
    {
        if (value == null)
        {
            throw new ArgumentNullException(nameof(value));
        }

        return Write(value).WriteLine();
    }

    public string GenerateCode()
    {
        return _builder.ToString();
    }
}
