// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Text;

namespace Microsoft.Net.Http.Headers
{
    // According to the RFC, in places where a "parameter" is required, the value is mandatory
    // (e.g. Media-Type, Accept). However, we don't introduce a dedicated type for it. So NameValueHeaderValue supports
    // name-only values in addition to name/value pairs.
    public class NameValueHeaderValue
    {
        private static readonly HttpHeaderParser<NameValueHeaderValue> SingleValueParser
            = new GenericHeaderParser<NameValueHeaderValue>(false, GetNameValueLength);
        internal static readonly HttpHeaderParser<NameValueHeaderValue> MultipleValueParser
            = new GenericHeaderParser<NameValueHeaderValue>(true, GetNameValueLength);

        private string _name;
        private string _value;
        private bool _isReadOnly;

        private NameValueHeaderValue()
        {
            // Used by the parser to create a new instance of this type.
        }

        public NameValueHeaderValue(string name)
            : this(name, null)
        {
        }

        public NameValueHeaderValue(string name, string value)
        {
            CheckNameValueFormat(name, value);

            _name = name;
            _value = value;
        }

        public string Name
        {
            get { return _name; }
        }

        public string Value
        {
            get { return _value; }
            set
            {
                HeaderUtilities.ThrowIfReadOnly(IsReadOnly);
                CheckValueFormat(value);
                _value = value;
            }
        }

        public bool IsReadOnly { get { return _isReadOnly; } }

        /// <summary>
        /// Provides a copy of this object without the cost of re-validating the values.
        /// </summary>
        /// <returns>A copy.</returns>
        public NameValueHeaderValue Copy()
        {
            return new NameValueHeaderValue()
            {
                _name = _name,
                _value = _value
            };
        }

        public NameValueHeaderValue CopyAsReadOnly()
        {
            if (IsReadOnly)
            {
                return this;
            }

            return new NameValueHeaderValue()
            {
                _name = _name,
                _value = _value,
                _isReadOnly = true
            };
        }

        public override int GetHashCode()
        {
            Contract.Assert(_name != null);

            var nameHashCode = StringComparer.OrdinalIgnoreCase.GetHashCode(_name);

            if (!string.IsNullOrEmpty(_value))
            {
                // If we have a quoted-string, then just use the hash code. If we have a token, convert to lowercase
                // and retrieve the hash code.
                if (_value[0] == '"')
                {
                    return nameHashCode ^ _value.GetHashCode();
                }

                return nameHashCode ^ StringComparer.OrdinalIgnoreCase.GetHashCode(_value);
            }

            return nameHashCode;
        }

        public override bool Equals(object obj)
        {
            var other = obj as NameValueHeaderValue;

            if (other == null)
            {
                return false;
            }

            if (string.Compare(_name, other._name, StringComparison.OrdinalIgnoreCase) != 0)
            {
                return false;
            }

            // RFC2616: 14.20: unquoted tokens should use case-INsensitive comparison; quoted-strings should use
            // case-sensitive comparison. The RFC doesn't mention how to compare quoted-strings outside the "Expect"
            // header. We treat all quoted-strings the same: case-sensitive comparison.

            if (string.IsNullOrEmpty(_value))
            {
                return string.IsNullOrEmpty(other._value);
            }

            if (_value[0] == '"')
            {
                // We have a quoted string, so we need to do case-sensitive comparison.
                return (string.CompareOrdinal(_value, other._value) == 0);
            }
            else
            {
                return (string.Compare(_value, other._value, StringComparison.OrdinalIgnoreCase) == 0);
            }
        }

        public static NameValueHeaderValue Parse(string input)
        {
            var index = 0;
            return SingleValueParser.ParseValue(input, ref index);
        }

        public static bool TryParse(string input, out NameValueHeaderValue parsedValue)
        {
            var index = 0;
            return SingleValueParser.TryParseValue(input, ref index, out parsedValue);
        }

        public static IList<NameValueHeaderValue> ParseList(IList<string> input)
        {
            return MultipleValueParser.ParseValues(input);
        }

        public static bool TryParseList(IList<string> input, out IList<NameValueHeaderValue> parsedValues)
        {
            return MultipleValueParser.TryParseValues(input, out parsedValues);
        }

        public override string ToString()
        {
            if (!string.IsNullOrEmpty(_value))
            {
                return _name + "=" + _value;
            }
            return _name;
        }

        internal static void ToString(
            IList<NameValueHeaderValue> values,
            char separator,
            bool leadingSeparator,
            StringBuilder destination)
        {
            Contract.Assert(destination != null);

            if ((values == null) || (values.Count == 0))
            {
                return;
            }

            for (var i = 0; i < values.Count; i++)
            {
                if (leadingSeparator || (destination.Length > 0))
                {
                    destination.Append(separator);
                    destination.Append(' ');
                }
                destination.Append(values[i].ToString());
            }
        }

        internal static string ToString(IList<NameValueHeaderValue> values, char separator, bool leadingSeparator)
        {
            if ((values == null) || (values.Count == 0))
            {
                return null;
            }

            var sb = new StringBuilder();

            ToString(values, separator, leadingSeparator, sb);

            return sb.ToString();
        }

        internal static int GetHashCode(IList<NameValueHeaderValue> values)
        {
            if ((values == null) || (values.Count == 0))
            {
                return 0;
            }

            var result = 0;
            for (var i = 0; i < values.Count; i++)
            {
                result = result ^ values[i].GetHashCode();
            }
            return result;
        }

        private static int GetNameValueLength(string input, int startIndex, out NameValueHeaderValue parsedValue)
        {
            Contract.Requires(input != null);
            Contract.Requires(startIndex >= 0);

            parsedValue = null;

            if (string.IsNullOrEmpty(input) || (startIndex >= input.Length))
            {
                return 0;
            }

            // Parse the name, i.e. <name> in name/value string "<name>=<value>". Caller must remove
            // leading whitespaces.
            var nameLength = HttpRuleParser.GetTokenLength(input, startIndex);

            if (nameLength == 0)
            {
                return 0;
            }

            var name = input.Substring(startIndex, nameLength);
            var current = startIndex + nameLength;
            current = current + HttpRuleParser.GetWhitespaceLength(input, current);

            // Parse the separator between name and value
            if ((current == input.Length) || (input[current] != '='))
            {
                // We only have a name and that's OK. Return.
                parsedValue = new NameValueHeaderValue();
                parsedValue._name = name;
                current = current + HttpRuleParser.GetWhitespaceLength(input, current); // skip whitespaces
                return current - startIndex;
            }

            current++; // skip delimiter.
            current = current + HttpRuleParser.GetWhitespaceLength(input, current);

            // Parse the value, i.e. <value> in name/value string "<name>=<value>"
            int valueLength = GetValueLength(input, current);

            // Value after the '=' may be empty
            // Use parameterless ctor to avoid double-parsing of name and value, i.e. skip public ctor validation.
            parsedValue = new NameValueHeaderValue();
            parsedValue._name = name;
            parsedValue._value = input.Substring(current, valueLength);
            current = current + valueLength;
            current = current + HttpRuleParser.GetWhitespaceLength(input, current); // skip whitespaces
            return current - startIndex;
        }

        // Returns the length of a name/value list, separated by 'delimiter'. E.g. "a=b, c=d, e=f" adds 3
        // name/value pairs to 'nameValueCollection' if 'delimiter' equals ','.
        internal static int GetNameValueListLength(
            string input,
            int startIndex,
            char delimiter,
            IList<NameValueHeaderValue> nameValueCollection)
        {
            Contract.Requires(nameValueCollection != null);
            Contract.Requires(startIndex >= 0);

            if ((string.IsNullOrEmpty(input)) || (startIndex >= input.Length))
            {
                return 0;
            }

            var current = startIndex + HttpRuleParser.GetWhitespaceLength(input, startIndex);
            while (true)
            {
                NameValueHeaderValue parameter = null;
                var nameValueLength = GetNameValueLength(input, current, out parameter);

                if (nameValueLength == 0)
                {
                    // There may be a trailing ';'
                    return current - startIndex;
                }

                nameValueCollection.Add(parameter);
                current = current + nameValueLength;
                current = current + HttpRuleParser.GetWhitespaceLength(input, current);

                if ((current == input.Length) || (input[current] != delimiter))
                {
                    // We're done and we have at least one valid name/value pair.
                    return current - startIndex;
                }

                // input[current] is 'delimiter'. Skip the delimiter and whitespaces and try to parse again.
                current++; // skip delimiter.
                current = current + HttpRuleParser.GetWhitespaceLength(input, current);
            }
        }

        public static NameValueHeaderValue Find(IList<NameValueHeaderValue> values, string name)
        {
            Contract.Requires((name != null) && (name.Length > 0));

            if ((values == null) || (values.Count == 0))
            {
                return null;
            }

            for (var i = 0; i < values.Count; i++)
            {
                var value = values[i];
                if (string.Compare(value.Name, name, StringComparison.OrdinalIgnoreCase) == 0)
                {
                    return value;
                }
            }
            return null;
        }

        internal static int GetValueLength(string input, int startIndex)
        {
            Contract.Requires(input != null);

            if (startIndex >= input.Length)
            {
                return 0;
            }

            var valueLength = HttpRuleParser.GetTokenLength(input, startIndex);

            if (valueLength == 0)
            {
                // A value can either be a token or a quoted string. Check if it is a quoted string.
                if (HttpRuleParser.GetQuotedStringLength(input, startIndex, out valueLength) != HttpParseResult.Parsed)
                {
                    // We have an invalid value. Reset the name and return.
                    return 0;
                }
            }
            return valueLength;
        }

        private static void CheckNameValueFormat(string name, string value)
        {
            HeaderUtilities.CheckValidToken(name, nameof(name));
            CheckValueFormat(value);
        }

        private static void CheckValueFormat(string value)
        {
            // Either value is null/empty or a valid token/quoted string
            if (!(string.IsNullOrEmpty(value) || (GetValueLength(value, 0) == value.Length)))
            {
                throw new FormatException(string.Format(System.Globalization.CultureInfo.InvariantCulture, "The header value is invalid: '{0}'", value));
            }
        }

        private static NameValueHeaderValue CreateNameValue()
        {
            return new NameValueHeaderValue();
        }
    }
}
