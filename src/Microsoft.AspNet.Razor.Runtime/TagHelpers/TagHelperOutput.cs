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
        private string _tagName;

        // Internal for testing
        internal TagHelperOutput(string tagName)
        {
            TagName = tagName;
            Attributes = new Dictionary<string, string>(StringComparer.Ordinal);
        }

        // Internal for testing
        internal TagHelperOutput(string tagName, [NotNull] IDictionary<string, string> attributes)
            : this(tagName, attributes, string.Empty)
        {
        }

        /// <summary>
        /// Instantiates a new instance of <see cref="TagHelperOutput"/>.
        /// </summary>
        /// <param name="tagName">The HTML element's tag name.</param>
        /// <param name="attributes">The HTML attributes.</param>
        /// <param name="content">The HTML element's content.</param>
        public TagHelperOutput(string tagName,
                               [NotNull] IDictionary<string, string> attributes,
                               string content)
        {
            TagName = tagName;
            Content = content;
            Attributes = new Dictionary<string, string>(attributes, StringComparer.Ordinal);
        }

        /// <summary>
        /// The HTML element's tag name.
        /// </summary>
        /// <remarks>
        /// A whitespace value results in no start or end tag being rendered.
        /// </remarks>
        public string TagName
        {
            get
            {
                return _tagName;
            }
            set
            {
                _tagName = value ?? string.Empty;
            }
        }

        /// <summary>
        /// The HTML element's content.
        /// </summary>
        public string Content
        {
            get
            {
                return _content;
            }
            set
            {
                _content = value ?? string.Empty;
            }
        }

        /// <summary>
        /// Indicates whether or not the tag is self closing.
        /// </summary>
        public bool SelfClosing { get; set; }

        /// <summary>
        /// The HTML element's attributes.
        /// </summary>
        public IDictionary<string, string> Attributes { get; private set; }

        /// <summary>
        /// Generates the <see cref="TagHelperOutput"/>'s start tag.
        /// </summary>
        /// <returns><c>string.Empty</c> if <see cref="TagName"/> is <c>string.Empty</c> or whitespace. Otherwise, the
        /// <see cref="string"/> representation of the <see cref="TagHelperOutput"/>'s start tag.</returns>
        public string GenerateStartTag()
        {
            // Only render a start tag if the tag name is not whitespace
            if (string.IsNullOrWhiteSpace(TagName))
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
        /// Generates the <see cref="TagHelperOutput"/>'s body.
        /// </summary>
        /// <returns><c>string.Empty</c> if <see cref="SelfClosing"/> is <c>true</c>. <see cref="Output"/> otherwise.
        /// </returns>
        public string GenerateContent()
        {
            if (SelfClosing)
            {
                return string.Empty;
            }

            return Content;
        }

        /// <summary>
        /// Generates the <see cref="TagHelperOutput"/>'s end tag.
        /// </summary>
        /// <returns><c>string.Empty</c> if <see cref="TagName"/> is <c>string.Empty</c> or whitespace. Otherwise, the
        /// <see cref="string"/> representation of the <see cref="TagHelperOutput"/>'s end tag.</returns>
        public string GenerateEndTag()
        {
            if (SelfClosing || string.IsNullOrWhiteSpace(TagName))
            {
                return string.Empty;
            }

            return string.Format(CultureInfo.InvariantCulture, "</{0}>", TagName);
        }
    }
}