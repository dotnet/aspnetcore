// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Text;
using Microsoft.Extensions.Primitives;

namespace Microsoft.Net.Http.Headers
{
    // http://tools.ietf.org/html/rfc6265
    public class CookieHeaderValue
    {
        private static readonly CookieHeaderParser SingleValueParser = new CookieHeaderParser(supportsMultipleValues: false);
        private static readonly CookieHeaderParser MultipleValueParser = new CookieHeaderParser(supportsMultipleValues: true);

        private StringSegment _name;
        private StringSegment _value;

        private CookieHeaderValue()
        {
            // Used by the parser to create a new instance of this type.
        }

        public CookieHeaderValue(StringSegment name)
            : this(name, StringSegment.Empty)
        {
            if (name == null)
            {
                throw new ArgumentNullException(nameof(name));
            }
        }

        public CookieHeaderValue(StringSegment name, StringSegment value)
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

        public StringSegment Name
        {
            get { return _name; }
            set
            {
                CheckNameFormat(value, nameof(value));
                _name = value;
            }
        }

        public StringSegment Value
        {
            get { return _value; }
            set
            {
                CheckValueFormat(value, nameof(value));
                _value = value;
            }
        }

        // name="val ue";
        public override string ToString()
        {
            var header = new StringBuilder();

            header.Append(_name);
            header.Append("=");
            header.Append(_value);

            return header.ToString();
        }

        public static CookieHeaderValue Parse(StringSegment input)
        {
            var index = 0;
            return SingleValueParser.ParseValue(input, ref index);
        }

        public static bool TryParse(StringSegment input, out CookieHeaderValue parsedValue)
        {
            var index = 0;
            return SingleValueParser.TryParseValue(input, ref index, out parsedValue);
        }

        public static IList<CookieHeaderValue> ParseList(IList<string> inputs)
        {
            return MultipleValueParser.ParseValues(inputs);
        }

        public static IList<CookieHeaderValue> ParseStrictList(IList<string> inputs)
        {
            return MultipleValueParser.ParseStrictValues(inputs);
        }

        public static bool TryParseList(IList<string> inputs, out IList<CookieHeaderValue> parsedValues)
        {
            return MultipleValueParser.TryParseValues(inputs, out parsedValues);
        }

        public static bool TryParseStrictList(IList<string> inputs, out IList<CookieHeaderValue> parsedValues)
        {
            return MultipleValueParser.TryParseStrictValues(inputs, out parsedValues);
        }

        // name=value; name="value"
        internal static bool TryGetCookieLength(StringSegment input, ref int offset, out CookieHeaderValue parsedValue)
        {
            Contract.Requires(offset >= 0);

            parsedValue = null;

            if (StringSegment.IsNullOrEmpty(input) || (offset >= input.Length))
            {
                return false;
            }

            var result = new CookieHeaderValue();

            // The caller should have already consumed any leading whitespace, commas, etc..

            // Name=value;

            // Name
            var itemLength = HttpRuleParser.GetTokenLength(input, offset);
            if (itemLength == 0)
            {
                return false;
            }
            result._name = input.Subsegment(offset, itemLength);
            offset += itemLength;

            // = (no spaces)
            if (!ReadEqualsSign(input, ref offset))
            {
                return false;
            }

            // value or "quoted value"
            // The value may be empty
            result._value = GetCookieValue(input, ref offset);

            parsedValue = result;
            return true;
        }

        // cookie-value      = *cookie-octet / ( DQUOTE* cookie-octet DQUOTE )
        // cookie-octet      = %x21 / %x23-2B / %x2D-3A / %x3C-5B / %x5D-7E
        //                     ; US-ASCII characters excluding CTLs, whitespace DQUOTE, comma, semicolon, and backslash
        internal static StringSegment GetCookieValue(StringSegment input, ref int offset)
        {
            Contract.Requires(input != null);
            Contract.Requires(offset >= 0);
            Contract.Ensures((Contract.Result<int>() >= 0) && (Contract.Result<int>() <= (input.Length - offset)));

            var startIndex = offset;

            if (offset >= input.Length)
            {
                return StringSegment.Empty;
            }
            var inQuotes = false;

            if (input[offset] == '"')
            {
                inQuotes = true;
                offset++;
            }

            while (offset < input.Length)
            {
                var c = input[offset];
                if (!IsCookieValueChar(c))
                {
                    break;
                }

                offset++;
            }

            if (inQuotes)
            {
                if (offset == input.Length || input[offset] != '"')
                {
                    // Missing final quote
                    return StringSegment.Empty;
                }
                offset++;
            }

            int length = offset - startIndex;
            if (offset > startIndex)
            {
                return input.Subsegment(startIndex, length);
            }

            return StringSegment.Empty;
        }

        private static bool ReadEqualsSign(StringSegment input, ref int offset)
        {
            // = (no spaces)
            if (offset >= input.Length || input[offset] != '=')
            {
                return false;
            }
            offset++;
            return true;
        }

        // cookie-octet      = %x21 / %x23-2B / %x2D-3A / %x3C-5B / %x5D-7E
        //                     ; US-ASCII characters excluding CTLs, whitespace DQUOTE, comma, semicolon, and backslash
        private static bool IsCookieValueChar(char c)
        {
            if (c < 0x21 || c > 0x7E)
            {
                return false;
            }
            return !(c == '"' || c == ',' || c == ';' || c == '\\');
        }

        internal static void CheckNameFormat(StringSegment name, string parameterName)
        {
            if (name == null)
            {
                throw new ArgumentNullException(nameof(name));
            }

            if (HttpRuleParser.GetTokenLength(name, 0) != name.Length)
            {
                throw new ArgumentException("Invalid cookie name: " + name, parameterName);
            }
        }

        internal static void CheckValueFormat(StringSegment value, string parameterName)
        {
            if (value == null)
            {
                throw new ArgumentNullException(nameof(value));
            }

            var offset = 0;
            var result = GetCookieValue(value, ref offset);
            if (result.Length != value.Length)
            {
                throw new ArgumentException("Invalid cookie value: " + value, parameterName);
            }
        }

        public override bool Equals(object obj)
        {
            var other = obj as CookieHeaderValue;

            if (other == null)
            {
                return false;
            }

            return StringSegment.Equals(_name, other._name, StringComparison.OrdinalIgnoreCase)
                && StringSegment.Equals(_value, other._value, StringComparison.OrdinalIgnoreCase);
        }

        public override int GetHashCode()
        {
            return _name.GetHashCode() ^ _value.GetHashCode();
        }
    }
}