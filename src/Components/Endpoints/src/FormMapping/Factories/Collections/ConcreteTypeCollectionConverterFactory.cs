// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;

namespace Microsoft.AspNetCore.Components.Endpoints.FormMapping;

internal class ConcreteTypeCollectionConverterFactory<TCollection, TElement>
    : IFormDataConverterFactory
{
    public static readonly ConcreteTypeCollectionConverterFactory<TCollection, TElement> Instance =
        new();

    [UnconditionalSuppressMessage("Trimming", "IL2046", Justification = "This derived implementation doesn't require unreferenced code like other implementations of the interface.")]
    [UnconditionalSuppressMessage("AOT", "IL3051", Justification = "This derived implementation doesn't use dynamic code like other implementations of the interface.")]
    public bool CanConvert(Type _, FormDataMapperOptions options) => true;

    [RequiresDynamicCode(FormMappingHelpers.RequiresDynamicCodeMessage)]
    [RequiresUnreferencedCode(FormMappingHelpers.RequiresUnreferencedCodeMessage)]
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
