// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Internal;

namespace Microsoft.AspNetCore.Components.Endpoints.FormMapping;

internal class CollectionConverterFactory : IFormDataConverterFactory
{
    public static readonly CollectionConverterFactory Instance = new();

    [RequiresDynamicCode(FormMappingHelpers.RequiresDynamicCodeMessage)]
    [RequiresUnreferencedCode(FormMappingHelpers.RequiresUnreferencedCodeMessage)]
    public bool CanConvert(Type type, FormDataMapperOptions options)
    {
        var element = ResolveElementType(type);
        if (element == null)
        {
            return false;
        }

        if (Activator.CreateInstance(typeof(TypedCollectionConverterFactory<,>)
            .MakeGenericType(type, element!)) is not IFormDataConverterFactory factory)
        {
            return false;
        }

        return factory.CanConvert(type, options);
    }

    [RequiresDynamicCode(FormMappingHelpers.RequiresDynamicCodeMessage)]
    [RequiresUnreferencedCode(FormMappingHelpers.RequiresUnreferencedCodeMessage)]
    public static Type? ResolveElementType(Type type)
    {
        var enumerable = ClosedGenericMatcher.ExtractGenericInterface(type, typeof(IEnumerable<>));
        if (enumerable == null && !(type.IsArray && type.GetArrayRank() == 1))
        {
            return null;
        }

        return enumerable != null ? enumerable.GetGenericArguments()[0] : type.GetElementType()!;
    }

    [RequiresDynamicCode(FormMappingHelpers.RequiresDynamicCodeMessage)]
    [RequiresUnreferencedCode(FormMappingHelpers.RequiresUnreferencedCodeMessage)]
    public FormDataConverter CreateConverter(Type type, FormDataMapperOptions options)
    {
        ArgumentNullException.ThrowIfNull(type);
        ArgumentNullException.ThrowIfNull(options);

        // There is an assumption here that if the type is a bindable collection, it's going to implement
        // ICollection<T> and IEnumerable<T>. There could potentially be a type that implements ICollection<T>
        // multiple times for different T's explicitly, but that is not something we will support (nor something supported today).
        var enumerableType = ClosedGenericMatcher.ExtractGenericInterface(type, typeof(IEnumerable<>));
        var elementType = enumerableType?.GetGenericArguments()[0];

        // The collection converter heavily relies on generics to adapt to different collection types.
        // Since reflection gets a bit tricky with generics, we instead close over the generic collection and
        // element types to make it simpler to create the converter.
        var factory = Activator.CreateInstance(typeof(TypedCollectionConverterFactory<,>)
            .MakeGenericType(type, elementType!)) as IFormDataConverterFactory;

        if (factory == null)
        {
            throw new InvalidOperationException($"Unable to create converter for '{type.FullName}'.");
        }

        return factory.CreateConverter(type, options);
    }
}
