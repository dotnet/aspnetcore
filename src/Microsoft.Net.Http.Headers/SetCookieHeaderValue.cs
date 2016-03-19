// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Text;

namespace Microsoft.Net.Http.Headers
{
    // http://tools.ietf.org/html/rfc6265
    public class SetCookieHeaderValue
    {
        private const string ExpiresToken = "expires";
        private const string MaxAgeToken = "max-age";
        private const string DomainToken = "domain";
        private const string PathToken = "path";
        private const string SecureToken = "secure";
        private const string HttpOnlyToken = "httponly";
        private const string DefaultPath = "/"; // TODO: Used?

        private static readonly HttpHeaderParser<SetCookieHeaderValue> SingleValueParser
            = new GenericHeaderParser<SetCookieHeaderValue>(false, GetSetCookieLength);
        private static readonly HttpHeaderParser<SetCookieHeaderValue> MultipleValueParser
            = new GenericHeaderParser<SetCookieHeaderValue>(true, GetSetCookieLength);

        private string _name;
        private string _value;

        private SetCookieHeaderValue()
        {
            // Used by the parser to create a new instance of this type.
        }

        public SetCookieHeaderValue(string name)
            : this(name, string.Empty)
        {
        }

        public SetCookieHeaderValue(string name, string value)
        {
            if (name == null)
            {
                throw new ArgumentNullException(nameof(name));
            }

            if (value == null)
            {
                throw new ArgumentNullException(nameof(value));
            }

            Name = name;
            Value = value;
        }

        public string Name
        {
            get { return _name; }
            set
            {
                CookieHeaderValue.CheckNameFormat(value, nameof(value));
                _name = value;
            }
        }

        public string Value
        {
            get { return _value; }
            set
            {
                CookieHeaderValue.CheckValueFormat(value, nameof(value));
                _value = value;
            }
        }

        public DateTimeOffset? Expires { get; set; }

        public TimeSpan? MaxAge { get; set; }

        public string Domain { get; set; }

        // TODO: PathString?
        public string Path { get; set; }

        public bool Secure { get; set; }

        public bool HttpOnly { get; set; }

        // name="val ue"; expires=Sun, 06 Nov 1994 08:49:37 GMT; max-age=86400; domain=domain1; path=path1; secure; httponly
        public override string ToString()
        {
            StringBuilder header = new StringBuilder();
            AppendToStringBuilder(header);

            return header.ToString();
        }

        /// <summary>
        /// Append string representation of this <see cref="SetCookieHeaderValue"/> to given
        /// <paramref name="builder"/>.
        /// </summary>
        /// <param name="builder">
        /// The <see cref="StringBuilder"/> to receive the string representation of this
        /// <see cref="SetCookieHeaderValue"/>.
        /// </param>
        public void AppendToStringBuilder(StringBuilder builder)
        {
            builder.Append(_name);
            builder.Append("=");
            builder.Append(_value);

            if (Expires.HasValue)
            {
                AppendSegment(builder, ExpiresToken, HeaderUtilities.FormatDate(Expires.Value));
            }

            if (MaxAge.HasValue)
            {
                AppendSegment(builder, MaxAgeToken, HeaderUtilities.FormatInt64((long)MaxAge.Value.TotalSeconds));
            }

            if (Domain != null)
            {
                AppendSegment(builder, DomainToken, Domain);
            }

            if (Path != null)
            {
                AppendSegment(builder, PathToken, Path);
            }

            if (Secure)
            {
                AppendSegment(builder, SecureToken, null);
            }

            if (HttpOnly)
            {
                AppendSegment(builder, HttpOnlyToken, null);
            }
        }

        private static void AppendSegment(StringBuilder builder, string name, string value)
        {
            builder.Append("; ");
            builder.Append(name);
            if (value != null)
            {
                builder.Append("=");
                builder.Append(value);
            }
        }

        public static SetCookieHeaderValue Parse(string input)
        {
            var index = 0;
            return SingleValueParser.ParseValue(input, ref index);
        }

        public static bool TryParse(string input, out SetCookieHeaderValue parsedValue)
        {
            var index = 0;
            return SingleValueParser.TryParseValue(input, ref index, out parsedValue);
        }

        public static IList<SetCookieHeaderValue> ParseList(IList<string> inputs)
        {
            return MultipleValueParser.ParseValues(inputs);
        }

        public static IList<SetCookieHeaderValue> ParseStrictList(IList<string> inputs)
        {
            return MultipleValueParser.ParseStrictValues(inputs);
        }

        public static bool TryParseList(IList<string> inputs, out IList<SetCookieHeaderValue> parsedValues)
        {
            return MultipleValueParser.TryParseValues(inputs, out parsedValues);
        }

        public static bool TryParseStrictList(IList<string> inputs, out IList<SetCookieHeaderValue> parsedValues)
        {
            return MultipleValueParser.TryParseStrictValues(inputs, out parsedValues);
        }

        // name=value; expires=Sun, 06 Nov 1994 08:49:37 GMT; max-age=86400; domain=domain1; path=path1; secure; httponly
        private static int GetSetCookieLength(string input, int startIndex, out SetCookieHeaderValue parsedValue)
        {
            Contract.Requires(startIndex >= 0);
            var offset = startIndex;

            parsedValue = null;

            if (string.IsNullOrEmpty(input) || (offset >= input.Length))
            {
                return 0;
            }

            var result = new SetCookieHeaderValue();

            // The caller should have already consumed any leading whitespace, commas, etc..

            // Name=value;

            // Name
            var itemLength = HttpRuleParser.GetTokenLength(input, offset);
            if (itemLength == 0)
            {
                return 0;
            }
            result._name = input.Substring(offset, itemLength);
            offset += itemLength;

            // = (no spaces)
            if (!ReadEqualsSign(input, ref offset))
            {
                return 0;
            }

            // value or "quoted value"
            // The value may be empty
            result._value = CookieHeaderValue.GetCookieValue(input, ref offset);

            // *(';' SP cookie-av)
            while (offset < input.Length)
            {
                if (input[offset] == ',')
                {
                    // Divider between headers
                    break;
                }
                if (input[offset] != ';')
                {
                    // Expecting a ';' between parameters
                    return 0;
                }
                offset++;

                offset += HttpRuleParser.GetWhitespaceLength(input, offset);

                //  cookie-av = expires-av / max-age-av / domain-av / path-av / secure-av / httponly-av / extension-av
                itemLength = HttpRuleParser.GetTokenLength(input, offset);
                if (itemLength == 0)
                {
                    // Trailing ';' or leading into garbage. Let the next parser fail.
                    break;
                }
                var token = input.Substring(offset, itemLength);
                offset += itemLength;

                //  expires-av = "Expires=" sane-cookie-date
                if (string.Equals(token, ExpiresToken, StringComparison.OrdinalIgnoreCase))
                {
                    // = (no spaces)
                    if (!ReadEqualsSign(input, ref offset))
                    {
                        return 0;
                    }
                    var dateString = ReadToSemicolonOrEnd(input, ref offset);
                    DateTimeOffset expirationDate;
                    if (!HttpRuleParser.TryStringToDate(dateString, out expirationDate))
                    {
                        // Invalid expiration date, abort
                        return 0;
                    }
                    result.Expires = expirationDate;
                }
                // max-age-av = "Max-Age=" non-zero-digit *DIGIT
                else if (string.Equals(token, MaxAgeToken, StringComparison.OrdinalIgnoreCase))
                {
                    // = (no spaces)
                    if (!ReadEqualsSign(input, ref offset))
                    {
                        return 0;
                    }

                    itemLength = HttpRuleParser.GetNumberLength(input, offset, allowDecimal: false);
                    if (itemLength == 0)
                    {
                        return 0;
                    }
                    var numberString = input.Substring(offset, itemLength);
                    long maxAge;
                    if (!HeaderUtilities.TryParseInt64(numberString, out maxAge))
                    {
                        // Invalid expiration date, abort
                        return 0;
                    }
                    result.MaxAge = TimeSpan.FromSeconds(maxAge);
                    offset += itemLength;
                }
                // domain-av = "Domain=" domain-value
                // domain-value = <subdomain> ; defined in [RFC1034], Section 3.5, as enhanced by [RFC1123], Section 2.1
                else if (string.Equals(token, DomainToken, StringComparison.OrdinalIgnoreCase))
                {
                    // = (no spaces)
                    if (!ReadEqualsSign(input, ref offset))
                    {
                        return 0;
                    }
                    // We don't do any detailed validation on the domain.
                    result.Domain = ReadToSemicolonOrEnd(input, ref offset);
                }
                // path-av = "Path=" path-value
                // path-value = <any CHAR except CTLs or ";">
                else if (string.Equals(token, PathToken, StringComparison.OrdinalIgnoreCase))
                {
                    // = (no spaces)
                    if (!ReadEqualsSign(input, ref offset))
                    {
                        return 0;
                    }
                    // We don't do any detailed validation on the path.
                    result.Path = ReadToSemicolonOrEnd(input, ref offset);
                }
                // secure-av = "Secure"
                else if (string.Equals(token, SecureToken, StringComparison.OrdinalIgnoreCase))
                {
                    result.Secure = true;
                }
                // httponly-av = "HttpOnly"
                else if (string.Equals(token, HttpOnlyToken, StringComparison.OrdinalIgnoreCase))
                {
                    result.HttpOnly = true;
                }
                // extension-av = <any CHAR except CTLs or ";">
                else
                {
                    // TODO: skip it? Store it in a list?
                }
            }

            parsedValue = result;
            return offset - startIndex;
        }

        private static bool ReadEqualsSign(string input, ref int offset)
        {
            // = (no spaces)
            if (offset >= input.Length || input[offset] != '=')
            {
                return false;
            }
            offset++;
            return true;
        }

        private static string ReadToSemicolonOrEnd(string input, ref int offset)
        {
            var end = input.IndexOf(';', offset);
            if (end < 0)
            {
                // Remainder of the string
                end = input.Length;
            }
            var itemLength = end - offset;
            var result = input.Substring(offset, itemLength);
            offset += itemLength;
            return result;
        }

        public override bool Equals(object obj)
        {
            var other = obj as SetCookieHeaderValue;

            if (other == null)
            {
                return false;
            }

            return string.Equals(_name, other._name, StringComparison.OrdinalIgnoreCase)
                && string.Equals(_value, other._value, StringComparison.OrdinalIgnoreCase)
                && Expires.Equals(other.Expires)
                && MaxAge.Equals(other.MaxAge)
                && string.Equals(Domain, other.Domain, StringComparison.OrdinalIgnoreCase)
                && string.Equals(Path, other.Path, StringComparison.OrdinalIgnoreCase)
                && Secure == other.Secure
                && HttpOnly == other.HttpOnly;
        }

        public override int GetHashCode()
        {
            return StringComparer.OrdinalIgnoreCase.GetHashCode(_name)
                ^ StringComparer.OrdinalIgnoreCase.GetHashCode(_value)
                ^ (Expires.HasValue ? Expires.GetHashCode() : 0)
                ^ (MaxAge.HasValue ? MaxAge.GetHashCode() : 0)
                ^ (Domain != null ? StringComparer.OrdinalIgnoreCase.GetHashCode(Domain) : 0)
                ^ (Path != null ? StringComparer.OrdinalIgnoreCase.GetHashCode(Path) : 0)
                ^ Secure.GetHashCode()
                ^ HttpOnly.GetHashCode();
        }
    }
}