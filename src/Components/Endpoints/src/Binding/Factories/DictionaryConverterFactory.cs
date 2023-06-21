// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Internal;

namespace Microsoft.AspNetCore.Components.Endpoints.Binding;

internal class DictionaryConverterFactory : IFormDataConverterFactory
{
    internal static readonly DictionaryConverterFactory Instance = new();

    [RequiresDynamicCode(FormBindingHelpers.RequiresDynamicCodeMessage)]
    [RequiresUnreferencedCode(FormBindingHelpers.RequiresUnreferencedCodeMessage)]
    public bool CanConvert(Type type, FormDataMapperOptions options)
    {
        // Type must implement IDictionary<TKey, TValue> IReadOnlyDictionary<TKey, TValue>
        // Note that IDictionary doesn't extend IReadOnlyDictionary, hence the need for two checks
        var dictionaryType = ClosedGenericMatcher.ExtractGenericInterface(type, typeof(IDictionary<,>)) ??
            ClosedGenericMatcher.ExtractGenericInterface(type, typeof(IReadOnlyDictionary<,>));

        if (dictionaryType == null)
        {
            return false;
        }

        // Key type must implement IParsable<T>
        var keyType = dictionaryType?.GetGenericArguments()[0];
        if (keyType == null)
        {
            return false;
        }

        var parsableKeyType = ClosedGenericMatcher.ExtractGenericInterface(keyType, typeof(IParsable<>));
        if (parsableKeyType == null)
        {
            return false;
        }

        // Value must have a converter
        var valueType = dictionaryType?.GetGenericArguments()[1];
        if (valueType == null)
        {
            return false;
        }

        var converter = options.ResolveConverter(valueType);
        if (converter == null)
        {
            return false;
        }

        var factory = Activator.CreateInstance(typeof(TypedDictionaryConverterFactory<,,>)
            .MakeGenericType(type, keyType, valueType)) as IFormDataConverterFactory;

        if (factory == null)
        {
            return false;
        }

        return factory.CanConvert(type, options);
    }

    [RequiresDynamicCode(FormBindingHelpers.RequiresDynamicCodeMessage)]
    [RequiresUnreferencedCode(FormBindingHelpers.RequiresUnreferencedCodeMessage)]
    public FormDataConverter CreateConverter(Type type, FormDataMapperOptions options)
    {
        // Type must implement IDictionary<TKey, TValue> IReadOnlyDictionary<TKey, TValue>
        // Note that IDictionary doesn't extend IReadOnlyDictionary, hence the need for two checks
        var dictionaryType = ClosedGenericMatcher.ExtractGenericInterface(type, typeof(IDictionary<,>)) ??
            ClosedGenericMatcher.ExtractGenericInterface(type, typeof(IReadOnlyDictionary<,>));
        if (dictionaryType == null)
        {
            throw new InvalidOperationException($"Unable to create converter for '{type.FullName}'.");
        }

        // Key type must implement IParsable<T>
        var keyType = dictionaryType?.GetGenericArguments()[0];
        if (keyType == null)
        {
            throw new InvalidOperationException($"Unable to create converter for '{type.FullName}'.");
        }

        var parsableKeyType = ClosedGenericMatcher.ExtractGenericInterface(keyType, typeof(IParsable<>));
        if (parsableKeyType == null)
        {
            throw new InvalidOperationException($"Unable to create converter for '{type.FullName}'.");
        }

        // Value must have a converter
        var valueType = dictionaryType?.GetGenericArguments()[1];
        if (valueType == null)
        {
            throw new InvalidOperationException($"Unable to create converter for '{type.FullName}'.");
        }

        var converter = options.ResolveConverter(valueType);
        if (converter == null)
        {
            throw new InvalidOperationException($"Unable to create converter for '{type.FullName}'.");
        }

        var factory = Activator.CreateInstance(typeof(TypedDictionaryConverterFactory<,,>)
            .MakeGenericType(type, keyType, valueType)) as IFormDataConverterFactory;

        if (factory == null)
        {
            throw new InvalidOperationException($"Unable to create converter for '{type.FullName}'.");
        }

        return factory.CreateConverter(type, options);
    }
}
