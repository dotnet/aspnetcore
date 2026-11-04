// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;

namespace Microsoft.AspNetCore.Components.Endpoints.FormMapping;

internal sealed class ConcreteTypeDictionaryConverterFactory<TDictionary, TKey, TValue> : IFormDataConverterFactory
    where TKey : ISpanParsable<TKey>
{
    public static readonly ConcreteTypeDictionaryConverterFactory<TDictionary, TKey, TValue> Instance = new();

    [UnconditionalSuppressMessage("Trimming", "IL2046", Justification = "This derived implementation doesn't require unreferenced code like other implementations of the interface.")]
    [UnconditionalSuppressMessage("AOT", "IL3051", Justification = "This derived implementation doesn't use dynamic code like other implementations of the interface.")]
    public bool CanConvert(Type type, FormDataMapperOptions options) => true;

    [RequiresDynamicCode(FormMappingHelpers.RequiresDynamicCodeMessage)]
    [RequiresUnreferencedCode(FormMappingHelpers.RequiresUnreferencedCodeMessage)]
    public FormDataConverter CreateConverter(Type type, FormDataMapperOptions options)
    {
        // Resolve the element type converter
        var keyConverter = options.ResolveConverter<TKey>() ??
            throw new InvalidOperationException($"Unable to create converter for '{typeof(TDictionary).FullName}'.");

        var valueConverter = options.ResolveConverter<TValue>() ??
            throw new InvalidOperationException($"Unable to create converter for '{typeof(TDictionary).FullName}'.");

        var customFactory = Activator.CreateInstance(typeof(CustomDictionaryConverterFactory<>)
            .MakeGenericType(typeof(TDictionary), typeof(TKey), typeof(TValue), typeof(TDictionary))) as CustomDictionaryConverterFactory;

        if (customFactory == null)
        {
            throw new InvalidOperationException($"Unable to create converter for type '{typeof(TDictionary).FullName}'.");
        }

        return customFactory.CreateConverter(keyConverter, valueConverter);
    }

    private abstract class CustomDictionaryConverterFactory
    {
        public abstract FormDataConverter CreateConverter(FormDataConverter<TKey> keyConverter, FormDataConverter<TValue> valueConverter);
    }

    private class CustomDictionaryConverterFactory<TCustomDictionary> : CustomDictionaryConverterFactory
        where TCustomDictionary : TDictionary, IDictionary<TKey, TValue>, new()
    {
        public override FormDataConverter CreateConverter(FormDataConverter<TKey> keyConverter, FormDataConverter<TValue> valueConverter)
        {
            return new DictionaryConverter<
                TCustomDictionary,
                DictionaryBufferAdapter<TCustomDictionary, TKey, TValue>,
                TCustomDictionary,
                TKey,
                TValue>(valueConverter);
        }
    }
}
