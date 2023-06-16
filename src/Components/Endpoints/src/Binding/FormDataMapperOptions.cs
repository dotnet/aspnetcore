// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;

namespace Microsoft.AspNetCore.Components.Endpoints.Binding;

internal sealed class FormDataMapperOptions
{
    private readonly ConcurrentDictionary<Type, FormDataConverter> _converters = new();
    private readonly List<Func<Type, FormDataMapperOptions, FormDataConverter?>> _factories = new();

    [RequiresDynamicCode(FormBindingHelpers.RequiresDynamicCodeMessage)]
    [RequiresUnreferencedCode(FormBindingHelpers.RequiresUnreferencedCodeMessage)]
    public FormDataMapperOptions()
    {
        _converters = new(WellKnownConverters.Converters);

        _factories.Add((type, options) => ParsableConverterFactory.Instance.CanConvert(type, options) ? ParsableConverterFactory.Instance.CreateConverter(type, options) : null);
        _factories.Add((type, options) => NullableConverterFactory.Instance.CanConvert(type, options) ? NullableConverterFactory.Instance.CreateConverter(type, options) : null);
        _factories.Add((type, options) => DictionaryConverterFactory.Instance.CanConvert(type, options) ? DictionaryConverterFactory.Instance.CreateConverter(type, options) : null);
        _factories.Add((type, options) => CollectionConverterFactory.Instance.CanConvert(type, options) ? CollectionConverterFactory.Instance.CreateConverter(type, options) : null);
        _factories.Add((type, options) => ComplexTypeConverterFactory.Instance.CanConvert(type, options) ? ComplexTypeConverterFactory.Instance.CreateConverter(type, options) : null);
    }

    // Not configurable for now, this is the max number of elements we will bind. This is important for
    // security reasons, as we don't want to bind a huge collection and cause perf issues.
    // Some examples of this are:
    // Binding to collection using hashes, where the payload can be crafted to force the worst case on insertion
    // which is O(n).
    internal int MaxCollectionSize = 100;

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

    private static FormDataConverter CreateConverter(Type type, FormDataMapperOptions options)
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

    // For testing
    internal void AddConverter<T>(FormDataConverter<T> converter)
    {
        _converters[typeof(T)] = converter;
    }
}
