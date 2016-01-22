// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Text.Encodings.Web;
using System.Threading.Tasks;
using Microsoft.AspNet.Html;

namespace Microsoft.AspNet.Razor.TagHelpers
{
    /// <summary>
    /// Class used to represent the output of an <see cref="ITagHelper"/>.
    /// </summary>
    public class TagHelperOutput : IHtmlContent
    {
        private readonly Func<bool, HtmlEncoder, Task<TagHelperContent>> _getChildContentAsync;
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
                new TagHelperAttributeList(),
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
            if (attributes == null)
            {
                throw new ArgumentNullException(nameof(attributes));
            }

            if (getChildContentAsync == null)
            {
                throw new ArgumentNullException(nameof(getChildContentAsync));
            }

            TagName = tagName;
            Attributes = attributes;
            _getChildContentAsync = getChildContentAsync;
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
        /// a <c>Microsoft.AspNet.Mvc.Rendering.HtmlString</c> instance. MVC converts most other types to a
        /// <see cref="string"/>, then HTML encodes the result.
        /// </remarks>
        public TagHelperAttributeList Attributes { get; }

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

        /// <inheritdoc />
        public void WriteTo(TextWriter writer, HtmlEncoder encoder)
        {
            if (writer == null)
            {
                throw new ArgumentNullException(nameof(writer));
            }

            _preElement?.WriteTo(writer, encoder);

            var isTagNameNullOrWhitespace = string.IsNullOrWhiteSpace(TagName);

            if (!isTagNameNullOrWhitespace)
            {
                writer.Write('<');
                writer.Write(TagName);

                // Perf: Avoid allocating enumerator
                for (var i = 0; i < Attributes.Count; i++)
                {
                    var attribute = Attributes[i];
                    writer.Write(' ');
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
                        // There's no way of tracking the attribute value quotations in the Razor source. Therefore, we
                        // must escape any IHtmlContent double quote values in the case that a user wrote:
                        // <p name='A " is valid in single quotes'></p>
                        using (var stringWriter = new StringWriter())
                        {
                            htmlContent.WriteTo(stringWriter, encoder);

                            var stringValue = stringWriter.ToString();
                            stringValue = stringValue.Replace("\"", "&quot;");

                            writer.Write(stringValue);
                        }
                    }
                    else if (value != null)
                    {
                        encoder.Encode(writer, value.ToString());
                    }

                    writer.Write('"');
                }

                if (TagMode == TagMode.SelfClosing)
                {
                    writer.Write(" /");
                }

                writer.Write('>');
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
    }
}
