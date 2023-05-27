// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Concurrent;

namespace Microsoft.AspNetCore.Components.Endpoints.Binding;

internal class FormDataSerializerOptions
{
    private readonly ConcurrentDictionary<Type, FormDataConverter> _converters = new();
    private readonly List<Func<Type, FormDataSerializerOptions, FormDataConverter?>> _factories = new();

    public FormDataSerializerOptions()
    {
        _converters = new(WellKnownConverters.Converters);

        _factories.Add((type, options) => ParsableConverterFactory.CanConvert(type, options) ? ParsableConverterFactory.CreateConverter(type, options) : null);
        _factories.Add((type, options) => NullableConverterFactory.CanConvert(type, options) ? NullableConverterFactory.CreateConverter(type, options) : null);
    }

    internal bool HasConverter(Type valueType) => _converters.ContainsKey(valueType);

    internal bool IsSingleValueConverter(Type type)
    {
        return _converters.TryGetValue(type, out var converter) &&
            converter is ISingleValueConverter;
    }

    internal FormDataConverter<T> ResolveConverter<T>()
    {
        return (FormDataConverter<T>)_converters.GetOrAdd(typeof(T), CreateConverter, this);
    }

    private static FormDataConverter CreateConverter(Type type, FormDataSerializerOptions options)
    {
        FormDataConverter? converter;
        foreach (var factory in options._factories)
        {
            converter = factory(type, options);
            if (converter != null)
            {
                return converter;
            }
        }

        throw new InvalidOperationException($"No converter registered for type '{type.FullName}'.");
    }

    internal FormDataConverter ResolveConverter(Type type)
    {
        return _converters.GetOrAdd(type, CreateConverter, this);
    }
}
