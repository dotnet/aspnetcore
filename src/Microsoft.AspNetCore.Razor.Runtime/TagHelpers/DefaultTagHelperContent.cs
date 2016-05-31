// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Html;

namespace Microsoft.AspNetCore.Razor.TagHelpers
{
    /// <summary>
    /// Default concrete <see cref="TagHelperContent"/>.
    /// </summary>
    [DebuggerDisplay("{DebuggerToString(),nq}")]
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
            if (destination == null)
            {
                throw new ArgumentNullException(nameof(destination));
            }

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
            if (destination == null)
            {
                throw new ArgumentNullException(nameof(destination));
            }

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
            if (writer == null)
            {
                throw new ArgumentNullException(nameof(writer));
            }

            if (encoder == null)
            {
                throw new ArgumentNullException(nameof(encoder));
            }

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

        private void WriteToCore(object entry, TextWriter writer, HtmlEncoder encoder)
        {
            if (entry == null)
            {
                return;
            }

            var stringValue = entry as string;
            if (stringValue != null)
            {
                encoder.Encode(writer, stringValue);
            }
            else
            {
                ((IHtmlContent)entry).WriteTo(writer, encoder);
            }
        }

        private void CopyToCore(object entry, IHtmlContentBuilder destination)
        {
            if (entry == null)
            {
                return;
            }

            string entryAsString;
            IHtmlContentContainer entryAsContainer;
            if ((entryAsString = entry as string) != null)
            {
                destination.Append(entryAsString);
            }
            else if ((entryAsContainer = entry as IHtmlContentContainer) != null)
            {
                entryAsContainer.CopyTo(destination);
            }
            else
            {
                destination.AppendHtml((IHtmlContent)entry);
            }
        }

        private void MoveToCore(object entry, IHtmlContentBuilder destination)
        {
            if (entry == null)
            {
                return;
            }

            string entryAsString;
            IHtmlContentContainer entryAsContainer;
            if ((entryAsString = entry as string) != null)
            {
                destination.Append(entryAsString);
            }
            else if ((entryAsContainer = entry as IHtmlContentContainer) != null)
            {
                entryAsContainer.MoveTo(destination);
            }
            else
            {
                destination.AppendHtml((IHtmlContent)entry);
            }
        }

        private bool IsEmptyOrWhiteSpaceCore(object entry, EmptyOrWhiteSpaceWriter writer)
        {
            if (entry == null)
            {
                return false;
            }

            var stringValue = entry as string;
            if (stringValue != null)
            {
                // Do not encode the string because encoded value remains whitespace from user's POV.
                if (!string.IsNullOrWhiteSpace(stringValue))
                {
                    return false;
                }
            }
            else
            {
                // Use NullHtmlEncoder to avoid treating encoded whitespace as non-whitespace e.g. "\t" as "&#x9;".
                ((IHtmlContent)entry).WriteTo(writer, NullHtmlEncoder.Default);
                if (!writer.IsEmptyOrWhiteSpace)
                {
                    return false;
                }
            }

            return true;
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
        private class EmptyOrWhiteSpaceWriter : TextWriter
        {
            public override Encoding Encoding
            {
                get
                {
                    throw new NotImplementedException();
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
}