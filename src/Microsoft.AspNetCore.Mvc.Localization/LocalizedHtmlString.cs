// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Text;
using System.Text.Encodings.Web;
using Microsoft.AspNet.Html;

namespace Microsoft.AspNet.Mvc.Localization
{
    /// <summary>
    /// An <see cref="IHtmlContent"/> with localized content.
    /// </summary>
    public class LocalizedHtmlString : IHtmlContent
    {
#if DOTNET5_5
        private static readonly object[] EmptyArguments = Array.Empty<object>();
#else
        private static readonly object[] EmptyArguments = new object[0];
#endif
        private readonly object[] _arguments;

        /// <summary>
        /// Creates an instance of <see cref="LocalizedHtmlString"/>.
        /// </summary>
        /// <param name="name">The name of the string resource.</param>
        /// <param name="value">The string resource.</param>
        public LocalizedHtmlString(string name, string value)
            : this(name, value, isResourceNotFound: false, arguments: EmptyArguments)
        {
        }

        /// <summary>
        /// Creates an instance of <see cref="LocalizedHtmlString"/>.
        /// </summary>
        /// <param name="name">The name of the string resource.</param>
        /// <param name="value">The string resource.</param>
        /// <param name="isResourceNotFound">A flag that indicates if the resource is not found.</param>
        public LocalizedHtmlString(string name, string value, bool isResourceNotFound)
            : this(name, value, isResourceNotFound, arguments: EmptyArguments)
        {
        }

        /// <summary>
        /// Creates an instance of <see cref="LocalizedHtmlString"/>.
        /// </summary>
        /// <param name="name">The name of the string resource.</param>
        /// <param name="value">The string resource.</param>
        /// <param name="isResourceNotFound">A flag that indicates if the resource is not found.</param>
        /// <param name="arguments">The values to format the <paramref name="value"/> with.</param>
        public LocalizedHtmlString(string name, string value, bool isResourceNotFound, params object[] arguments)
        {
            if (name == null)
            {
                throw new ArgumentNullException(nameof(name));
            }

            if (value == null)
            {
                throw new ArgumentNullException(nameof(value));
            }

            if (arguments == null)
            {
                throw new ArgumentNullException(nameof(arguments));
            }

            Name = name;
            Value = value;
            IsResourceNotFound = isResourceNotFound;
            _arguments = arguments;
        }

        /// <summary>
        /// The name of the string resource.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// The string resource.
        /// </summary>
        public string Value { get; }

        /// <summary>
        /// Gets a flag that indicates if the resource is not found.
        /// </summary>
        public bool IsResourceNotFound { get; }

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

            var htmlTextWriter = writer as HtmlTextWriter;
            if (htmlTextWriter == null)
            {
                FormatValue(writer, encoder, Value, _arguments);
            }
            else
            {
                htmlTextWriter.Write(this);
            }
        }

        private static void FormatValue(
            TextWriter writer,
            HtmlEncoder encoder,
            string resourceString,
            object[] arguments)
        {
            var position = 0;
            var length = resourceString.Length;
            StringBuilder tokenBuffer = null;
            var isToken = false;

            while (position < length)
            {
                var currentCharacter = resourceString[position];
                position++;

                if (currentCharacter == '}')
                {
                    if (position < length && resourceString[position] == '}')
                    {
                        // Escaped curly brace: "}}".
                        AppendCurlyBrace(isToken, currentCharacter, tokenBuffer, writer);
                        position++;
                    }
                    else
                    {
                        // End of a token.
                        Append(isToken, '}', tokenBuffer, writer);
                        if (position == length)
                        {
                            break;
                        }

                        AppendToOutput(tokenBuffer, arguments, writer, encoder);

                        isToken = false;
                        tokenBuffer = null;
                    }
                }
                else if (currentCharacter == '{')
                {
                    if (position < length && resourceString[position] == '{')
                    {
                        // Escaped curly brace: "{{".
                        AppendCurlyBrace(isToken, currentCharacter, tokenBuffer, writer);
                        position++;
                    }
                    else
                    {
                        // Start of a new token.
                        tokenBuffer = new StringBuilder();
                        tokenBuffer.Append('{');
                        isToken = true;
                    }
                }
                else
                {
                    Append(isToken, currentCharacter, tokenBuffer, writer);
                }
            }

            AppendToOutput(tokenBuffer, arguments, writer, encoder);
        }

        private static void Append(
            bool isToken,
            char value,
            StringBuilder tokenBuffer,
            TextWriter writer)
        {
            if (isToken)
            {
                tokenBuffer.Append(value);
            }
            else
            {
                writer.Write(value);
            }
        }

        private static void AppendCurlyBrace(
            bool isToken,
            char curlyBrace,
            StringBuilder tokenBuffer,
            TextWriter writer)
        {
            if (isToken)
            {
                tokenBuffer
                    .Append(curlyBrace)
                    .Append(curlyBrace);
            }
            else
            {
                writer.Write(curlyBrace);
            }
        }

        private static void AppendToOutput(
            StringBuilder tokenBuffer,
            object[] arguments,
            TextWriter writer,
            HtmlEncoder encoder)
        {
            if (tokenBuffer != null && tokenBuffer.Length > 0)
            {
                encoder.Encode(writer, string.Format(tokenBuffer.ToString(), arguments));
            }
        }
    }
}