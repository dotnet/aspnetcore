// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Text;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Html;

namespace Microsoft.AspNetCore.Razor.TagHelpers;

/// <summary>
/// Default concrete <see cref="TagHelperContent"/>.
/// </summary>
[DebuggerDisplay("{" + nameof(DebuggerToString) + "(),nq}")]
public class DefaultTagHelperContent : TagHelperContent
{
    private object _singleContent;
    private bool _isSingleContentSet;
    private bool _isModified;
    private bool _hasContent;
    private List<object> _buffer;

    private List<object> Buffer
    {
        get
        {
            if (_buffer == null)
            {
                _buffer = new List<object>();
            }

            if (_isSingleContentSet)
            {
                Debug.Assert(_buffer.Count == 0);

                _buffer.Add(_singleContent);
                _isSingleContentSet = false;
            }

            return _buffer;
        }
    }

    /// <inheritdoc />
    public override bool IsModified => _isModified;

    /// <inheritdoc />
    /// <remarks>Returns <c>true</c> for a cleared <see cref="TagHelperContent"/>.</remarks>
    public override bool IsEmptyOrWhiteSpace
    {
        get
        {
            if (!_hasContent)
            {
                return true;
            }

            using (var writer = new EmptyOrWhiteSpaceWriter())
            {
                if (_isSingleContentSet)
                {
                    return IsEmptyOrWhiteSpaceCore(_singleContent, writer);
                }

                for (var i = 0; i < (_buffer?.Count ?? 0); i++)
                {
                    if (!IsEmptyOrWhiteSpaceCore(Buffer[i], writer))
                    {
                        return false;
                    }
                }
            }

            return true;
        }
    }

    /// <inheritdoc />
    public override TagHelperContent Append(string unencoded) => AppendCore(unencoded);

    /// <inheritdoc />
    public override TagHelperContent AppendHtml(IHtmlContent htmlContent) => AppendCore(htmlContent);

    /// <inheritdoc />
    public override TagHelperContent AppendHtml(string encoded)
    {
        if (encoded == null)
        {
            return AppendCore(null);
        }

        return AppendCore(new HtmlString(encoded));
    }

    /// <inheritdoc />
    public override void CopyTo(IHtmlContentBuilder destination)
    {
        ArgumentNullException.ThrowIfNull(destination);

        if (!_hasContent)
        {
            return;
        }

        if (_isSingleContentSet)
        {
            CopyToCore(_singleContent, destination);
        }
        else
        {
            for (var i = 0; i < (_buffer?.Count ?? 0); i++)
            {
                CopyToCore(Buffer[i], destination);
            }
        }
    }

    /// <inheritdoc />
    public override void MoveTo(IHtmlContentBuilder destination)
    {
        ArgumentNullException.ThrowIfNull(destination);

        if (!_hasContent)
        {
            return;
        }

        if (_isSingleContentSet)
        {
            MoveToCore(_singleContent, destination);
        }
        else
        {
            for (var i = 0; i < (_buffer?.Count ?? 0); i++)
            {
                MoveToCore(Buffer[i], destination);
            }
        }

        Clear();
    }

    /// <inheritdoc />
    public override TagHelperContent Clear()
    {
        _hasContent = false;
        _isModified = true;
        _isSingleContentSet = false;
        _buffer?.Clear();
        return this;
    }

    /// <inheritdoc />
    public override void Reinitialize()
    {
        Clear();
        _isModified = false;
    }

    /// <inheritdoc />
    public override string GetContent() => GetContent(HtmlEncoder.Default);

    /// <inheritdoc />
    public override string GetContent(HtmlEncoder encoder)
    {
        if (!_hasContent)
        {
            return string.Empty;
        }

        using (var writer = new StringWriter())
        {
            WriteTo(writer, encoder);
            return writer.ToString();
        }
    }

    /// <inheritdoc />
    public override void WriteTo(TextWriter writer, HtmlEncoder encoder)
    {
        ArgumentNullException.ThrowIfNull(writer);
        ArgumentNullException.ThrowIfNull(encoder);

        if (!_hasContent)
        {
            return;
        }

        if (_isSingleContentSet)
        {
            WriteToCore(_singleContent, writer, encoder);
            return;
        }

        for (var i = 0; i < (_buffer?.Count ?? 0); i++)
        {
            WriteToCore(Buffer[i], writer, encoder);
        }
    }

    private static void WriteToCore(object entry, TextWriter writer, HtmlEncoder encoder)
    {
        if (entry == null)
        {
            return;
        }

        if (entry is string stringValue)
        {
            encoder.Encode(writer, stringValue);
        }
        else
        {
            ((IHtmlContent)entry).WriteTo(writer, encoder);
        }
    }

    private static void CopyToCore(object entry, IHtmlContentBuilder destination)
    {
        if (entry == null)
        {
            return;
        }

        if (entry is string entryAsString)
        {
            destination.Append(entryAsString);
        }
        else if (entry is IHtmlContentContainer entryAsContainer)
        {
            entryAsContainer.CopyTo(destination);
        }
        else
        {
            destination.AppendHtml((IHtmlContent)entry);
        }
    }

    private static void MoveToCore(object entry, IHtmlContentBuilder destination)
    {
        if (entry == null)
        {
            return;
        }

        if (entry is string entryAsString)
        {
            destination.Append(entryAsString);
        }
        else if (entry is IHtmlContentContainer entryAsContainer)
        {
            entryAsContainer.MoveTo(destination);
        }
        else
        {
            destination.AppendHtml((IHtmlContent)entry);
        }
    }

    private static bool IsEmptyOrWhiteSpaceCore(object entry, EmptyOrWhiteSpaceWriter writer)
    {
        if (entry == null)
        {
            return true;
        }

        if (entry is string stringValue)
        {
            // Do not encode the string because encoded value remains whitespace from user's POV.
            return string.IsNullOrWhiteSpace(stringValue);
        }

        // Use NullHtmlEncoder to avoid treating encoded whitespace as non-whitespace e.g. "\t" as "&#x9;".
        ((IHtmlContent)entry).WriteTo(writer, NullHtmlEncoder.Default);

        return writer.IsEmptyOrWhiteSpace;
    }

    private TagHelperContent AppendCore(object entry)
    {
        if (!_hasContent)
        {
            _isSingleContentSet = true;
            _singleContent = entry;
        }
        else
        {
            Buffer.Add(entry);
        }

        _isModified = true;
        _hasContent = true;

        return this;
    }

    private string DebuggerToString()
    {
        return GetContent();
    }

    // Overrides Write(string) to find if the content written is empty/whitespace.
    private sealed class EmptyOrWhiteSpaceWriter : TextWriter
    {
        public override Encoding Encoding
        {
            get
            {
                return Encoding.UTF8;
            }
        }

        public bool IsEmptyOrWhiteSpace { get; private set; } = true;

        public override void Write(char value)
        {
            if (IsEmptyOrWhiteSpace && !char.IsWhiteSpace(value))
            {
                IsEmptyOrWhiteSpace = false;
            }
        }

        public override void Write(string value)
        {
            if (IsEmptyOrWhiteSpace && !string.IsNullOrWhiteSpace(value))
            {
                IsEmptyOrWhiteSpace = false;
            }
        }
    }
}
