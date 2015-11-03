// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Globalization;

namespace Microsoft.Net.Http.Headers
{
    internal abstract class HttpHeaderParser<T>
    {
        private bool _supportsMultipleValues;

        protected HttpHeaderParser(bool supportsMultipleValues)
        {
            _supportsMultipleValues = supportsMultipleValues;
        }

        public bool SupportsMultipleValues
        {
            get { return _supportsMultipleValues; }
        }

        // If a parser supports multiple values, a call to ParseValue/TryParseValue should return a value for 'index'
        // pointing to the next non-whitespace character after a delimiter. E.g. if called with a start index of 0
        // for string "value , second_value", then after the call completes, 'index' must point to 's', i.e. the first
        // non-whitespace after the separator ','.
        public abstract bool TryParseValue(string value, ref int index, out T parsedValue);

        public T ParseValue(string value, ref int index)
        {
            // Index may be value.Length (e.g. both 0). This may be allowed for some headers (e.g. Accept but not
            // allowed by others (e.g. Content-Length). The parser has to decide if this is valid or not.
            Contract.Requires((value == null) || ((index >= 0) && (index <= value.Length)));

            // If a parser returns 'null', it means there was no value, but that's valid (e.g. "Accept: "). The caller
            // can ignore the value.
            T result;
            if (!TryParseValue(value, ref index, out result))
            {
                throw new FormatException(string.Format(CultureInfo.InvariantCulture, "Invalid value '{0}'.",
                    value?.Substring(index) ?? "<null>"));
            }
            return result;
        }

        public virtual bool TryParseValues(IList<string> values, out IList<T> parsedValues)
        {
            Contract.Assert(_supportsMultipleValues);
            // If a parser returns an empty list, it means there was no value, but that's valid (e.g. "Accept: "). The caller
            // can ignore the value.
            parsedValues = null;
            var results = new List<T>();
            if (values == null)
            {
                return false;
            }
            foreach (var value in values)
            {
                int index = 0;

                while (!string.IsNullOrEmpty(value) && index < value.Length)
                {
                    T output;
                    if (TryParseValue(value, ref index, out output))
                    {
                        // The entry may not contain an actual value, like " , "
                        if (output != null)
                        {
                            results.Add(output);
                        }
                    }
                    else
                    {
                        return false;
                    }
                }
            }
            if (results.Count > 0)
            {
                parsedValues = results;
                return true;
            }
            return false;
        }

        public IList<T> ParseValues(IList<string> values)
        {
            Contract.Assert(_supportsMultipleValues);
            // If a parser returns an empty list, it means there was no value, but that's valid (e.g. "Accept: "). The caller
            // can ignore the value.
            var parsedValues = new List<T>();
            if (values == null)
            {
                return parsedValues;
            }
            foreach (var value in values)
            {
                int index = 0;

                while (!string.IsNullOrEmpty(value) && index < value.Length)
                {
                    T output;
                    if (TryParseValue(value, ref index, out output))
                    {
                        // The entry may not contain an actual value, like " , "
                        if (output != null)
                        {
                            parsedValues.Add(output);
                        }
                    }
                    else
                    {
                        throw new FormatException(string.Format(CultureInfo.InvariantCulture, "Invalid values '{0}'.",
                            value.Substring(index)));
                    }
                }
            }
            return parsedValues;
        }

        // If ValueType is a custom header value type (e.g. NameValueHeaderValue) it implements ToString() correctly.
        // However for existing types like int, byte[], DateTimeOffset we can't override ToString(). Therefore the
        // parser provides a ToString() virtual method that can be overridden by derived types to correctly serialize
        // values (e.g. byte[] to Base64 encoded string).
        // The default implementation is to just call ToString() on the value itself which is the right thing to do
        // for most headers (custom types, string, etc.).
        public virtual string ToString(object value)
        {
            Contract.Requires(value != null);

            return value.ToString();
        }
    }
}
