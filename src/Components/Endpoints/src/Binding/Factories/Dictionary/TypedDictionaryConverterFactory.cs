// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.Collections.ObjectModel;

namespace Microsoft.AspNetCore.Components.Endpoints.Binding;

internal class TypedDictionaryConverterFactory<TDictionaryType, TKey, TValue> : IFormDataConverterFactory
    where TKey : IParsable<TKey>
{
    public bool CanConvert(Type type, FormDataSerializerOptions options)
    {
        // Resolve the value type converter
        var valueTypeConverter = options.ResolveConverter<TValue>();
        if (valueTypeConverter == null)
        {
            return false;
        }

        var keyTypeConverter = options.ResolveConverter<TKey>();
        if (keyTypeConverter == null)
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

    public FormDataConverter CreateConverter(Type type, FormDataSerializerOptions options)
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

internal class DictionaryBufferAdapter<TDictionaryType, TKey, TValue>
    : IDictionaryBufferAdapter<TDictionaryType, TDictionaryType, TKey, TValue>
    where TDictionaryType : IDictionary<TKey, TValue>, new()
    where TKey : IParsable<TKey>
{
    public static TDictionaryType Add(ref TDictionaryType buffer, TKey key, TValue value)
    {
        buffer.Add(key, value);
        return buffer;
    }

    public static TDictionaryType CreateBuffer() => new TDictionaryType();

    public static TDictionaryType ToResult(TDictionaryType buffer) => buffer;
}

internal class ReadOnlyDictionaryBufferAdapter<TKey, TValue>
    : IDictionaryBufferAdapter<ReadOnlyDictionary<TKey, TValue>, Dictionary<TKey, TValue>, TKey, TValue>
    where TKey : IParsable<TKey>
{
    public static Dictionary<TKey, TValue> Add(ref Dictionary<TKey, TValue> buffer, TKey key, TValue value)
    {
        buffer.Add(key, value);
        return buffer;
    }

    public static Dictionary<TKey, TValue> CreateBuffer() =>
        new Dictionary<TKey, TValue>();

    public static ReadOnlyDictionary<TKey, TValue> ToResult(Dictionary<TKey, TValue> buffer) =>
        new ReadOnlyDictionary<TKey, TValue>(buffer);

    internal static DictionaryConverter<IReadOnlyDictionary<TKey, TValue>> CreateInterfaceConverter(FormDataConverter<TValue> valueTypeConverter)
    {
        return new DictionaryConverter<IReadOnlyDictionary<TKey, TValue>,
            DictionaryStaticCastAdapter<
                IReadOnlyDictionary<TKey, TValue>,
                ReadOnlyDictionary<TKey, TValue>,
                ReadOnlyDictionaryBufferAdapter<TKey, TValue>,
                Dictionary<TKey, TValue>,
                TKey,
                TValue>,
            Dictionary<TKey, TValue>,
            TKey,
            TValue>(valueTypeConverter);
    }

    internal static DictionaryConverter<ReadOnlyDictionary<TKey, TValue>> CreateConverter(FormDataConverter<TValue> valueTypeConverter)
    {
        return new DictionaryConverter<ReadOnlyDictionary<TKey, TValue>,
            ReadOnlyDictionaryBufferAdapter<TKey, TValue>,
            Dictionary<TKey, TValue>,
            TKey,
            TValue>(valueTypeConverter);
    }
}

internal class ImmutableDictionaryBufferAdapter<TKey, TValue>
    : IDictionaryBufferAdapter<ImmutableDictionary<TKey, TValue>, ImmutableDictionary<TKey, TValue>.Builder, TKey, TValue>
    where TKey : IParsable<TKey>
{
    public static ImmutableDictionary<TKey, TValue>.Builder Add(ref ImmutableDictionary<TKey, TValue>.Builder buffer, TKey key, TValue value)
    {
        buffer.Add(key, value);
        return buffer;
    }

    public static ImmutableDictionary<TKey, TValue>.Builder CreateBuffer() => ImmutableDictionary.CreateBuilder<TKey, TValue>();

    public static ImmutableDictionary<TKey, TValue> ToResult(ImmutableDictionary<TKey, TValue>.Builder buffer) => buffer.ToImmutable();

    internal static DictionaryConverter<IImmutableDictionary<TKey, TValue>> CreateInterfaceConverter(FormDataConverter<TValue> valueTypeConverter)
    {
        return new DictionaryConverter<IImmutableDictionary<TKey, TValue>,
            DictionaryStaticCastAdapter<
                IImmutableDictionary<TKey, TValue>,
                ImmutableDictionary<TKey, TValue>,
                ImmutableDictionaryBufferAdapter<TKey, TValue>,
                ImmutableDictionary<TKey, TValue>.Builder,
                TKey,
                TValue>,
            ImmutableDictionary<TKey, TValue>.Builder,
            TKey,
            TValue>(valueTypeConverter);
    }
}

internal class ImmutableSortedDictionaryBufferAdapter<TKey, TValue>
    : IDictionaryBufferAdapter<ImmutableSortedDictionary<TKey, TValue>, ImmutableSortedDictionary<TKey, TValue>.Builder, TKey, TValue>
    where TKey : IParsable<TKey>
{
    public static ImmutableSortedDictionary<TKey, TValue>.Builder Add(ref ImmutableSortedDictionary<TKey, TValue>.Builder buffer, TKey key, TValue value)
    {
        buffer.Add(key, value);
        return buffer;
    }

    public static ImmutableSortedDictionary<TKey, TValue>.Builder CreateBuffer() => ImmutableSortedDictionary.CreateBuilder<TKey, TValue>();

    public static ImmutableSortedDictionary<TKey, TValue> ToResult(ImmutableSortedDictionary<TKey, TValue>.Builder buffer) => buffer.ToImmutable();
}

internal class DictionaryStaticCastAdapter<TDictionaryInterface, TDictionaryImplementation, TDictionaryAdapter, TBuffer, TKey, TValue>
    : IDictionaryBufferAdapter<TDictionaryInterface, TBuffer, TKey, TValue>
    where TDictionaryAdapter : IDictionaryBufferAdapter<TDictionaryImplementation, TBuffer, TKey, TValue>
    where TDictionaryImplementation : TDictionaryInterface
    where TKey : IParsable<TKey>
{
    public static TBuffer CreateBuffer() => TDictionaryAdapter.CreateBuffer();

    public static TBuffer Add(ref TBuffer buffer, TKey key, TValue element) => TDictionaryAdapter.Add(ref buffer, key, element);

    public static TDictionaryInterface ToResult(TBuffer buffer) => TDictionaryAdapter.ToResult(buffer);
}
