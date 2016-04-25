// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Html;
using Microsoft.Extensions.Internal;

namespace Microsoft.AspNetCore.Razor.TagHelpers
{
    /// <summary>
    /// An HTML tag helper attribute.
    /// </summary>
    public class TagHelperAttribute : IHtmlContentContainer
    {
        /// <summary>
        /// Instantiates a new instance of <see cref="TagHelperAttribute"/> with the specified <paramref name="name"/>.
        /// <see cref="ValueStyle"/> is set to <see cref="HtmlAttributeValueStyle.Minimized"/> and <see cref="Value"/> to
        /// <c>null</c>.
        /// </summary>
        /// <param name="name">The <see cref="Name"/> of the attribute.</param>
        public TagHelperAttribute(string name)
            : this(name, value: null, valueStyle: HtmlAttributeValueStyle.Minimized)
        {
        }

        /// <summary>
        /// Instantiates a new instance of <see cref="TagHelperAttribute"/> with the specified <paramref name="name"/>
        /// and <paramref name="value"/>. <see cref="ValueStyle"/> is set to <see cref="HtmlAttributeValueStyle.DoubleQuotes"/>.
        /// </summary>
        /// <param name="name">The <see cref="Name"/> of the attribute.</param>
        /// <param name="value">The <see cref="Value"/> of the attribute.</param>
        public TagHelperAttribute(string name, object value)
            : this(name, value, valueStyle: HtmlAttributeValueStyle.DoubleQuotes)
        {
        }

        /// <summary>
        /// Instantiates a new instance of <see cref="TagHelperAttribute"/> with the specified <paramref name="name"/>,
        /// <paramref name="value"/> and <paramref name="valueStyle"/>.
        /// </summary>
        /// <param name="name">The <see cref="Name"/> of the new instance.</param>
        /// <param name="value">The <see cref="Value"/> of the new instance.</param>
        /// <param name="valueStyle">The <see cref="ValueStyle"/> of the new instance.</param>
        /// <remarks>If <paramref name="valueStyle"/> is <see cref="HtmlAttributeValueStyle.Minimized"/>,
        /// <paramref name="value"/> is ignored when this instance is rendered.</remarks>
        public TagHelperAttribute(string name, object value, HtmlAttributeValueStyle valueStyle)
        {
            if (name == null)
            {
                throw new ArgumentNullException(nameof(name));
            }

            Name = name;
            Value = value;
            ValueStyle = valueStyle;
        }

        /// <summary>
        /// Gets the name of the attribute.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Gets the value of the attribute.
        /// </summary>
        public object Value { get; }

        /// <summary>
        /// Gets the value style of the attribute.
        /// </summary>
        public HtmlAttributeValueStyle ValueStyle { get; }

        /// <inheritdoc />
        /// <remarks><see cref="Name"/> is compared case-insensitively.</remarks>
        public bool Equals(TagHelperAttribute other)
        {
            return
                other != null &&
                string.Equals(Name, other.Name, StringComparison.OrdinalIgnoreCase) &&
                ValueStyle == other.ValueStyle &&
                (ValueStyle == HtmlAttributeValueStyle.Minimized || Equals(Value, other.Value));
        }

        /// <inheritdoc />
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

            writer.Write(Name);

            if (ValueStyle == HtmlAttributeValueStyle.Minimized)
            {
                return;
            }

            var valuePrefix = GetAttributeValuePrefix(ValueStyle);
            if (valuePrefix != null)
            {
                writer.Write(valuePrefix);
            }

            var htmlContent = Value as IHtmlContent;
            if (htmlContent != null)
            {
                htmlContent.WriteTo(writer, encoder);
            }
            else if (Value != null)
            {
                encoder.Encode(writer, Value.ToString());
            }

            var valueSuffix = GetAttributeValueSuffix(ValueStyle);
            if (valueSuffix != null)
            {
                writer.Write(valueSuffix);
            }
        }

        /// <inheritdoc />
        public void CopyTo(IHtmlContentBuilder destination)
        {
            if (destination == null)
            {
                throw new ArgumentNullException(nameof(destination));
            }

            destination.AppendHtml(Name);

            if (ValueStyle == HtmlAttributeValueStyle.Minimized)
            {
                return;
            }

            var valuePrefix = GetAttributeValuePrefix(ValueStyle);
            if (valuePrefix != null)
            {
                destination.AppendHtml(valuePrefix);
            }

            string valueAsString;
            IHtmlContentContainer valueAsHtmlContainer;
            IHtmlContent valueAsHtmlContent;
            if ((valueAsString = Value as string) != null)
            {
                destination.Append(valueAsString);
            }
            else if ((valueAsHtmlContainer = Value as IHtmlContentContainer) != null)
            {
                valueAsHtmlContainer.CopyTo(destination);
            }
            else if ((valueAsHtmlContent = Value as IHtmlContent) != null)
            {
                destination.AppendHtml(valueAsHtmlContent);
            }
            else if (Value != null)
            {
                destination.Append(Value.ToString());
            }

            var valueSuffix = GetAttributeValueSuffix(ValueStyle);
            if (valueSuffix != null)
            {
                destination.AppendHtml(valueSuffix);
            }
        }

        /// <inheritdoc />
        public void MoveTo(IHtmlContentBuilder destination)
        {
            if (destination == null)
            {
                throw new ArgumentNullException(nameof(destination));
            }

            destination.AppendHtml(Name);

            if (ValueStyle == HtmlAttributeValueStyle.Minimized)
            {
                return;
            }

            var valuePrefix = GetAttributeValuePrefix(ValueStyle);
            if (valuePrefix != null)
            {
                destination.AppendHtml(valuePrefix);
            }

            string valueAsString;
            IHtmlContentContainer valueAsHtmlContainer;
            IHtmlContent valueAsHtmlContent;
            if ((valueAsString = Value as string) != null)
            {
                destination.Append(valueAsString);
            }
            else if ((valueAsHtmlContainer = Value as IHtmlContentContainer) != null)
            {
                valueAsHtmlContainer.MoveTo(destination);
            }
            else if ((valueAsHtmlContent = Value as IHtmlContent) != null)
            {
                destination.AppendHtml(valueAsHtmlContent);
            }
            else if (Value != null)
            {
                destination.Append(Value.ToString());
            }

            var valueSuffix = GetAttributeValueSuffix(ValueStyle);
            if (valueSuffix != null)
            {
                destination.AppendHtml(valueSuffix);
            }
        }

        /// <inheritdoc />
        public override bool Equals(object obj)
        {
            var other = obj as TagHelperAttribute;

            return Equals(other);
        }

        /// <inheritdoc />
        public override int GetHashCode()
        {
            var hashCodeCombiner = HashCodeCombiner.Start();
            hashCodeCombiner.Add(Name, StringComparer.Ordinal);

            if (ValueStyle != HtmlAttributeValueStyle.Minimized)
            {
                hashCodeCombiner.Add(Value);
            }

            hashCodeCombiner.Add(ValueStyle);

            return hashCodeCombiner.CombinedHash;
        }

        private static string GetAttributeValuePrefix(HtmlAttributeValueStyle valueStyle)
        {
            switch (valueStyle)
            {
                case HtmlAttributeValueStyle.DoubleQuotes:
                    return "=\"";
                case HtmlAttributeValueStyle.SingleQuotes:
                    return "='";
                case HtmlAttributeValueStyle.NoQuotes:
                    return "=";
            }

            return null;
        }

        private static string GetAttributeValueSuffix(HtmlAttributeValueStyle valueStyle)
        {
            switch (valueStyle)
            {
                case HtmlAttributeValueStyle.DoubleQuotes:
                    return "\"";
                case HtmlAttributeValueStyle.SingleQuotes:
                    return "'";
            }

            return null;
        }
    }
}