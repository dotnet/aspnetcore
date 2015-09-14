// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Text;

namespace Microsoft.Net.Http.Headers
{
    // http://tools.ietf.org/html/rfc6265
    public class CookieHeaderValue
    {
        private static readonly CookieHeaderParser SingleValueParser = new CookieHeaderParser(supportsMultipleValues: false);
        private static readonly CookieHeaderParser MultipleValueParser = new CookieHeaderParser(supportsMultipleValues: true);

        private string _name;
        private string _value;

        private CookieHeaderValue()
        {
            // Used by the parser to create a new instance of this type.
        }

        public CookieHeaderValue(string name)
            : this(name, string.Empty)
        {
            if (name == null)
            {
                throw new ArgumentNullException(nameof(name));
            }
        }

        public CookieHeaderValue(string name, string value)
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
                CheckNameFormat(value, nameof(value));
                _name = value;
            }
        }

        public string Value
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

        public static CookieHeaderValue Parse(string input)
        {
            var index = 0;
            return SingleValueParser.ParseValue(input, ref index);
        }

        public static bool TryParse(string input, out CookieHeaderValue parsedValue)
        {
            var index = 0;
            return SingleValueParser.TryParseValue(input, ref index, out parsedValue);
        }

        public static IList<CookieHeaderValue> ParseList(IList<string> inputs)
        {
            return MultipleValueParser.ParseValues(inputs);
        }

        public static bool TryParseList(IList<string> inputs, out IList<CookieHeaderValue> parsedValues)
        {
            return MultipleValueParser.TryParseValues(inputs, out parsedValues);
        }

        // name=value; name="value"
        internal static int GetCookieLength(string input, int startIndex, out CookieHeaderValue parsedValue)
        {
            Contract.Requires(startIndex >= 0);
            var offset = startIndex;

            parsedValue = null;

            if (string.IsNullOrEmpty(input) || (offset >= input.Length))
            {
                return 0;
            }

            var result = new CookieHeaderValue();

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

            string value;
            // value or "quoted value"
            itemLength = GetCookieValueLength(input, offset, out value);
            // The value may be empty
            result._value = input.Substring(offset, itemLength);
            offset += itemLength;

            parsedValue = result;
            return offset - startIndex;
        }

        // cookie-value      = *cookie-octet / ( DQUOTE* cookie-octet DQUOTE )
        // cookie-octet      = %x21 / %x23-2B / %x2D-3A / %x3C-5B / %x5D-7E
        //                     ; US-ASCII characters excluding CTLs, whitespace DQUOTE, comma, semicolon, and backslash
        internal static int GetCookieValueLength(string input, int startIndex, out string value)
        {
            Contract.Requires(input != null);
            Contract.Requires(startIndex >= 0);
            Contract.Ensures((Contract.Result<int>() >= 0) && (Contract.Result<int>() <= (input.Length - startIndex)));

            value = null;
            if (startIndex >= input.Length)
            {
                return 0;
            }
            var inQuotes = false;
            var offset = startIndex;

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
                    return 0; // Missing final quote
                }
                offset++;
            }

            int length = offset - startIndex;
            if (length == 0)
            {
                return 0;
            }

            value = input.Substring(startIndex, length);
            return length;
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

        internal static void CheckNameFormat(string name, string parameterName)
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

        internal static void CheckValueFormat(string value, string parameterName)
        {
            if (value == null)
            {
                throw new ArgumentNullException(nameof(value));
            }

            string temp;
            if (GetCookieValueLength(value, 0, out temp) != value.Length)
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

            return string.Equals(_name, other._name, StringComparison.OrdinalIgnoreCase)
                && string.Equals(_value, other._value, StringComparison.OrdinalIgnoreCase);
        }

        public override int GetHashCode()
        {
            return _name.GetHashCode() ^ _value.GetHashCode();
        }
    }
}