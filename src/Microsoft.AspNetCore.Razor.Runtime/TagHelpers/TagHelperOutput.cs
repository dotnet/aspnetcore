// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Text.Encodings.Web;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Html;

namespace Microsoft.AspNetCore.Razor.TagHelpers
{
    /// <summary>
    /// Class used to represent the output of an <see cref="ITagHelper"/>.
    /// </summary>
    public class TagHelperOutput : IHtmlContentContainer
    {
        private readonly Func<bool, HtmlEncoder, Task<TagHelperContent>> _getChildContentAsync;
        private TagHelperAttributeList _attributes;
        private TagHelperContent _preElement;
        private TagHelperContent _preContent;
        private TagHelperContent _content;
        private TagHelperContent _postContent;
        private TagHelperContent _postElement;
        private bool _wasSuppressOutputCalled;

        // Internal for testing
        internal TagHelperOutput(string tagName)
            : this(
                tagName,
                null,
                (useCachedResult, encoder) => Task.FromResult<TagHelperContent>(new DefaultTagHelperContent()))
        {
        }

        /// <summary>
        /// Instantiates a new instance of <see cref="TagHelperOutput"/>.
        /// </summary>
        /// <param name="tagName">The HTML element's tag name.</param>
        /// <param name="attributes">The HTML attributes.</param>
        /// <param name="getChildContentAsync">
        /// A delegate used to execute children asynchronously with the given <see cref="HtmlEncoder"/> in scope and
        /// return their rendered content.
        /// </param>
        public TagHelperOutput(
            string tagName,
            TagHelperAttributeList attributes,
            Func<bool, HtmlEncoder, Task<TagHelperContent>> getChildContentAsync)
        {
            if (getChildContentAsync == null)
            {
                throw new ArgumentNullException(nameof(getChildContentAsync));
            }

            TagName = tagName;
            _getChildContentAsync = getChildContentAsync;
            _attributes = attributes;
        }

        /// <summary>
        /// The HTML element's tag name.
        /// </summary>
        /// <remarks>
        /// A whitespace or <c>null</c> value results in no start or end tag being rendered.
        /// </remarks>
        public string TagName { get; set; }

        /// <summary>
        /// Content that precedes the HTML element.
        /// </summary>
        /// <remarks>Value is rendered before the HTML element.</remarks>
        public TagHelperContent PreElement
        {
            get
            {
                if (_preElement == null)
                {
                    _preElement = new DefaultTagHelperContent();
                }

                return _preElement;
            }
        }

        /// <summary>
        /// The HTML element's pre content.
        /// </summary>
        /// <remarks>Value is prepended to the <see cref="ITagHelper"/>'s final output.</remarks>
        public TagHelperContent PreContent
        {
            get
            {
                if (_preContent == null)
                {
                    _preContent = new DefaultTagHelperContent();
                }

                return _preContent;
            }
        }

        /// <summary>
        /// Get or set the HTML element's main content.
        /// </summary>
        /// <remarks>Value occurs in the <see cref="ITagHelper"/>'s final output after <see cref="PreContent"/> and
        /// before <see cref="PostContent"/></remarks>
        public TagHelperContent Content
        {
            get
            {
                if (_content == null)
                {
                    _content = new DefaultTagHelperContent();
                }

                return _content;
            }
            set
            {
                if (value == null)
                {
                    throw new ArgumentNullException(nameof(value));
                }

                _content = value;
            }
        }

        /// <summary>
        /// The HTML element's post content.
        /// </summary>
        /// <remarks>Value is appended to the <see cref="ITagHelper"/>'s final output.</remarks>
        public TagHelperContent PostContent
        {
            get
            {
                if (_postContent == null)
                {
                    _postContent = new DefaultTagHelperContent();
                }

                return _postContent;
            }
        }

        /// <summary>
        /// Content that follows the HTML element.
        /// </summary>
        /// <remarks>Value is rendered after the HTML element.</remarks>
        public TagHelperContent PostElement
        {
            get
            {
                if (_postElement == null)
                {
                    _postElement = new DefaultTagHelperContent();
                }

                return _postElement;
            }
        }

        /// <summary>
        /// <c>true</c> if <see cref="Content"/> has been set, <c>false</c> otherwise.
        /// </summary>
        public bool IsContentModified
        {
            get
            {
                return _wasSuppressOutputCalled || _content?.IsModified == true;
            }
        }

        /// <summary>
        /// Syntax of the element in the generated HTML.
        /// </summary>
        public TagMode TagMode { get; set; }

        /// <summary>
        /// The HTML element's attributes.
        /// </summary>
        /// <remarks>
        /// MVC will HTML encode <see cref="string"/> values when generating the start tag. It will not HTML encode
        /// a <c>Microsoft.AspNetCore.Mvc.Rendering.HtmlString</c> instance. MVC converts most other types to a
        /// <see cref="string"/>, then HTML encodes the result.
        /// </remarks>
        public TagHelperAttributeList Attributes
        {
            get
            {
                if (_attributes == null)
                {
                    _attributes = new TagHelperAttributeList();
                }

                return _attributes;
            }
        }

        /// <summary>
        /// Changes <see cref="TagHelperOutput"/> to generate nothing.
        /// </summary>
        /// <remarks>
        /// Sets <see cref="TagName"/> to <c>null</c>, and clears <see cref="PreElement"/>, <see cref="PreContent"/>,
        /// <see cref="Content"/>, <see cref="PostContent"/>, and <see cref="PostElement"/> to suppress output.
        /// </remarks>
        public void SuppressOutput()
        {
            TagName = null;
            _wasSuppressOutputCalled = true;
            _preElement?.Clear();
            _preContent?.Clear();
            _content?.Clear();
            _postContent?.Clear();
            _postElement?.Clear();
        }

        /// <summary>
        /// Executes children asynchronously and returns their rendered content.
        /// </summary>
        /// <returns>A <see cref="Task"/> that on completion returns content rendered by children.</returns>
        /// <remarks>
        /// This method is memoized. Multiple calls will not cause children to re-execute with the page's original
        /// <see cref="HtmlEncoder"/>.
        /// </remarks>
        public Task<TagHelperContent> GetChildContentAsync()
        {
            return GetChildContentAsync(useCachedResult: true, encoder: null);
        }

        /// <summary>
        /// Executes children asynchronously and returns their rendered content.
        /// </summary>
        /// <param name="useCachedResult">
        /// If <c>true</c>, multiple calls will not cause children to re-execute with the page's original
        /// <see cref="HtmlEncoder"/>; returns cached content.
        /// </param>
        /// <returns>A <see cref="Task"/> that on completion returns content rendered by children.</returns>
        public Task<TagHelperContent> GetChildContentAsync(bool useCachedResult)
        {
            return GetChildContentAsync(useCachedResult, encoder: null);
        }

        /// <summary>
        /// Executes children asynchronously with the given <paramref name="encoder"/> in scope and returns their
        /// rendered content.
        /// </summary>
        /// <param name="encoder">
        /// The <see cref="HtmlEncoder"/> to use when the page handles non-<see cref="IHtmlContent"/> C# expressions.
        /// If <c>null</c>, executes children with the page's current <see cref="HtmlEncoder"/>.
        /// </param>
        /// <returns>A <see cref="Task"/> that on completion returns content rendered by children.</returns>
        /// <remarks>
        /// This method is memoized. Multiple calls with the same <see cref="HtmlEncoder"/> instance will not cause
        /// children to re-execute with that encoder in scope.
        /// </remarks>
        public Task<TagHelperContent> GetChildContentAsync(HtmlEncoder encoder)
        {
            return GetChildContentAsync(useCachedResult: true, encoder: encoder);
        }

        /// <summary>
        /// Executes children asynchronously with the given <paramref name="encoder"/> in scope and returns their
        /// rendered content.
        /// </summary>
        /// <param name="useCachedResult">
        /// If <c>true</c>, multiple calls with the same <see cref="HtmlEncoder"/> will not cause children to
        /// re-execute; returns cached content.
        /// </param>
        /// <param name="encoder">
        /// The <see cref="HtmlEncoder"/> to use when the page handles non-<see cref="IHtmlContent"/> C# expressions.
        /// If <c>null</c>, executes children with the page's current <see cref="HtmlEncoder"/>.
        /// </param>
        /// <returns>A <see cref="Task"/> that on completion returns content rendered by children.</returns>
        public Task<TagHelperContent> GetChildContentAsync(bool useCachedResult, HtmlEncoder encoder)
        {
            return _getChildContentAsync(useCachedResult, encoder);
        }

        void IHtmlContentContainer.CopyTo(IHtmlContentBuilder destination)
        {
            if (destination == null)
            {
                throw new ArgumentNullException(nameof(destination));
            }

            _preElement?.CopyTo(destination);

            var isTagNameNullOrWhitespace = string.IsNullOrWhiteSpace(TagName);

            if (!isTagNameNullOrWhitespace)
            {
                destination.AppendHtml("<");
                destination.AppendHtml(TagName);

                CopyAttributesTo(destination);

                if (TagMode == TagMode.SelfClosing)
                {
                    destination.AppendHtml(" /");
                }

                destination.AppendHtml(">");
            }

            if (isTagNameNullOrWhitespace || TagMode == TagMode.StartTagAndEndTag)
            {
                _preContent?.CopyTo(destination);

                _content?.CopyTo(destination);

                _postContent?.CopyTo(destination);
            }

            if (!isTagNameNullOrWhitespace && TagMode == TagMode.StartTagAndEndTag)
            {
                destination.AppendHtml("</");
                destination.AppendHtml(TagName);
                destination.AppendHtml(">");
            }

            _postElement?.CopyTo(destination);
        }

        void IHtmlContentContainer.MoveTo(IHtmlContentBuilder destination)
        {
            if (destination == null)
            {
                throw new ArgumentNullException(nameof(destination));
            }

            _preElement?.MoveTo(destination);

            var isTagNameNullOrWhitespace = string.IsNullOrWhiteSpace(TagName);

            if (!isTagNameNullOrWhitespace)
            {
                destination.AppendHtml("<");
                destination.AppendHtml(TagName);
                
                CopyAttributesTo(destination);

                if (TagMode == TagMode.SelfClosing)
                {
                    destination.AppendHtml(" /");
                }

                destination.AppendHtml(">");
            }

            if (isTagNameNullOrWhitespace || TagMode == TagMode.StartTagAndEndTag)
            {
                _preContent?.MoveTo(destination);
                _content?.MoveTo(destination);
                _postContent?.MoveTo(destination);
            }

            if (!isTagNameNullOrWhitespace && TagMode == TagMode.StartTagAndEndTag)
            {
                destination.AppendHtml("</");
                destination.AppendHtml(TagName);
                destination.AppendHtml(">");
            }

            _postElement?.MoveTo(destination);

            // Depending on the code path we took, these might need to be cleared.
            _preContent?.Clear();
            _content?.Clear();
            _postContent?.Clear();
            _attributes?.Clear();
        }

        public void WriteTo(TextWriter writer, HtmlEncoder encoder)
        {
            if (writer == null)
            {
                throw new ArgumentNullException(nameof(writer));
            }

            if (encoder == null)
            {
                throw new ArgumentNullException(nameof(encoder));
            }

            _preElement?.WriteTo(writer, encoder);

            var isTagNameNullOrWhitespace = string.IsNullOrWhiteSpace(TagName);

            if (!isTagNameNullOrWhitespace)
            {
                writer.Write("<");
                writer.Write(TagName);

                // Perf: Avoid allocating enumerator
                for (var i = 0; i < (_attributes?.Count ?? 0); i++)
                {
                    var attribute = _attributes[i];
                    writer.Write(" ");
                    writer.Write(attribute.Name);

                    if (attribute.Minimized)
                    {
                        continue;
                    }

                    writer.Write("=\"");
                    var value = attribute.Value;
                    var htmlContent = value as IHtmlContent;
                    if (htmlContent != null)
                    {
                        // Perf: static text in a bound attribute go down this path. Avoid allocating if possible (common case).
                        var htmlEncodedString = value as HtmlEncodedString;
                        if (htmlEncodedString != null && !htmlEncodedString.Value.Contains("\""))
                        {
                            writer.Write(htmlEncodedString.Value);
                        }
                        else
                        {
                            // There's no way of tracking the attribute value quotations in the Razor source. Therefore, we
                            // must escape any IHtmlContent double quote values in the case that a user wrote:
                            // <p name='A " is valid in single quotes'></p>
                            using (var stringWriter = new StringWriter())
                            {
                                htmlContent.WriteTo(stringWriter, encoder);
                                stringWriter.GetStringBuilder().Replace("\"", "&quot;");

                                var stringValue = stringWriter.ToString();
                                writer.Write(stringValue);
                            }
                        }
                    }
                    else if (value != null)
                    {
                        encoder.Encode(writer, value.ToString());
                    }

                    writer.Write("\"");
                }

                if (TagMode == TagMode.SelfClosing)
                {
                    writer.Write(" /");
                }

                writer.Write(">");
            }

            if (isTagNameNullOrWhitespace || TagMode == TagMode.StartTagAndEndTag)
            {
                _preContent?.WriteTo(writer, encoder);

                _content?.WriteTo(writer, encoder);

                _postContent?.WriteTo(writer, encoder);
            }

            if (!isTagNameNullOrWhitespace && TagMode == TagMode.StartTagAndEndTag)
            {
                writer.Write("</");
                writer.Write(TagName);
                writer.Write(">");
            }

            _postElement?.WriteTo(writer, encoder);
        }

        private void CopyAttributesTo(IHtmlContentBuilder destination)
        {
            StringWriter stringWriter = null;

            // Perf: Avoid allocating enumerator
            for (var i = 0; i < (_attributes?.Count ?? 0); i++)
            {
                var attribute = _attributes[i];
                destination.AppendHtml(" ");
                destination.AppendHtml(attribute.Name);

                if (attribute.Minimized)
                {
                    continue;
                }

                destination.AppendHtml("=\"");
                var value = attribute.Value;
                var htmlContent = value as IHtmlContent;
                if (htmlContent != null)
                {
                    // Perf: static text in a bound attribute go down this path. Avoid allocating if possible (common case).
                    var htmlEncodedString = value as HtmlEncodedString;
                    if (htmlEncodedString != null && !htmlEncodedString.Value.Contains("\""))
                    {
                        destination.AppendHtml(htmlEncodedString);
                    }
                    else
                    {
                        // Perf: We'll share this writer implementation for all attributes since
                        // they can't nest.
                        stringWriter = stringWriter ?? new StringWriter();

                        destination.AppendHtml(new AttributeContent(htmlContent, stringWriter));
                    }
                }
                else if (value != null)
                {
                    destination.Append(value.ToString());
                }

                destination.AppendHtml("\"");
            }
        }

        private class AttributeContent : IHtmlContent
        {
            private readonly IHtmlContent _inner;
            private readonly StringWriter _stringWriter;

            public AttributeContent(IHtmlContent inner, StringWriter stringWriter)
            {
                _inner = inner;
                _stringWriter = stringWriter;
            }

            public void WriteTo(TextWriter writer, HtmlEncoder encoder)
            {
                // There's no way of tracking the attribute value quotations in the Razor source. Therefore, we
                // must escape any IHtmlContent double quote values in the case that a user wrote:
                // <p name='A " is valid in single quotes'></p>
                _inner.WriteTo(_stringWriter, encoder);
                _stringWriter.GetStringBuilder().Replace("\"", "&quot;");

                var stringValue = _stringWriter.ToString();
                writer.Write(stringValue);

                _stringWriter.GetStringBuilder().Clear();
            }
        }
    }
}
