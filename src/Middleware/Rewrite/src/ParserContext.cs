// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Rewrite;

/// <summary>
/// Represents a string iterator, with captures.
/// </summary>
internal sealed class ParserContext
{
    public readonly string Template;
    public int Index { get; set; }
    private int? _mark;

    public ParserContext(string condition)
    {
        Template = condition;
        Index = -1;
    }

    public char Current => (Index < Template.Length && Index >= 0) ? Template[Index] : (char)0;

    public bool Back()
    {
        return --Index >= 0;
    }

    public bool Next()
    {
        return ++Index < Template.Length;
    }

    public bool HasNext()
    {
        return (Index + 1) < Template.Length;
    }

    public void Mark()
    {
        _mark = Index;
    }

    public int GetIndex()
    {
        return Index;
    }

    public string? Capture()
    {
        // TODO make this return a range rather than a string.
        if (_mark.HasValue)
        {
            var value = Template.Substring(_mark.Value, Index - _mark.Value);
            _mark = null;
            return value;
        }
        else
        {
            return null;
        }
    }
}
