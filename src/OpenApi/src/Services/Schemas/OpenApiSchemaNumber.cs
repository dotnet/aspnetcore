// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Numerics;

namespace Microsoft.AspNetCore.OpenApi;

/// <summary>
/// Represents a finite JSON number exactly as an arbitrary-precision significand multiplied by a power of ten.
/// </summary>
/// <remarks>
/// Values are normalized by canonicalizing zero and removing trailing decimal zeroes from the significand.
/// Parsing accepts only invariant JSON number syntax and is limited to 10,000 characters. Significands supplied
/// directly are limited to 10,000 decimal digits.
/// </remarks>
[Experimental("ASP0040", UrlFormat = "https://aka.ms/aspnet/analyzer/{0}")]
public readonly struct OpenApiSchemaNumber :
    IComparable<OpenApiSchemaNumber>,
    IEquatable<OpenApiSchemaNumber>
{
    private const int MaxDigitCount = 10_000;
    private const int MaxTextLength = 10_000;
    private const int MaxPlainZeroCount = 16;
    private const long MaxParsedExponentMagnitude = (long)int.MaxValue + MaxDigitCount + 1;

    /// <summary>
    /// Initializes a new instance of <see cref="OpenApiSchemaNumber"/>.
    /// </summary>
    /// <param name="significand">The arbitrary-precision significand.</param>
    /// <param name="exponent">The power of ten by which <paramref name="significand"/> is multiplied.</param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// The significand contains more than 10,000 decimal digits, or normalization would produce an exponent
    /// outside the range of <see cref="int"/>.
    /// </exception>
    public OpenApiSchemaNumber(BigInteger significand, int exponent)
    {
        if (significand.IsZero)
        {
            Significand = BigInteger.Zero;
            Exponent = 0;
            return;
        }

        var negative = significand.Sign < 0;
        var digits = BigInteger.Abs(significand).ToString(CultureInfo.InvariantCulture);
        if (digits.Length > MaxDigitCount)
        {
            throw new ArgumentOutOfRangeException(nameof(significand), Resources.SchemaNumberSignificandTooLarge);
        }

        var normalizedLength = digits.Length;
        while (digits[normalizedLength - 1] == '0')
        {
            normalizedLength--;
        }

        var normalizedExponent = (long)exponent + digits.Length - normalizedLength;
        if (normalizedExponent is < int.MinValue or > int.MaxValue)
        {
            throw new ArgumentOutOfRangeException(nameof(exponent), Resources.SchemaNumberExponentOutOfRange);
        }

        Significand = normalizedLength == digits.Length
            ? significand
            : BigInteger.Parse(digits.AsSpan(0, normalizedLength), NumberStyles.None, CultureInfo.InvariantCulture) *
                (negative ? BigInteger.MinusOne : BigInteger.One);
        Exponent = (int)normalizedExponent;
    }

    /// <summary>
    /// Gets the normalized arbitrary-precision significand.
    /// </summary>
    public BigInteger Significand { get; }

    /// <summary>
    /// Gets the power of ten by which <see cref="Significand"/> is multiplied.
    /// </summary>
    public int Exponent { get; }

    /// <summary>
    /// Parses an invariant finite JSON number without losing precision.
    /// </summary>
    /// <param name="value">The JSON number text.</param>
    /// <returns>The parsed number.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="value"/> is <see langword="null"/>.</exception>
    /// <exception cref="FormatException">
    /// <paramref name="value"/> is not valid JSON number text, exceeds 10,000 characters, or cannot be represented
    /// by a normalized significand and a 32-bit exponent.
    /// </exception>
    public static OpenApiSchemaNumber Parse(string value)
    {
        ArgumentNullException.ThrowIfNull(value);
        if (value.Length is 0 or > MaxTextLength)
        {
            throw new FormatException(Resources.SchemaNumberInvalid);
        }

        var text = value.AsSpan();
        var index = 0;
        var negative = text[index] == '-';
        if (negative && ++index == text.Length)
        {
            throw new FormatException(Resources.SchemaNumberInvalid);
        }

        var digits = new char[text.Length];
        var digitCount = 0;
        if (text[index] == '0')
        {
            digits[digitCount++] = text[index++];
            if (index < text.Length && IsDigit(text[index]))
            {
                throw new FormatException(Resources.SchemaNumberInvalid);
            }
        }
        else if (text[index] is >= '1' and <= '9')
        {
            do
            {
                digits[digitCount++] = text[index++];
            }
            while (index < text.Length && IsDigit(text[index]));
        }
        else
        {
            throw new FormatException(Resources.SchemaNumberInvalid);
        }

        var fractionalDigitCount = 0;
        if (index < text.Length && text[index] == '.')
        {
            index++;
            var fractionStart = index;
            while (index < text.Length && IsDigit(text[index]))
            {
                digits[digitCount++] = text[index++];
            }

            fractionalDigitCount = index - fractionStart;
            if (fractionalDigitCount == 0)
            {
                throw new FormatException(Resources.SchemaNumberInvalid);
            }
        }

        long explicitExponent = 0;
        if (index < text.Length && text[index] is 'e' or 'E')
        {
            index++;
            var exponentNegative = index < text.Length && text[index] == '-';
            if (index < text.Length && text[index] is '+' or '-')
            {
                index++;
            }

            var exponentStart = index;
            while (index < text.Length && IsDigit(text[index]))
            {
                var digit = text[index++] - '0';
                if (explicitExponent > (MaxParsedExponentMagnitude - digit) / 10)
                {
                    throw new FormatException(Resources.SchemaNumberInvalid);
                }

                explicitExponent = (explicitExponent * 10) + digit;
            }

            if (index == exponentStart)
            {
                throw new FormatException(Resources.SchemaNumberInvalid);
            }

            if (exponentNegative)
            {
                explicitExponent = -explicitExponent;
            }
        }

        if (index != text.Length)
        {
            throw new FormatException(Resources.SchemaNumberInvalid);
        }

        var firstSignificantDigit = 0;
        while (firstSignificantDigit < digitCount && digits[firstSignificantDigit] == '0')
        {
            firstSignificantDigit++;
        }

        if (firstSignificantDigit == digitCount)
        {
            return default;
        }

        var normalizedLength = digitCount;
        while (digits[normalizedLength - 1] == '0')
        {
            normalizedLength--;
        }

        var exponent = explicitExponent - fractionalDigitCount + digitCount - normalizedLength;
        if (exponent is < int.MinValue or > int.MaxValue)
        {
            throw new FormatException(Resources.SchemaNumberInvalid);
        }

        var significand = BigInteger.Parse(
            digits.AsSpan(firstSignificantDigit, normalizedLength - firstSignificantDigit),
            NumberStyles.None,
            CultureInfo.InvariantCulture);
        if (negative)
        {
            significand = -significand;
        }

        return new OpenApiSchemaNumber(significand, (int)exponent);
    }

    /// <inheritdoc />
    public int CompareTo(OpenApiSchemaNumber other)
    {
        var signComparison = Significand.Sign.CompareTo(other.Significand.Sign);
        if (signComparison != 0)
        {
            return signComparison;
        }

        if (Significand.IsZero)
        {
            return 0;
        }

        var leftDigits = BigInteger.Abs(Significand).ToString(CultureInfo.InvariantCulture);
        var rightDigits = BigInteger.Abs(other.Significand).ToString(CultureInfo.InvariantCulture);
        var magnitudeComparison = ((long)leftDigits.Length + Exponent)
            .CompareTo((long)rightDigits.Length + other.Exponent);
        if (magnitudeComparison == 0)
        {
            var length = Math.Max(leftDigits.Length, rightDigits.Length);
            for (var index = 0; index < length; index++)
            {
                var left = index < leftDigits.Length ? leftDigits[index] : '0';
                var right = index < rightDigits.Length ? rightDigits[index] : '0';
                if (left != right)
                {
                    magnitudeComparison = left.CompareTo(right);
                    break;
                }
            }
        }

        return Significand.Sign < 0 ? -magnitudeComparison : magnitudeComparison;
    }

    /// <inheritdoc />
    public bool Equals(OpenApiSchemaNumber other)
        => Significand == other.Significand && Exponent == other.Exponent;

    /// <inheritdoc />
    public override bool Equals([NotNullWhen(true)] object? obj)
        => obj is OpenApiSchemaNumber other && Equals(other);

    /// <inheritdoc />
    public override int GetHashCode() => HashCode.Combine(Significand, Exponent);

    /// <summary>
    /// Formats this value as an invariant JSON number without losing precision.
    /// </summary>
    /// <returns>The exact JSON number text.</returns>
    public override string ToString()
    {
        if (Significand.IsZero)
        {
            return "0";
        }

        var negative = Significand.Sign < 0;
        var digits = BigInteger.Abs(Significand).ToString(CultureInfo.InvariantCulture);
        var sign = negative ? "-" : string.Empty;
        if (Exponent == 0)
        {
            return sign + digits;
        }

        if (Exponent > 0 && Exponent <= MaxPlainZeroCount)
        {
            return sign + digits + new string('0', Exponent);
        }

        if (Exponent < 0)
        {
            var decimalPoint = (long)digits.Length + Exponent;
            if (decimalPoint > 0)
            {
                return string.Concat(sign, digits.AsSpan(0, (int)decimalPoint), ".", digits.AsSpan((int)decimalPoint));
            }

            if (decimalPoint >= -MaxPlainZeroCount)
            {
                return sign + "0." + new string('0', (int)-decimalPoint) + digits;
            }
        }

        return string.Concat(
            sign,
            digits,
            "e",
            Exponent > 0 ? "+" : string.Empty,
            Exponent.ToString(CultureInfo.InvariantCulture));
    }

    /// <summary>
    /// Converts a 32-bit integer to an exact schema number.
    /// </summary>
    public static implicit operator OpenApiSchemaNumber(int value) => new(value, 0);

    /// <summary>
    /// Converts a 64-bit integer to an exact schema number.
    /// </summary>
    public static implicit operator OpenApiSchemaNumber(long value) => new(value, 0);

    /// <summary>
    /// Converts an arbitrary-precision integer to an exact schema number.
    /// </summary>
    public static implicit operator OpenApiSchemaNumber(BigInteger value) => new(value, 0);

    /// <summary>
    /// Converts a decimal value to an exact schema number.
    /// </summary>
    public static implicit operator OpenApiSchemaNumber(decimal value)
        => Parse(value.ToString(CultureInfo.InvariantCulture));

    /// <summary>
    /// Determines whether two values are equal.
    /// </summary>
    public static bool operator ==(OpenApiSchemaNumber left, OpenApiSchemaNumber right) => left.Equals(right);

    /// <summary>
    /// Determines whether two values are not equal.
    /// </summary>
    public static bool operator !=(OpenApiSchemaNumber left, OpenApiSchemaNumber right) => !left.Equals(right);

    /// <summary>
    /// Determines whether one value is less than another.
    /// </summary>
    public static bool operator <(OpenApiSchemaNumber left, OpenApiSchemaNumber right) => left.CompareTo(right) < 0;

    /// <summary>
    /// Determines whether one value is less than or equal to another.
    /// </summary>
    public static bool operator <=(OpenApiSchemaNumber left, OpenApiSchemaNumber right) => left.CompareTo(right) <= 0;

    /// <summary>
    /// Determines whether one value is greater than another.
    /// </summary>
    public static bool operator >(OpenApiSchemaNumber left, OpenApiSchemaNumber right) => left.CompareTo(right) > 0;

    /// <summary>
    /// Determines whether one value is greater than or equal to another.
    /// </summary>
    public static bool operator >=(OpenApiSchemaNumber left, OpenApiSchemaNumber right) => left.CompareTo(right) >= 0;

    private static bool IsDigit(char value) => value is >= '0' and <= '9';
}
