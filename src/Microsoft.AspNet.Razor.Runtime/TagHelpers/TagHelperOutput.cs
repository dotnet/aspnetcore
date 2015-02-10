// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Net;
using System.Text;

namespace Microsoft.AspNet.Razor.Runtime.TagHelpers
{
    /// <summary>
    /// Class used to represent the output of an <see cref="ITagHelper"/>.
    /// </summary>
    public class TagHelperOutput
    {
        private string _content;
        private bool _contentSet;
        private bool _isTagNameNullOrWhitespace;
        private string _tagName;

        // Internal for testing
        internal TagHelperOutput(string tagName)
        {
            TagName = tagName;
            Attributes = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Instantiates a new instance of <see cref="TagHelperOutput"/>.
        /// </summary>
        /// <param name="tagName">The HTML element's tag name.</param>
        /// <param name="attributes">The HTML attributes.</param>
        public TagHelperOutput(string tagName, [NotNull] IDictionary<string, string> attributes)
        {
            TagName = tagName;
            Attributes = new Dictionary<string, string>(attributes, StringComparer.OrdinalIgnoreCase);
            PreContent = string.Empty;
            _content = string.Empty;
            PostContent = string.Empty;
        }

        /// <summary>
        /// The HTML element's tag name.
        /// </summary>
        /// <remarks>
        /// A whitespace or <c>null</c> value results in no start or end tag being rendered.
        /// </remarks>
        public string TagName
        {
            get
            {
                return _tagName;
            }
            set
            {
                _tagName = value;
                _isTagNameNullOrWhitespace = string.IsNullOrWhiteSpace(_tagName);
            }
        }

        /// <summary>
        /// The HTML element's pre content.
        /// </summary>
        /// <remarks>Value is prepended to the <see cref="ITagHelper"/>'s final output.</remarks>
        public string PreContent { get; set; }

        /// <summary>
        /// The HTML element's main content.
        /// </summary>
        /// <remarks>Value occurs in the <see cref="ITagHelper"/>'s final output after <see cref="PreContent"/> and 
        /// before <see cref="PostContent"/></remarks>
        public string Content
        {
            get
            {
                return _content;
            }
            set
            {
                _contentSet = true;
                _content = value;
            }
        }

        /// <summary>
        /// The HTML element's post content.
        /// </summary>
        /// <remarks>Value is appended to the <see cref="ITagHelper"/>'s final output.</remarks>
        public string PostContent { get; set; }

        /// <summary>
        /// <c>true</c> if <see cref="Content"/> has been set, <c>false</c> otherwise.
        /// </summary>
        public bool ContentSet
        {
            get
            {
                return _contentSet;
            }
        }

        /// <summary>
        /// Indicates whether or not the tag is self-closing.
        /// </summary>
        public bool SelfClosing { get; set; }

        /// <summary>
        /// The HTML element's attributes.
        /// </summary>
        public IDictionary<string, string> Attributes { get; }

        /// <summary>
        /// Generates the <see cref="TagHelperOutput"/>'s start tag.
        /// </summary>
        /// <returns><c>string.Empty</c> if <see cref="TagName"/> is <c>null</c> or whitespace. Otherwise, the
        /// <see cref="string"/> representation of the <see cref="TagHelperOutput"/>'s start tag.</returns>
        public string GenerateStartTag()
        {
            // Only render a start tag if the tag name is not whitespace
            if (_isTagNameNullOrWhitespace)
            {
                return string.Empty;
            }

            var sb = new StringBuilder();

            sb.Append('<')
              .Append(TagName);

            foreach (var attribute in Attributes)
            {
                var value = WebUtility.HtmlEncode(attribute.Value);
                sb.Append(' ')
                  .Append(attribute.Key)
                  .Append("=\"")
                  .Append(value)
                  .Append('"');
            }

            if (SelfClosing)
            {
                sb.Append(" /");
            }

            sb.Append('>');

            return sb.ToString();
        }

        /// <summary>
        /// Generates the <see cref="TagHelperOutput"/>'s <see cref="PreContent"/>.
        /// </summary>
        /// <returns><c>string.Empty</c> if <see cref="TagName"/> is not <c>null</c> or whitespace
        /// and <see cref="SelfClosing"/> is <c>true</c>. Otherwise, <see cref="PreContent"/>.</returns>
        public string GeneratePreContent()
        {
            if (!_isTagNameNullOrWhitespace && SelfClosing)
            {
                return string.Empty;
            }

            return PreContent;
        }

        /// <summary>
        /// Generates the <see cref="TagHelperOutput"/>'s body.
        /// </summary>
        /// <returns><c>string.Empty</c> if <see cref="TagName"/> is not <c>null</c> or whitespace
        /// and <see cref="SelfClosing"/> is <c>true</c>. Otherwise, <see cref="Content"/>.</returns>
        public string GenerateContent()
        {
            if (!_isTagNameNullOrWhitespace && SelfClosing)
            {
                return string.Empty;
            }

            return Content;
        }

        /// <summary>
        /// Generates the <see cref="TagHelperOutput"/>'s <see cref="PostContent"/>.
        /// </summary>
        /// <returns><c>string.Empty</c> if <see cref="TagName"/> is not <c>null</c> or whitespace
        /// and <see cref="SelfClosing"/> is <c>true</c>. Otherwise, <see cref="PostContent"/>.</returns>
        public string GeneratePostContent()
        {
            if (!_isTagNameNullOrWhitespace && SelfClosing)
            {
                return string.Empty;
            }

            return PostContent;
        }

        /// <summary>
        /// Generates the <see cref="TagHelperOutput"/>'s end tag.
        /// </summary>
        /// <returns><c>string.Empty</c> if <see cref="TagName"/> is <c>null</c> or whitespace. Otherwise, the
        /// <see cref="string"/> representation of the <see cref="TagHelperOutput"/>'s end tag.</returns>
        public string GenerateEndTag()
        {
            if (SelfClosing || _isTagNameNullOrWhitespace)
            {
                return string.Empty;
            }

            return string.Format(CultureInfo.InvariantCulture, "</{0}>", TagName);
        }

        /// <summary>
        /// Changes <see cref="TagHelperOutput"/> to generate nothing.
        /// </summary>
        /// <remarks>
        /// Sets <see cref="TagName"/>, <see cref="PreContent"/>, <see cref="Content"/>, and <see cref="PostContent"/> 
        /// to <c>null</c> to suppress output.
        /// </remarks>
        public void SuppressOutput()
        {
            TagName = null;
            PreContent = null;
            Content = null;
            PostContent = null;
        }
    }
}