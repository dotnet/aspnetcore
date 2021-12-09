// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.Globalization;
using Microsoft.Extensions.Primitives;

namespace Microsoft.AspNetCore.Mvc.ModelBinding;

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
/// Treat <see cref="ValueProviderResult"/> as an <see cref="IEnumerable{String}"/> to consume all values,
/// regardless of whether a single value or multiple values were submitted.
/// </para>
/// </remarks>
public readonly struct ValueProviderResult : IEquatable<ValueProviderResult>, IEnumerable<string>
{
    private static readonly CultureInfo _invariantCulture = CultureInfo.InvariantCulture;

    /// <summary>
    /// A <see cref="ValueProviderResult"/> that represents a lack of data.
    /// </summary>
    public static ValueProviderResult None = new ValueProviderResult(Array.Empty<string>());

    /// <summary>
    /// Creates a new <see cref="ValueProviderResult"/> using <see cref="CultureInfo.InvariantCulture"/>.
    /// </summary>
    /// <param name="values">The submitted values.</param>
    public ValueProviderResult(StringValues values)
        : this(values, _invariantCulture)
    {
    }

    /// <summary>
    /// Creates a new <see cref="ValueProviderResult"/>.
    /// </summary>
    /// <param name="values">The submitted values.</param>
    /// <param name="culture">The <see cref="CultureInfo"/> associated with this value.</param>
    public ValueProviderResult(StringValues values, CultureInfo? culture)
    {
        Values = values;
        Culture = culture ?? _invariantCulture;
    }

    /// <summary>
    /// Gets or sets the <see cref="CultureInfo"/> associated with the values.
    /// </summary>
    public CultureInfo Culture { get; }

    /// <summary>
    /// Gets or sets the values.
    /// </summary>
    public StringValues Values { get; }

    /// <summary>
    /// Gets the first value based on the order values were provided in the request. Use <see cref="FirstValue"/>
    /// to get a single value for processing regardless of whether a single or multiple values were provided
    /// in the request.
    /// </summary>
    public string? FirstValue
    {
        get
        {
            if (Values.Count == 0)
            {
                return null;
            }
            return Values[0];
        }
    }

    /// <summary>
    /// Gets the number of submitted values.
    /// </summary>
    public int Length => Values.Count;

    /// <inheritdoc />
    public override bool Equals(object? obj)
    {
        var other = obj as ValueProviderResult?;
        return other.HasValue && Equals(other.Value);
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
            var x = Values;
            var y = other.Values;
            for (var i = 0; i < x.Count; i++)
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
        return ToString().GetHashCode();
    }

    /// <inheritdoc />
    public override string ToString()
    {
        return Values.ToString();
    }

    /// <summary>
    /// Gets an <see cref="IEnumerator{String}"/> for this <see cref="ValueProviderResult"/>.
    /// </summary>
    /// <returns>An <see cref="IEnumerator{String}"/>.</returns>
    public IEnumerator<string> GetEnumerator()
    {
        return ((IEnumerable<string>)Values).GetEnumerator();
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
        return result.Values.ToString();
    }

    /// <summary>
    /// Converts the provided <see cref="ValueProviderResult"/> into a an array of <see cref="string"/> containing
    /// all submitted values.
    /// </summary>
    /// <param name="result">The <see cref="ValueProviderResult"/>.</param>
    public static explicit operator string[](ValueProviderResult result)
    {
        // ToArray() handles the entirely-null case and we assume individual values are never null.
#pragma warning disable CS8619 // Nullability of reference types in value doesn't match target type.
        return result.Values.ToArray();
#pragma warning restore CS8619 // Nullability of reference types in value doesn't match target type.
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
}
