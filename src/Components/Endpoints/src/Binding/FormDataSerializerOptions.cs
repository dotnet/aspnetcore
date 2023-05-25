// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Components.Endpoints.Binding;

internal class FormDataSerializerOptions
{
    private readonly Dictionary<Type, FormDataConverter> _converters = new();

    public FormDataSerializerOptions()
    {
        _converters.Add(typeof(string), new ParsableConverter<string>());
        _converters.Add(typeof(char), new ParsableConverter<char>());
        _converters.Add(typeof(bool), new ParsableConverter<bool>());
        _converters.Add(typeof(byte), new ParsableConverter<byte>());
        _converters.Add(typeof(sbyte), new ParsableConverter<sbyte>());
        _converters.Add(typeof(ushort), new ParsableConverter<ushort>());
        _converters.Add(typeof(uint), new ParsableConverter<uint>());
        _converters.Add(typeof(ulong), new ParsableConverter<ulong>());
        _converters.Add(typeof(Int128), new ParsableConverter<Int128>());
        _converters.Add(typeof(short), new ParsableConverter<short>());
        _converters.Add(typeof(int), new ParsableConverter<int>());
        _converters.Add(typeof(long), new ParsableConverter<long>());
        _converters.Add(typeof(UInt128), new ParsableConverter<UInt128>());
        _converters.Add(typeof(Half), new ParsableConverter<Half>());
        _converters.Add(typeof(float), new ParsableConverter<float>());
        _converters.Add(typeof(double), new ParsableConverter<double>());
        _converters.Add(typeof(decimal), new ParsableConverter<decimal>());
        _converters.Add(typeof(DateOnly), new ParsableConverter<DateOnly>());
        _converters.Add(typeof(DateTime), new ParsableConverter<DateTime>());
        _converters.Add(typeof(DateTimeOffset), new ParsableConverter<DateTimeOffset>());
        _converters.Add(typeof(TimeSpan), new ParsableConverter<TimeSpan>());
        _converters.Add(typeof(TimeOnly), new ParsableConverter<TimeOnly>());
        _converters.Add(typeof(Guid), new ParsableConverter<Guid>());
    }

    internal bool HasConverter(Type valueType) => _converters.ContainsKey(valueType);

    internal bool IsSingleValueConverter(Type type)
    {
        return _converters.TryGetValue(type, out var converter) &&
            converter is ISingleValueConverter;
    }

    internal FormDataConverter<T> ResolveConverter<T>()
    {
        if (!_converters.TryGetValue(typeof(T), out var converter))
        {
            throw new InvalidOperationException($"No converter registered for type '{typeof(T)}'.");
        }

        return (FormDataConverter<T>)converter;
    }
}
