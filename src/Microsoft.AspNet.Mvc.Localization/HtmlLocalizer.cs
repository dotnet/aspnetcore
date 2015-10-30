// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Text.Encodings.Web;
using Microsoft.Extensions.Localization;

namespace Microsoft.AspNet.Mvc.Localization
{
    /// <summary>
    /// An <see cref="IHtmlLocalizer"/> that uses the <see cref="IStringLocalizer"/> to provide localized HTML content.
    /// This service just encodes the arguments but not the resource string.
    /// </summary>
    public class HtmlLocalizer : IHtmlLocalizer
    {
        private IStringLocalizer _localizer;
        private readonly HtmlEncoder _encoder;

        /// <summary>
        /// Creates a new <see cref="HtmlLocalizer"/>.
        /// </summary>
        /// <param name="localizer">The <see cref="IStringLocalizer"/> to read strings from.</param>
        /// <param name="encoder">The <see cref="HtmlEncoder"/>.</param>
        public HtmlLocalizer(IStringLocalizer localizer, HtmlEncoder encoder)
        {
            if (localizer == null)
            {
                throw new ArgumentNullException(nameof(localizer));
            }

            if (encoder == null)
            {
                throw new ArgumentNullException(nameof(encoder));
            }

            _localizer = localizer;
            _encoder = encoder;
        }

        /// <inheritdoc />
        public virtual LocalizedString this[string key]
        {
            get
            {
                if (key == null)
                {
                    throw new ArgumentNullException(nameof(key));
                }

                return _localizer[key];
            }
        }

        /// <inheritdoc />
        public virtual LocalizedString this[string key, params object[] arguments]
        {
            get
            {
                if (key == null)
                {
                    throw new ArgumentNullException(nameof(key));
                }

                return _localizer[key, arguments];
            }
        }

        /// <summary>
        /// Creates a new <see cref="IHtmlLocalizer"/> for a specific <see cref="CultureInfo"/>.
        /// </summary>
        /// <param name="culture">The <see cref="CultureInfo"/> to use.</param>
        /// <returns>A culture-specific <see cref="IHtmlLocalizer"/>.</returns>
        public virtual IHtmlLocalizer WithCulture(CultureInfo culture)
        {
            if (culture == null)
            {
                throw new ArgumentNullException(nameof(culture));
            }

            return new HtmlLocalizer(_localizer.WithCulture(culture), _encoder);
        }

        /// <summary>
        /// Creates a new <see cref="IStringLocalizer"/> for a specific <see cref="CultureInfo"/>.
        /// </summary>
        /// <param name="culture">The <see cref="CultureInfo"/> to use.</param>
        /// <returns>A culture-specific <see cref="IStringLocalizer"/>.</returns>
        IStringLocalizer IStringLocalizer.WithCulture(CultureInfo culture)
        {
            if (culture == null)
            {
                throw new ArgumentNullException(nameof(culture));
            }

            return new HtmlLocalizer(_localizer.WithCulture(culture), _encoder);
        }

        /// <inheritdoc />
        public virtual LocalizedString GetString(string key)
        {
            if (key == null)
            {
                throw new ArgumentNullException(nameof(key));
            }

            return _localizer.GetString(key);
        }

        /// <inheritdoc />
        public virtual LocalizedString GetString(string key, params object[] arguments)
        {
            if (key == null)
            {
                throw new ArgumentNullException(nameof(key));
            }

            return _localizer.GetString(key, arguments);
        }

        /// <inheritdoc />
        public virtual IEnumerable<LocalizedString> GetAllStrings(bool includeAncestorCultures) =>
            _localizer.GetAllStrings(includeAncestorCultures);

        /// <inheritdoc />
        public virtual LocalizedHtmlString Html(string key)
        {
            if (key == null)
            {
                throw new ArgumentNullException(nameof(key));
            }

            return ToHtmlString(_localizer.GetString(key));
        }

        /// <inheritdoc />
        public virtual LocalizedHtmlString Html(string key, params object[] arguments)
        {
            if (key == null)
            {
                throw new ArgumentNullException(nameof(key));
            }

            var stringValue = _localizer[key].Value;

            return ToHtmlString(new LocalizedString(key, EncodeArguments(stringValue, arguments)));
        }

        /// <summary>
        /// Creates a new <see cref="LocalizedHtmlString"/> for a <see cref="LocalizedString"/>.
        /// </summary>
        /// <param name="result">The <see cref="LocalizedString"/>.</param>
        protected virtual LocalizedHtmlString ToHtmlString(LocalizedString result) =>
            new LocalizedHtmlString(result.Name, result.Value, result.ResourceNotFound);

        /// <summary>
        /// Encodes the arguments based on the object type.
        /// </summary>
        /// <param name="resourceString">The resourceString whose arguments need to be encoded.</param>
        /// <param name="arguments">The array of objects to encode.</param>
        /// <returns>The string with encoded arguments.</returns>
        protected virtual string EncodeArguments(string resourceString, object[] arguments)
        {
            if (resourceString == null)
            {
                throw new ArgumentNullException(nameof(resourceString));
            }

            if (arguments == null)
            {
                throw new ArgumentNullException(nameof(arguments));
            }

            var position = 0;
            var length = resourceString.Length;
            char currentCharacter;
            StringBuilder tokenBuffer = null;
            var outputBuffer = new StringBuilder();
            var isToken = false;

            while (position < length)
            {
                currentCharacter = resourceString[position];

                position++;
                if (currentCharacter == '}')
                {
                    if (position < length && resourceString[position] == '}')  // Treat as escape character for }}
                    {
                        if (isToken)
                        {
                            tokenBuffer.Append("}}");
                        }
                        else
                        {
                            outputBuffer.Append("}");
                        }

                        position++;
                    }
                    else
                    {
                        AppendToBuffer(isToken, '}', tokenBuffer, outputBuffer);

                        if (position == length)
                        {
                            break;
                        }
                        AppendToOutputBuffer(arguments, tokenBuffer, outputBuffer);

                        isToken = false;
                        tokenBuffer = null;
                    }
                }
                else if (currentCharacter == '{')
                {
                    if (position < length && resourceString[position] == '{')  // Treat as escape character for {{
                    {
                        if (isToken)
                        {
                            tokenBuffer.Append("{{");
                        }
                        else
                        {
                            outputBuffer.Append("{");
                        }
                        position++;
                    }
                    else
                    {
                        tokenBuffer = new StringBuilder();
                        tokenBuffer.Append("{");
                        isToken = true;
                    }
                }
                else
                {
                    AppendToBuffer(isToken, currentCharacter, tokenBuffer, outputBuffer);
                }
            }
            AppendToOutputBuffer(arguments, tokenBuffer, outputBuffer);

            return outputBuffer.ToString();
        }

        private void AppendToBuffer(
            bool isToken,
            char value,
            StringBuilder tokenBuffer,
            StringBuilder outputBuffer)
        {
            if (isToken)
            {
                tokenBuffer.Append(value);
            }
            else
            {
                outputBuffer.Append(value);
            }
        }

        private void AppendToOutputBuffer(object[] arguments, StringBuilder tokenBuffer, StringBuilder outputBuffer)
        {
            if (tokenBuffer != null && tokenBuffer.Length > 0)
            {
                outputBuffer.Append(_encoder.Encode(string.Format(tokenBuffer.ToString(), arguments)));
            }
        }
    }
}