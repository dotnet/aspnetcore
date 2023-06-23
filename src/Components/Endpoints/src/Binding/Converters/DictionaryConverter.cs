// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Runtime.CompilerServices;

namespace Microsoft.AspNetCore.Components.Endpoints.Binding;

internal abstract class DictionaryConverter<TDictionary> : FormDataConverter<TDictionary>
{
}

internal sealed class DictionaryConverter<TDictionary, TDictionaryPolicy, TBuffer, TKey, TValue> : DictionaryConverter<TDictionary>
    where TKey : ISpanParsable<TKey>
    where TDictionaryPolicy : IDictionaryBufferAdapter<TDictionary, TBuffer, TKey, TValue>
{
    private readonly FormDataConverter<TValue> _valueConverter;

    public DictionaryConverter(FormDataConverter<TValue> elementConverter)
    {
        ArgumentNullException.ThrowIfNull(elementConverter);

        _valueConverter = elementConverter;
    }

    internal override bool TryRead(
        ref FormDataReader context,
        Type type,
        FormDataMapperOptions options,
        [NotNullWhen(true)] out TDictionary? result,
        out bool found)
    {
        TValue currentValue;
        TBuffer buffer;
        bool foundCurrentValue;
        bool currentElementSuccess;
        bool succeded = true;

        var keys = context.GetKeys();
        found = keys.HasValues();
        if (!found)
        {
            result = default!;
            return true;
        }

        buffer = TDictionaryPolicy.CreateBuffer();

        // We can't precompute dictionary anyKeys ahead of time,
        // so the moment we find a dictionary, we request the list of anyKeys
        // for the current location, which will involve parsing the form data anyKeys
        // and building a tree of anyKeys.
        var keyCount = 0;
        var maxCollectionSize = options.MaxCollectionSize;

        foreach (var key in keys)
        {
            context.PushPrefix(key.Span);
            currentElementSuccess = _valueConverter.TryRead(ref context, typeof(TValue), options, out currentValue!, out foundCurrentValue);
            context.PopPrefix(key.Span);

            if (!TKey.TryParse(key[1..^1].Span, CultureInfo.InvariantCulture, out var keyValue))
            {
                succeded = false;
                context.AddMappingError(
                    FormattableStringFactory.Create(FormDataResources.DictionaryUnparsableKey, key[1..^1], typeof(TKey).FullName),
                    null);

                continue;
            }

            keyCount++;
            if (keyCount > maxCollectionSize)
            {
                context.AddMappingError(
                     FormattableStringFactory.Create(FormDataResources.MaxCollectionSizeReached, "dictionary", maxCollectionSize),
                     null);
                succeded = false;
                break;
            }

            TDictionaryPolicy.Add(ref buffer, keyValue!, currentValue);
        }

        result = TDictionaryPolicy.ToResult(buffer);
        return succeded;
    }
}
