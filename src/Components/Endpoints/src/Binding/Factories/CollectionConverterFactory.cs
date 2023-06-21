// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Internal;

namespace Microsoft.AspNetCore.Components.Endpoints.Binding;

internal class CollectionConverterFactory : IFormDataConverterFactory
{
    public static readonly CollectionConverterFactory Instance = new();

    [RequiresDynamicCode(FormBindingHelpers.RequiresDynamicCodeMessage)]
    [RequiresUnreferencedCode(FormBindingHelpers.RequiresUnreferencedCodeMessage)]
    public bool CanConvert(Type type, FormDataMapperOptions options)
    {
        var enumerable = ClosedGenericMatcher.ExtractGenericInterface(type, typeof(IEnumerable<>));
        if (enumerable == null && !(type.IsArray && type.GetArrayRank() == 1))
        {
            return false;
        }

        var element = enumerable != null ? enumerable.GetGenericArguments()[0] : type.GetElementType()!;

        if (Activator.CreateInstance(typeof(TypedCollectionConverterFactory<,>)
            .MakeGenericType(type, element!)) is not IFormDataConverterFactory factory)
        {
            return false;
        }

        return factory.CanConvert(type, options);
    }

    [RequiresDynamicCode(FormBindingHelpers.RequiresDynamicCodeMessage)]
    [RequiresUnreferencedCode(FormBindingHelpers.RequiresUnreferencedCodeMessage)]
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
