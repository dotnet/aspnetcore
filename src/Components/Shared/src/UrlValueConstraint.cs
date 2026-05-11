// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;

namespace Microsoft.AspNetCore.Components.Routing;

/// <summary>
/// Shared logic for parsing tokens from route values and querystring values.
/// </summary>
internal abstract class UrlValueConstraint
{
    public delegate bool TryParseDelegate<T>(ReadOnlySpan<char> str, [MaybeNullWhen(false)] out T result);

    private static readonly ConcurrentDictionary<Type, UrlValueConstraint> _cachedInstances = new();

    public static bool TryGetByTargetType(Type targetType, [MaybeNullWhen(false)] out UrlValueConstraint result)
    {
        if (!_cachedInstances.TryGetValue(targetType, out result))
        {
            result = Create(targetType);
            if (result is null)
            {
                return false;
            }

            _cachedInstances.TryAdd(targetType, result);
        }

        return true;
    }

    private static bool TryParse(ReadOnlySpan<char> str, out string result)
    {
        result = str.ToString();
        return true;
    }

    private static bool TryParse(ReadOnlySpan<char> str, out DateTime result)
        => DateTime.TryParse(str, CultureInfo.InvariantCulture, DateTimeStyles.None, out result);

    private static bool TryParse(ReadOnlySpan<char> str, out DateOnly result)
        => DateOnly.TryParse(str, CultureInfo.InvariantCulture, DateTimeStyles.None, out result);

    private static bool TryParse(ReadOnlySpan<char> str, out TimeOnly result)
        => TimeOnly.TryParse(str, CultureInfo.InvariantCulture, DateTimeStyles.None, out result);

    private static bool TryParse(ReadOnlySpan<char> str, out decimal result)
        => decimal.TryParse(str, NumberStyles.Number, CultureInfo.InvariantCulture, out result);

    private static bool TryParse(ReadOnlySpan<char> str, out double result)
        => double.TryParse(str, NumberStyles.Number, CultureInfo.InvariantCulture, out result);

    private static bool TryParse(ReadOnlySpan<char> str, out float result)
        => float.TryParse(str, NumberStyles.Number, CultureInfo.InvariantCulture, out result);

    private static bool TryParse(ReadOnlySpan<char> str, out int result)
        => int.TryParse(str, NumberStyles.Integer, CultureInfo.InvariantCulture, out result);

    private static bool TryParse(ReadOnlySpan<char> str, out long result)
        => long.TryParse(str, NumberStyles.Integer, CultureInfo.InvariantCulture, out result);

    private static UrlValueConstraint? Create(Type targetType) => targetType switch
    {
        var x when x == typeof(string) => new TypedUrlValueConstraint<string>(TryParse),
        var x when x == typeof(bool) => new TypedUrlValueConstraint<bool>(bool.TryParse),
        var x when x == typeof(bool?) => new NullableTypedUrlValueConstraint<bool>(bool.TryParse),
        var x when x == typeof(DateTime) => new TypedUrlValueConstraint<DateTime>(TryParse),
        var x when x == typeof(DateTime?) => new NullableTypedUrlValueConstraint<DateTime>(TryParse),
        var x when x == typeof(DateOnly) => new TypedUrlValueConstraint<DateOnly>(TryParse),
        var x when x == typeof(DateOnly?) => new NullableTypedUrlValueConstraint<DateOnly>(TryParse),
        var x when x == typeof(TimeOnly) => new TypedUrlValueConstraint<TimeOnly>(TryParse),
        var x when x == typeof(TimeOnly?) => new NullableTypedUrlValueConstraint<TimeOnly>(TryParse),
        var x when x == typeof(decimal) => new TypedUrlValueConstraint<decimal>(TryParse),
        var x when x == typeof(decimal?) => new NullableTypedUrlValueConstraint<decimal>(TryParse),
        var x when x == typeof(double) => new TypedUrlValueConstraint<double>(TryParse),
        var x when x == typeof(double?) => new NullableTypedUrlValueConstraint<double>(TryParse),
        var x when x == typeof(float) => new TypedUrlValueConstraint<float>(TryParse),
        var x when x == typeof(float?) => new NullableTypedUrlValueConstraint<float>(TryParse),
        var x when x == typeof(Guid) => new TypedUrlValueConstraint<Guid>(Guid.TryParse),
        var x when x == typeof(Guid?) => new NullableTypedUrlValueConstraint<Guid>(Guid.TryParse),
        var x when x == typeof(int) => new TypedUrlValueConstraint<int>(TryParse),
        var x when x == typeof(int?) => new NullableTypedUrlValueConstraint<int>(TryParse),
        var x when x == typeof(long) => new TypedUrlValueConstraint<long>(TryParse),
        var x when x == typeof(long?) => new NullableTypedUrlValueConstraint<long>(TryParse),
        var x => null
    };

    public abstract bool TryParse(ReadOnlySpan<char> value, [MaybeNullWhen(false)] out object result);

    public abstract object? Parse(ReadOnlySpan<char> value, string destinationNameForMessage);

    public abstract Array ParseMultiple(StringSegmentAccumulator values, string destinationNameForMessage);

    private class TypedUrlValueConstraint<T> : UrlValueConstraint
    {
        private readonly TryParseDelegate<T> _parser;

        public TypedUrlValueConstraint(TryParseDelegate<T> parser)
        {
            _parser = parser;
        }

        public override bool TryParse(ReadOnlySpan<char> value, [MaybeNullWhen(false)] out object result)
        {
            if (_parser(value, out var typedResult))
            {
                result = typedResult!;
                return true;
            }
            else
            {
                result = null;
                return false;
            }
        }

        public override object? Parse(ReadOnlySpan<char> value, string destinationNameForMessage)
        {
            if (!_parser(value, out var parsedValue))
            {
                throw new InvalidOperationException($"Cannot parse the value '{value.ToString()}' as type '{typeof(T)}' for '{destinationNameForMessage}'.");
            }

            return parsedValue;
        }

        public override Array ParseMultiple(StringSegmentAccumulator values, string destinationNameForMessage)
        {
            var count = values.Count;
            if (count == 0)
            {
                return Array.Empty<T>();
            }

            var result = new T?[count];

            for (var i = 0; i < count; i++)
            {
                if (!_parser(values[i].Span, out result[i]))
                {
                    throw new InvalidOperationException($"Cannot parse the value '{values[i]}' as type '{typeof(T)}' for '{destinationNameForMessage}'.");
                }
            }

            return result;
        }
    }

    private sealed class NullableTypedUrlValueConstraint<T> : TypedUrlValueConstraint<T?> where T : struct
    {
        public NullableTypedUrlValueConstraint(TryParseDelegate<T> parser)
            : base(SupportNullable(parser))
        {
        }

        private static TryParseDelegate<T?> SupportNullable(TryParseDelegate<T> parser)
        {
            return TryParseNullable;

            bool TryParseNullable(ReadOnlySpan<char> value, [MaybeNullWhen(false)] out T? result)
            {
                if (value.IsEmpty)
                {
                    result = default;
                    return true;
                }
                else if (parser(value, out var parsedValue))
                {
                    result = parsedValue;
                    return true;
                }
                else
                {
                    result = default;
                    return false;
                }
            }
        }
    }
}
