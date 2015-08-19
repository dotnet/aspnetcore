// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using Microsoft.Framework.Internal;

namespace Microsoft.AspNet.Mvc.ModelBinding
{
    /// <summary>
    /// Result of an <see cref="IValueProvider.GetValue(string)"/> operation.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <see cref="ValueProviderResult"/> can represent a single submitted value or multiple submitted values.
    /// </para>
    /// <para>
    /// Use <see cref="FirstValue"/> to consume only a single value, regardless of whether a single value or
    /// multiple values were submitted.
    /// </para>
    /// <para>
    /// Treat <see cref="ValueProviderResult"/> as an <see cref="IEnumerable{string}"/> to consume all values,
    /// regardless of whether a single value or multiple values were submitted.
    /// </para>
    /// </remarks>
    public struct ValueProviderResult : IEquatable<ValueProviderResult>, IEnumerable<string>
    {
        private static readonly CultureInfo _invariantCulture = CultureInfo.InvariantCulture;

        /// <summary>
        /// A <see cref="ValueProviderResult"/> that represents a lack of data.
        /// </summary>
        public static ValueProviderResult None = new ValueProviderResult(new string[0]);

        /// <summary>
        /// Creates a new <see cref="ValueProviderResult"/> using <see cref="CultureInfo.InvariantCulture"/>.
        /// </summary>
        /// <param name="value">The submitted value.</param>
        public ValueProviderResult(string value)
            : this(value, _invariantCulture)
        {
        }

        /// <summary>
        /// Creates a new <see cref="ValueProviderResult"/>.
        /// </summary>
        /// <param name="value">The submitted value.</param>
        /// <param name="culture">The <see cref="CultureInfo"/> associated with this value.</param>
        public ValueProviderResult(string value, CultureInfo culture)
        {
            if (value == null)
            {
                Value = null;
                Values = None.Values;
            }
            else
            {
                Value = value;
                Values = null;
            }

            Culture = culture ?? _invariantCulture;
        }

        /// <summary>
        /// Creates a new <see cref="ValueProviderResult"/> using <see cref="CultureInfo.InvariantCulture"/>.
        /// </summary>
        /// <param name="values">The submitted values.</param>
        public ValueProviderResult(string[] values)
            : this(values, _invariantCulture)
        {
        }

        /// <summary>
        /// Creates a new <see cref="ValueProviderResult"/>.
        /// </summary>
        /// <param name="values">The submitted values.</param>
        /// <param name="culture">The <see cref="CultureInfo"/> associated with these values.</param>
        public ValueProviderResult(string[] values, CultureInfo culture)
        {
            if (values == null)
            {
                Value = null;
                Values = None.Values;
            }
            else
            {
                Value = null;
                Values = values;
            }

            Culture = culture;
        }

        /// <summary>
        /// Gets or sets the <see cref="CultureInfo"/> associated with the values.
        /// </summary>
        public CultureInfo Culture { get; private set; }

        /// <summary>
        /// Gets or sets a single value. Will be <c>null</c> if multiple values are present.
        /// </summary>
        public string Value { get; private set; }

        /// <summary>
        /// Gets or sets an array of values. Will be <c>null</c> if only a single value was provided.
        /// </summary>
        public string[] Values { get; private set; }

        /// <summary>
        /// Gets the first value based on the order values were provided in the request. Use <see cref="FirstValue"/>
        /// to get a single value for processing regarless of whether a single or multiple values were provided
        /// in the request.
        /// </summary>
        public string FirstValue
        {
            get
            {
                if (Value != null)
                {
                    return Value;
                }
                else if (Values != null && Values.Length > 0)
                {
                    return Values[0];
                }
                else
                {
                    return null;
                }
            }
        }

        /// <summary>
        /// Gets the number of submitted values.
        /// </summary>
        public int Length
        {
            get
            {
                if (Values != null)
                {
                    return Values.Length;
                }
                else if (Value != null)
                {
                    return 1;
                }
                else
                {
                    return 0;
                }
            }
        }

        /// <inheritdoc />
        public override bool Equals(object obj)
        {
            var other = obj as ValueProviderResult?;
            return other.HasValue ? Equals(other.Value) : false;
        }

        /// <inheritdoc />
        public bool Equals(ValueProviderResult other)
        {
            if (Length != other.Length)
            {
                return false;
            }
            else
            {
                var x = (string[])this;
                var y = (string[])other;
                for (var i = 0; i < x.Length; i++)
                {
                    if (!string.Equals(x[i], y[i], StringComparison.Ordinal))
                    {
                        return false;
                    }
                }
                return true;
            }
        }

        /// <inheritdoc />
        public override int GetHashCode()
        {
            return ((string)this)?.GetHashCode() ?? 0;
        }

        /// <inheritdoc />
        public override string ToString()
        {
            return (string)this;
        }

        /// <summary>
        /// Gets an <see cref="Enumerator"/> for this <see cref="ValueProviderResult"/>.
        /// </summary>
        /// <returns>An <see cref="Enumerator"/>.</returns>
        public Enumerator GetEnumerator()
        {
            return new Enumerator(this);
        }

        /// <inheritdoc />
        IEnumerator<string> IEnumerable<string>.GetEnumerator()
        {
            return GetEnumerator();
        }

        /// <inheritdoc />
        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        /// <summary>
        /// Converts the provided <see cref="ValueProviderResult"/> into a comma-separated string containing all
        /// submitted values.
        /// </summary>
        /// <param name="result">The <see cref="ValueProviderResult"/>.</param>
        public static explicit operator string(ValueProviderResult result)
        {
            if (result.Values == null)
            {
                return result.Value;
            }
            else if (result.Values.Length == 0)
            {
                return null;
            }
            else if (result.Values.Length == 1)
            {
                return result.Values[0];
            }
            else
            {
                return string.Join(",", result.Values);
            }
        }

        /// <summary>
        /// Converts the provided <see cref="ValueProviderResult"/> into a an array of <see cref="string"/> containing
        /// all submitted values.
        /// </summary>
        /// <param name="result">The <see cref="ValueProviderResult"/>.</param>
        public static explicit operator string[](ValueProviderResult result)
        {
            if (result.Values != null)
            {
                return result.Values;
            }
            else if (result.Value != null)
            {
                return new string[] { result.Value };
            }
            else
            {
                return None.Values;
            }
        }

        /// <summary>
        /// Compares two <see cref="ValueProviderResult"/> objects for equality.
        /// </summary>
        /// <param name="x">A <see cref="ValueProviderResult"/>.</param>
        /// <param name="y">A <see cref="ValueProviderResult"/>.</param>
        /// <returns><c>true</c> if the values are equal, otherwise <c>false</c>.</returns>
        public static bool operator ==(ValueProviderResult x, ValueProviderResult y)
        {
            return x.Equals(y);
        }

        /// <summary>
        /// Compares two <see cref="ValueProviderResult"/> objects for inequality.
        /// </summary>
        /// <param name="x">A <see cref="ValueProviderResult"/>.</param>
        /// <param name="y">A <see cref="ValueProviderResult"/>.</param>
        /// <returns><c>false</c> if the values are equal, otherwise <c>true</c>.</returns>
        public static bool operator !=(ValueProviderResult x, ValueProviderResult y)
        {
            return !x.Equals(y);
        }

        /// <summary>
        /// An enumerator for <see cref="ValueProviderResult"/>.
        /// </summary>
        public struct Enumerator : IEnumerator<string>
        {
            private readonly ValueProviderResult _result;
            private readonly int _length;
            private int _index;

            /// <summary>
            /// Creates a new <see cref="Enumerator"/>.
            /// </summary>
            /// <param name="result">The <see cref="ValueProviderResult"/>.</param>
            public Enumerator(ValueProviderResult result)
            {
                _result = result;
                _index = -1;
                _length = result.Length;
                Current = null;
            }

            /// <inheritdoc />
            public string Current { get; private set; }

            /// <inheritdoc />
            object IEnumerator.Current => Current;

            /// <inheritdoc />
            public void Dispose()
            {
            }

            /// <inheritdoc />
            public bool MoveNext()
            {
                ++_index;
                if (_index < _length)
                {
                    if (_result.Values != null)
                    {
                        Debug.Assert(_index < _result.Values.Length);
                        Current = _result.Values[_index];
                        return true;
                    }
                    else if (_result.Value != null && _index == 0)
                    {
                        Current = _result.Value;
                        return true;
                    }
                    else
                    {
                        return false;
                    }
                }

                Current = null;
                return false;
            }

            /// <inheritdoc />
            public void Reset()
            {
                _index = -1;
            }
        }
    }
}
