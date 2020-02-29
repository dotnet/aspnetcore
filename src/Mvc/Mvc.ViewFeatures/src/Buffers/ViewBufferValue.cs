// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Diagnostics;
using System.IO;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Html;

namespace Microsoft.AspNetCore.Mvc.ViewFeatures.Buffers
{
    /// <summary>
    /// Encapsulates a string or <see cref="IHtmlContent"/> value.
    /// </summary>
    [DebuggerDisplay("{DebuggerToString()}")]
    public readonly struct ViewBufferValue
    {
        /// <summary>
        /// Initializes a new instance of <see cref="ViewBufferValue"/> with a <c>string</c> value.
        /// </summary>
        /// <param name="value">The value.</param>
        public ViewBufferValue(string value)
        {
            Value = value;
        }

        /// <summary>
        /// Initializes a new instance of <see cref="ViewBufferValue"/> with a <see cref="IHtmlContent"/> value.
        /// </summary>
        /// <param name="content">The <see cref="IHtmlContent"/>.</param>
        public ViewBufferValue(IHtmlContent content)
        {
            Value = content;
        }

        /// <summary>
        /// Gets the value.
        /// </summary>
        public object Value { get; }

        private string DebuggerToString()
        {
            using (var writer = new StringWriter())
            {
                if (Value is string valueAsString)
                {
                    writer.Write(valueAsString);
                    return writer.ToString();
                }

                if (Value is IHtmlContent valueAsContent)
                {
                    valueAsContent.WriteTo(writer, HtmlEncoder.Default);
                    return writer.ToString();
                }

                return "(null)";
            }
        }
    }
}
