// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;

namespace Microsoft.AspNetCore.Components.Endpoints.FormMapping;

internal sealed class TypedDictionaryConverterFactory<TDictionaryType, TKey, TValue> : IFormDataConverterFactory
    where TKey : ISpanParsable<TKey>
{
    [RequiresDynamicCode(FormMappingHelpers.RequiresDynamicCodeMessage)]
    [RequiresUnreferencedCode(FormMappingHelpers.RequiresUnreferencedCodeMessage)]
    public bool CanConvert(Type type, FormDataMapperOptions options)
    {
        // Resolve the value type converter
        if (!options.CanConvert(typeof(TValue)))
        {
            return false;
        }

        if (type.IsInterface)
        {
            // At this point we are dealing with an interface. We test from the most specific to the least specific
            // to find the best fit for the well-known set of interfaces we support.
            return type switch
            {
                // System.Collections.Immutable
                var _ when type == (typeof(IImmutableDictionary<TKey, TValue>)) => true,

                // System.Collections.Generics
                var _ when type == (typeof(IReadOnlyDictionary<TKey, TValue>)) => true,
                var _ when type == (typeof(IDictionary<TKey, TValue>)) => true,

                _ => throw new InvalidOperationException($"Unable to create converter for '{type.FullName}'."),
            };
        }

        if (!type.IsAbstract && !type.IsGenericTypeDefinition)
        {
            return type switch
            {
                // Immutable collections
                var _ when type == (typeof(ImmutableDictionary<TKey, TValue>)) => true,
                var _ when type == (typeof(ImmutableSortedDictionary<TKey, TValue>)) => true,

                // Concurrent collections
                var _ when type == (typeof(ConcurrentDictionary<TKey, TValue>)) => true,

                // Generic collections
                var _ when type == (typeof(SortedList<TKey, TValue>)) => true,
                var _ when type == (typeof(SortedDictionary<TKey, TValue>)) => true,
                var _ when type == (typeof(Dictionary<TKey, TValue>)) => true,

                var _ when type == (typeof(ReadOnlyDictionary<TKey, TValue>)) => true,

                // Some of the types above implement IDictionary<TKey, TValue>, but do so in a very inneficient way, so we want to
                // use special converters for them.
                var _ when type.IsAssignableTo(typeof(IDictionary<TKey, TValue>)) && type.GetConstructor(Type.EmptyTypes) != null => true,
                _ => false
            };
        }

        return false;
    }

    [RequiresDynamicCode(FormMappingHelpers.RequiresDynamicCodeMessage)]
    [RequiresUnreferencedCode(FormMappingHelpers.RequiresUnreferencedCodeMessage)]
    public FormDataConverter CreateConverter(Type type, FormDataMapperOptions options)
    {
        // Resolve the value type converter
        var valueTypeConverter = options.ResolveConverter<TValue>();
        if (valueTypeConverter == null)
        {
            throw new InvalidOperationException($"Unable to create converter for '{type.FullName}'.");
        }

        if (type.IsInterface)
        {
            // At this point we are dealing with an interface. We test from the most specific to the least specific
            // to find the best fit for the well-known set of interfaces we support.
            return type switch
            {
                // System.Collections.Immutable
                var _ when type == (typeof(IImmutableDictionary<TKey, TValue>)) =>
                    ImmutableDictionaryBufferAdapter<TKey, TValue>.CreateInterfaceConverter(valueTypeConverter),
                // System.Collections.Generics
                var _ when type == (typeof(IReadOnlyDictionary<TKey, TValue>)) =>
                    ReadOnlyDictionaryBufferAdapter<TKey, TValue>.CreateInterfaceConverter(valueTypeConverter),
                var _ when type == (typeof(IDictionary<TKey, TValue>)) =>
                    new DictionaryConverter<IDictionary<TKey, TValue>,
                        DictionaryStaticCastAdapter<
                            IDictionary<TKey, TValue>,
                            Dictionary<TKey, TValue>,
                            DictionaryBufferAdapter<Dictionary<TKey, TValue>, TKey, TValue>,
                            Dictionary<TKey, TValue>,
                            TKey,
                            TValue>,
                        Dictionary<TKey, TValue>, TKey, TValue>(valueTypeConverter),

                _ => throw new InvalidOperationException($"Unable to create converter for '{type.FullName}'."),
            };
        }

        if (!type.IsAbstract && !type.IsGenericTypeDefinition)
        {
            return type switch
            {
                var _ when type == (typeof(ReadOnlyDictionary<TKey, TValue>)) =>
                    ReadOnlyDictionaryBufferAdapter<TKey, TValue>.CreateConverter(valueTypeConverter),
                // Immutable collections
                var _ when type == (typeof(ImmutableDictionary<TKey, TValue>)) =>
                    new DictionaryConverter<
                        ImmutableDictionary<TKey, TValue>,
                        ImmutableDictionaryBufferAdapter<TKey, TValue>,
                        ImmutableDictionary<TKey, TValue>.Builder,
                        TKey,
                        TValue>(valueTypeConverter),
                var _ when type == (typeof(ImmutableSortedDictionary<TKey, TValue>)) =>
                    new DictionaryConverter<
                        ImmutableSortedDictionary<TKey, TValue>,
                        ImmutableSortedDictionaryBufferAdapter<TKey, TValue>,
                        ImmutableSortedDictionary<TKey, TValue>.Builder,
                        TKey,
                        TValue>(valueTypeConverter),

                // Concurrent collections
                var _ when type == (typeof(ConcurrentDictionary<TKey, TValue>)) =>
                    ConcreteTypeDictionaryConverterFactory<ConcurrentDictionary<TKey, TValue>, TKey, TValue>.Instance.CreateConverter(type, options),

                // Generic collections
                var _ when type == (typeof(SortedList<TKey, TValue>)) =>
                    ConcreteTypeDictionaryConverterFactory<SortedList<TKey, TValue>, TKey, TValue>.Instance.CreateConverter(type, options),
                var _ when type == (typeof(SortedDictionary<TKey, TValue>)) =>
                    ConcreteTypeDictionaryConverterFactory<SortedDictionary<TKey, TValue>, TKey, TValue>.Instance.CreateConverter(type, options),
                var _ when type == (typeof(Dictionary<TKey, TValue>)) =>
                    ConcreteTypeDictionaryConverterFactory<Dictionary<TKey, TValue>, TKey, TValue>.Instance.CreateConverter(type, options),

                // Some of the types above implement IDictionary<TKey, TValue>, but do so in a very inneficient way, so we want to
                // use special converters for them.
                var _ when type.IsAssignableTo(typeof(IDictionary<TKey, TValue>)) && type.GetConstructor(Type.EmptyTypes) != null =>
                    ConcreteTypeDictionaryConverterFactory<TDictionaryType, TKey, TValue>.Instance.CreateConverter(type, options),
                _ => throw new InvalidOperationException($"Unable to create converter for '{type.FullName}'."),
            };
        }

        throw new InvalidOperationException($"Unable to create converter for '{type.FullName}'.");
    }
}
