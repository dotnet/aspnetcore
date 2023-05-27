// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using Microsoft.Extensions.Internal;

namespace Microsoft.AspNetCore.Components.Endpoints.Binding;

internal class CollectionConverterFactory : IFormDataConverterFactory
{
    private static readonly MethodInfo CreateArrayFactoryFunctionMethod =
        typeof(CollectionConverterFactory).GetMethod(nameof(CreateArrayFactoryFunction), BindingFlags.Static | BindingFlags.NonPublic) ??
        throw new InvalidOperationException("Can't find 'CreateArrayFactoryFunction' method.");

    private static T[] CreateArrayFactoryFunction<T>(ReadOnlyMemory<T> buffer)
    {
        var result = new T[buffer.Length];
        buffer.CopyTo(result);
        return result;
    }

    public static bool CanConvert(Type type, FormDataSerializerOptions options)
    {
        var enumerable = ClosedGenericMatcher.ExtractGenericInterface(type, typeof(IEnumerable<>));
        if (enumerable == null && !type.IsArray && type.GetArrayRank() != 1)
        {
            return false;
        }

        var element = enumerable != null ? enumerable.GetGenericArguments()[0] : type.GetElementType()!;

        return options.HasConverter(element);
    }

    public static FormDataConverter CreateConverter(Type type, FormDataSerializerOptions options)
    {
        ArgumentNullException.ThrowIfNull(type);
        ArgumentNullException.ThrowIfNull(options);

        if (!type.IsAbstract && !type.IsInterface && !type.IsTypeDefinition)
        {
            // This is a concrete converterType, so we can create an instance of it directly. The only requirement for this
            // factory is that the converterType implements ICollection, so we can add items to it.
            // This is the case for types like List<T>, HashSet<T>, etc.
            var result = TryCreateConverter(
                options,
                converterType: type,
                converterInterface: typeof(ICollection<>),
                implementationType: type,
                concrete: true);

            if (result == null)
            {
                throw new InvalidOperationException($"Unable to create converter for '{type.FullName}'.");
            }

            return (FormDataConverter)result;
        }

        if (type.IsInterface)
        {
            // At this point we are dealing with an interface. We test from the most specific to the least specific
            // to find the best fit for the well-known set of interfaces we support.
            var result = TryCreateConverter(options, type, typeof(ISet<>), typeof(HashSet<>), concrete: false) ??
                TryCreateConverter(options, type, typeof(IReadOnlySet<>), typeof(HashSet<>), concrete: false) ??
                TryCreateConverter(options, type, typeof(IList<>), typeof(List<>), concrete: false) ??
                TryCreateConverter(options, type, typeof(IReadOnlyList<>), typeof(List<>), concrete: false) ??
                TryCreateConverter(options, type, typeof(ICollection<>), typeof(List<>), concrete: false) ??
                TryCreateConverter(options, type, typeof(IReadOnlyCollection<>), typeof(List<>), concrete: false) ??
                TryCreateConverter(options, type, typeof(IEnumerable<>), typeof(List<>), concrete: false);
            if (result == null)
            {
                throw new InvalidOperationException($"Unable to create converter for '{type.FullName}'.");
            }
        }

        if (type.IsArray && type.GetArrayRank() == 1)
        {
            var elementType = type.GetElementType()!;
            var elementTypeConverter = options.ResolveConverter(elementType);
            if (elementTypeConverter == null)
            {
                throw new InvalidOperationException($"Unable to create converter for '{type.FullName}'.");
            }

            var result = Activator.CreateInstance(
                // BufferedCollectionConverter<T[], T[], TElement>
                typeof(BufferedCollectionConverter<,,>)
                    .MakeGenericType(type, type, type.GetElementType()!),
                elementTypeConverter,
                CreateArrayFactoryFunctionMethod
                    .MakeGenericMethod(elementType)
                    .CreateDelegate(
                        // Func<ReadOnlyMemory<T>, T[]>
                        typeof(Func<,>)
                            .MakeGenericType(
                                typeof(ReadOnlyMemory<>).MakeGenericType(elementType),
                                elementType.MakeArrayType())));

            if (result == null)
            {
                throw new InvalidOperationException($"Unable to create converter for '{type.FullName}'.");
            }

            return (FormDataConverter)result;
        }

        throw new InvalidOperationException($"Unable to create converter for '{type.FullName}'.");

        static object? TryCreateConverter(
            FormDataSerializerOptions options,
            Type converterType,
            Type converterInterface,
            Type implementationType,
            bool concrete)
        {
            var collectionInterface = ClosedGenericMatcher.ExtractGenericInterface(converterType, converterInterface);
            if (collectionInterface == null || (concrete && collectionInterface != converterType))
            {
                // This converterType doesn't implement the interface in converterInterface, or it does, but it's not an exact match.
                // When we test for interfaces we do so from most specific to least specific. For example, we test for IReadOnlyList<T>
                // before we test for IList<T>.
                // We don't support converting random interfaces because we can't cast to them, as we don't know what implementation type
                // to use.
                return null;
            }
            var elementType = collectionInterface.GetGenericArguments()[0];
            var elementTypeConverter = options.ResolveConverter(elementType);
            if (elementTypeConverter == null)
            {
                throw new InvalidOperationException($"Unable to create converter for '{converterType.FullName}'.");
            }

            // Since this is a concrete converterType, like List<T>, the implementation converterType and the final converterType are the same.
            var result = Activator.CreateInstance(
                typeof(UnbufferedCollectionConverter<,,>).MakeGenericType(
                    converterType,
                    // If we are dealing with a concrete converterType, we use that.
                    // Otherwise, we are trying to bind to an interface (like IList<T>, IReadOnlyList<T>, etc).
                    // In that case, we need to use the effective converterType implementation, which is the concrete converterType
                    // that the caller has chosen to use.
                    concrete ? converterType : implementationType.MakeGenericType(elementType),
                    elementType),
                elementTypeConverter);

            if (result == null)
            {
                throw new InvalidOperationException($"Unable to create converter for '{converterType.FullName}'.");
            }

            return result;
        }
    }
}

// This converter is for types like IList<T>, etc, where the collection can be created directly, the elements added to it
// and the final collection returned as the result. As opposed to other things like arrays, ImmutableCollections, etc, where
// we need to buffer the data first and then create the final collection for performance reasons.
// In essence, any interface that can be coerced into a list, we are going to use this converter.
internal class UnbufferedCollectionConverter<TCollection, TImplementation, TElement> : FormDataConverter<TCollection>
    where TCollection : IEnumerable<TElement?>
    where TImplementation : TCollection, ICollection<TElement?>, new()
{
    // Indexes up to 1000 are pre-allocated to avoid allocations for common cases.
    private static readonly string[] Indexes = new string[] {
        "[0]", "[1]", "[2]", "[3]", "[4]", "[5]", "[6]", "[7]", "[8]", "[9]",
        "[10]", "[11]", "[12]", "[13]", "[14]", "[15]", "[16]", "[17]", "[18]", "[19]",
        "[20]", "[21]", "[22]", "[23]", "[24]", "[25]", "[26]", "[27]", "[28]", "[29]",
        "[30]", "[31]", "[32]", "[33]", "[34]", "[35]", "[36]", "[37]", "[38]", "[39]",
        "[40]", "[41]", "[42]", "[43]", "[44]", "[45]", "[46]", "[47]", "[48]", "[49]",
        "[50]", "[51]", "[52]", "[53]", "[54]", "[55]", "[56]", "[57]", "[58]", "[59]",
        "[60]", "[61]", "[62]", "[63]", "[64]", "[65]", "[66]", "[67]", "[68]", "[69]",
        "[70]", "[71]", "[72]", "[73]", "[74]", "[75]", "[76]", "[77]", "[78]", "[79]",
        "[80]", "[81]", "[82]", "[83]", "[84]", "[85]", "[86]", "[87]", "[88]", "[89]",
        "[90]", "[91]", "[92]", "[93]", "[94]", "[95]", "[96]", "[97]", "[98]", "[99]",
    };
    private readonly FormDataConverter<TElement?> _elementConverter;

    public UnbufferedCollectionConverter(FormDataConverter<TElement?> elementConverter)
    {
        ArgumentNullException.ThrowIfNull(elementConverter);
        _elementConverter = elementConverter;
    }

    internal override bool TryRead(
        ref FormDataReader context,
        Type type,
        FormDataSerializerOptions options,
        [NotNullWhen(true)] out TCollection? result,
        out bool found)
    {
        TElement? currentElement;
        TImplementation collectionImplementation;
        bool foundCurrentElement;
        bool currentElementSuccess;
        bool succeded;
        // Even though we have indexes, we special case 0 an 1 and use literals directly. We leave them in the indexes
        // collection because it makes other indexes align.
        context.PushPrefix("[0]");
        succeded = _elementConverter.TryRead(ref context, typeof(TElement), options, out currentElement, out found);
        context.PopPrefix("[0]");
        if (!found)
        {
            result = default;
            return succeded;
        }

        // We already know we found an element;
        found = true;
        // At this point we have at least found one element, we can create the collection and assign it to it.
        collectionImplementation = new TImplementation();
        collectionImplementation.Add(currentElement);

        // Read element 1 and set conditions to enter the loop
        context.PushPrefix("[1]");
        currentElementSuccess = _elementConverter.TryRead(ref context, typeof(TElement), options, out currentElement, out foundCurrentElement);
        succeded = succeded && currentElementSuccess;
        context.PopPrefix("[1]");
        // We need to iterate while we keep finding values, even if some of them have errors. This is because we don't want data to be lost just
        // because we are not able to parse it. For example, if [5] = "asdf", we don't want to loose the values for [6], [7], etc. that can be
        // valid.
        // There will be a limit to how many errors we collect, at which point we will stop capturing errors.
        // Similarly, over 100 elements, we'll start doing more work to compute the index prefix. We chose 100 because that's the default
        // max collection size that we will support.
        var index = 2;
        for (; index < 100 && foundCurrentElement; index++)
        {
            // Add the current element
            collectionImplementation.Add(currentElement);

            // Get the precomputed prefix and try and bind the element.
            var prefix = Indexes[index];
            context.PushPrefix(prefix);
            currentElementSuccess = _elementConverter.TryRead(ref context, typeof(TElement), options, out currentElement!, out foundCurrentElement);
            succeded = succeded && currentElementSuccess;
            context.PopPrefix(prefix);
        }

        if (!foundCurrentElement)
        {
            result = collectionImplementation;
            return succeded;
        }

        // We have a "large" collection. Loop until we stop finding elements or reach the max collection size.
        var maxCollectionSize = options.MaxCollectionSize;
        // index is 100 here.
        for (; index < maxCollectionSize && foundCurrentElement; index++)
        {
            // Add the current element
            collectionImplementation.Add(currentElement);

            // Get the precomputed prefix and try and bind the element.
            var prefix = Indexes[index];
            context.PushPrefix(prefix);
            currentElementSuccess = _elementConverter.TryRead(ref context, typeof(TElement), options, out currentElement!, out foundCurrentElement);
            succeded = succeded && currentElementSuccess;
            context.PopPrefix(prefix);
        }

        result = collectionImplementation;

        if (!foundCurrentElement)
        {
            return succeded;
        }
        else
        {
            // We reached the max collection size.
            collectionImplementation.Add(currentElement);

            // Signal failure because we have stopped binding.
            // We will include an error in the list of reported errors.
            return false;
        }
    }
}

// This converter is for types like ImmutableArray, ImmutableList, etc, where the collection needs to be created in one shot.
// The strategy here is to collect all the elements in a buffer (probably a pooled array), bind everything, and then do a final
// copy.
internal class BufferedCollectionConverter<TCollection, TImplementation, TElement> : FormDataConverter<TCollection>
    where TCollection : IEnumerable<TElement?>
    where TImplementation : TCollection
{
    // Indexes up to 1000 are pre-allocated to avoid allocations for common cases.
    private static readonly string[] Indexes = new string[] {
        "[0]", "[1]", "[2]", "[3]", "[4]", "[5]", "[6]", "[7]", "[8]", "[9]",
        "[10]", "[11]", "[12]", "[13]", "[14]", "[15]", "[16]", "[17]", "[18]", "[19]",
        "[20]", "[21]", "[22]", "[23]", "[24]", "[25]", "[26]", "[27]", "[28]", "[29]",
        "[30]", "[31]", "[32]", "[33]", "[34]", "[35]", "[36]", "[37]", "[38]", "[39]",
        "[40]", "[41]", "[42]", "[43]", "[44]", "[45]", "[46]", "[47]", "[48]", "[49]",
        "[50]", "[51]", "[52]", "[53]", "[54]", "[55]", "[56]", "[57]", "[58]", "[59]",
        "[60]", "[61]", "[62]", "[63]", "[64]", "[65]", "[66]", "[67]", "[68]", "[69]",
        "[70]", "[71]", "[72]", "[73]", "[74]", "[75]", "[76]", "[77]", "[78]", "[79]",
        "[80]", "[81]", "[82]", "[83]", "[84]", "[85]", "[86]", "[87]", "[88]", "[89]",
        "[90]", "[91]", "[92]", "[93]", "[94]", "[95]", "[96]", "[97]", "[98]", "[99]",
    };
    private readonly FormDataConverter<TElement?> _elementConverter;
    private readonly Func<ReadOnlyMemory<TElement?>, TCollection> _resultFactory;

    public BufferedCollectionConverter(
        FormDataConverter<TElement?> elementConverter,
        Func<ReadOnlyMemory<TElement?>, TCollection> resultFactory)
    {
        ArgumentNullException.ThrowIfNull(elementConverter);
        _elementConverter = elementConverter;
        _resultFactory = resultFactory;
    }

    internal override bool TryRead(
        ref FormDataReader context,
        Type type,
        FormDataSerializerOptions options,
        [NotNullWhen(true)] out TCollection? result,
        out bool found)
    {
        // 16 is the minimum array size the pool returns.
        // For unmanaged types we could choose to start with a stackalloc instead of renting a buffer.
        var buffer = ArrayPool<TElement?>.Shared.Rent(16);
        TElement? currentElement;
        bool foundCurrentElement;
        bool currentElementSuccess;
        bool succeded;
        // Even though we have indexes, we special case 0 an 1 and use literals directly. We leave them in the indexes
        // collection because it makes other indexes align.
        context.PushPrefix("[0]");
        succeded = _elementConverter.TryRead(ref context, typeof(TElement), options, out currentElement, out found);
        context.PopPrefix("[0]");
        if (!found)
        {
            result = default;
            return succeded;
        }

        // We already know we found an element;
        found = true;
        // At this point we have at least found one element, we can create the collection and assign it to it.
        buffer[0] = currentElement;

        // Read element 1 and set conditions to enter the loop
        context.PushPrefix("[1]");
        currentElementSuccess = _elementConverter.TryRead(ref context, typeof(TElement), options, out currentElement, out foundCurrentElement);
        succeded = succeded && currentElementSuccess;
        context.PopPrefix("[1]");
        // We need to iterate while we keep finding values, even if some of them have errors. This is because we don't want data to be lost just
        // because we are not able to parse it. For example, if [5] = "asdf", we don't want to loose the values for [6], [7], etc. that can be
        // valid.
        // There will be a limit to how many errors we collect, at which point we will stop capturing errors.
        // Similarly, over 100 elements, we'll start doing more work to compute the index prefix. We chose 100 because that's the default
        // max collection size that we will support.
        var index = 2;
        for (; index < 100 && foundCurrentElement; index++)
        {
            if (index > buffer.Length)
            {
                // Expand the buffer if we ran out of capacity and we need to insert an additional
                // element. We duplicate the buffer size by default, which means 32, 64, 128.
                // With the default collection size, we copy the buffer three times at most 16->32, 32->64, 64->128
                // in addition to the copy to produce the final result.
                // This is a trade-off between requiring larger arrays from the pool by default and reducing the amount
                // of copies.
                // In general, we don't expect collections to be large, since they come from form data, so in most cases
                // we'll only do one copy for the final result.
                // Is also notable the fact that the pool might return arrays larger than the requested size, so we might
                // not need to copy the buffer at all.
                var newBuffer = ArrayPool<TElement?>.Shared.Rent(buffer.Length * 2);
                buffer.CopyTo(newBuffer, 0);
                ArrayPool<TElement?>.Shared.Return(buffer);
                buffer = newBuffer;
            }
            // Add the current element
            buffer[index - 1] = currentElement;
            // Get the precomputed prefix and try and bind the element.
            var prefix = Indexes[index];
            context.PushPrefix(prefix);
            currentElementSuccess = _elementConverter.TryRead(ref context, typeof(TElement), options, out currentElement!, out foundCurrentElement);
            succeded = succeded && currentElementSuccess;
            context.PopPrefix(prefix);
        }

        if (!foundCurrentElement)
        {
            result = _resultFactory(buffer.AsMemory(0, index - 1));
            return succeded;
        }

        // We have a "large" collection. Loop until we stop finding elements or reach the max collection size.
        var maxCollectionSize = options.MaxCollectionSize;
        // index is 100 here.
        for (; index < maxCollectionSize && foundCurrentElement; index++)
        {
            if (index > buffer.Length)
            {
                // Same comment with regards to the buffer as above.
                var newBuffer = ArrayPool<TElement?>.Shared.Rent(buffer.Length * 2);
                buffer.CopyTo(newBuffer, 0);
                ArrayPool<TElement?>.Shared.Return(buffer);
                buffer = newBuffer;
            }
            // Add the current element
            buffer[index - 1] = currentElement;

            // Get the precomputed prefix and try and bind the element.
            var prefix = Indexes[index];
            context.PushPrefix(prefix);
            currentElementSuccess = _elementConverter.TryRead(ref context, typeof(TElement), options, out currentElement!, out foundCurrentElement);
            succeded = succeded && currentElementSuccess;
            context.PopPrefix(prefix);
        }

        if (!foundCurrentElement)
        {
            result = _resultFactory(buffer.AsMemory(0, index - 1));
            return succeded;
        }
        else
        {
            if (index > buffer.Length)
            {
                // Same comment with regards to the buffer as above.
                var newBuffer = ArrayPool<TElement?>.Shared.Rent(buffer.Length * 2);
                buffer.CopyTo(newBuffer, 0);
                ArrayPool<TElement?>.Shared.Return(buffer);
                buffer = newBuffer;
            }
            // Add the current element
            buffer[maxCollectionSize - 1] = currentElement;

            result = _resultFactory(buffer.AsMemory(0, maxCollectionSize));
            // Signal failure because we have stopped binding.
            // We will include an error in the list of reported errors.
            return false;
        }
    }
}
