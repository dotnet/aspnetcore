// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Components.Endpoints.Binding;

internal class ConcreteTypeCollectionConverterFactory<TCollection, TElement>
    : IFormDataConverterFactory
{
    public static readonly ConcreteTypeCollectionConverterFactory<TCollection, TElement> Instance =
        new();

    public bool CanConvert(Type _, FormDataMapperOptions options) => true;

    public FormDataConverter CreateConverter(Type _, FormDataMapperOptions options)
    {
        // Resolve the element type converter
        var elementTypeConverter = options.ResolveConverter<TElement>() ??
            throw new InvalidOperationException($"Unable to create converter for '{typeof(TCollection).FullName}'.");

        var customFactory = Activator.CreateInstance(typeof(CustomCollectionConverterFactory<>)
            .MakeGenericType(typeof(TCollection), typeof(TElement), typeof(TCollection))) as CustomCollectionConverterFactory;

        if (customFactory == null)
        {
            throw new InvalidOperationException($"Unable to create converter for type '{typeof(TCollection).FullName}'.");
        }

        return customFactory.CreateConverter(elementTypeConverter);
    }

    private abstract class CustomCollectionConverterFactory
    {
        public abstract FormDataConverter CreateConverter(FormDataConverter<TElement> elementConverter);
    }

    private class CustomCollectionConverterFactory<TCustomCollection> : CustomCollectionConverterFactory
        where TCustomCollection : TCollection, ICollection<TElement>, new()
    {
        public override FormDataConverter CreateConverter(FormDataConverter<TElement> elementConverter)
        {
            return new CollectionConverter<
            TCustomCollection,
            ImplementingCollectionBufferAdapter<TCustomCollection, TCustomCollection, TElement>,
            TCustomCollection,
            TElement>(elementConverter);
        }
    }
}
