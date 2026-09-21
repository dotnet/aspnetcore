// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;

namespace Microsoft.AspNetCore.Components.Endpoints.FormMapping;

internal abstract class TypedCollectionConverterFactory : IFormDataConverterFactory
{
    [RequiresDynamicCode(FormMappingHelpers.RequiresDynamicCodeMessage)]
    [RequiresUnreferencedCode(FormMappingHelpers.RequiresUnreferencedCodeMessage)]
    public abstract bool CanConvert(Type type, FormDataMapperOptions options);

    [RequiresDynamicCode(FormMappingHelpers.RequiresDynamicCodeMessage)]
    [RequiresUnreferencedCode(FormMappingHelpers.RequiresUnreferencedCodeMessage)]
    public abstract FormDataConverter CreateConverter(Type type, FormDataMapperOptions options);
}

internal sealed class TypedCollectionConverterFactory<TCollection, TElement> : TypedCollectionConverterFactory
{
    [RequiresDynamicCode(FormMappingHelpers.RequiresDynamicCodeMessage)]
    [RequiresUnreferencedCode(FormMappingHelpers.RequiresUnreferencedCodeMessage)]
    public override bool CanConvert(Type _, FormDataMapperOptions options)
    {
        // Resolve the element type converter
        if (!options.CanConvert(typeof(TElement)))
        {
            return false;
        }

        // Arrays
        var type = typeof(TCollection);
        if (type.IsArray && type.GetArrayRank() == 1)
        {
            return true;
        }

        if (!type.IsInterface && !type.IsAbstract && !type.IsGenericTypeDefinition)
        {
            return type switch
            {
                // Special collections
                var _ when type == (typeof(Queue<TElement>)) => true,
                var _ when type == (typeof(Stack<TElement>)) => true,
                var _ when type == (typeof(ReadOnlyCollection<TElement>)) => true,

                // Concurrent collections
                var _ when type == (typeof(ConcurrentBag<TElement>)) => true,
                var _ when type == (typeof(ConcurrentStack<TElement>)) => true,
                var _ when type == (typeof(ConcurrentQueue<TElement>)) => true,

                // Immutable collections
                var _ when type == (typeof(ImmutableArray<TElement>)) => true,
                var _ when type == (typeof(ImmutableList<TElement>)) => true,
                var _ when type == (typeof(ImmutableHashSet<TElement>)) => true,
                var _ when type == (typeof(ImmutableSortedSet<TElement>)) => true,
                var _ when type == (typeof(ImmutableQueue<TElement>)) => true,
                var _ when type == (typeof(ImmutableStack<TElement>)) => true,

                // Some of the types above implement ICollection<T>, but do so in a very inneficient way, so we want to
                // use special converters for them.
                var _ when type.IsAssignableTo(typeof(ICollection<TElement>)) && type.GetConstructor(Type.EmptyTypes) != null => true,
                _ => false
            };
        }

        if (type.IsInterface)
        {
            // At this point we are dealing with an interface. We test from the most specific to the least specific
            // to find the best fit for the well-known set of interfaces we support.
            return type switch
            {
                // System.Collections.Immutable
                var _ when type == (typeof(IImmutableSet<TElement>)) => true,
                var _ when type == (typeof(IImmutableList<TElement>)) => true,
                var _ when type == (typeof(IImmutableQueue<TElement>)) => true,
                var _ when type == (typeof(IImmutableStack<TElement>)) => true,

                // System.Collections.Generics
                var _ when type == (typeof(IReadOnlySet<TElement>)) => true,
                var _ when type == (typeof(IReadOnlyList<TElement>)) => true,
                var _ when type == (typeof(IReadOnlyCollection<TElement>)) => true,
                var _ when type == (typeof(ISet<TElement>)) => true,
                var _ when type == (typeof(IList<TElement>)) => true,
                var _ when type == (typeof(ICollection<TElement>)) => true,

                // Leave IEnumerable to last, since it's the least specific.
                var _ when type == (typeof(IEnumerable<TElement>)) => true,

                _ => throw new InvalidOperationException($"Unable to create converter for '{type.FullName}'."),
            };

        }
        return false;
    }

    // There are four patterns that we support:
    // * The collection is an array: We use an array pool to buffer the elements and then create the final array.
    // * The collection is a concrete type that implements ICollection<T> and has a public parameterless constructor:
    //   We create an instance of that type as the buffer and add the elements to it directly.
    // * The collection is a well-known type that we have an adapter for: Queue<T>, Stack<T>, ReadOnlyCollection<T>,
    //   ImmutableArray<T>, etc. We use a specific adapter tailored for that type. For example, for Queue<T> we use
    //   the Queue directly as the buffer (queues don't implement ICollection<T>, so the adapter uses Push instead),
    //   or for ImmutableXXX<T> we either use ImmuttableXXX.CreateBuilder<T> to create a builder we use as a buffer,
    //   or collect the collection into an array buffer and call CreateRange to build the final collection.
    [RequiresDynamicCode(FormMappingHelpers.RequiresDynamicCodeMessage)]
    [RequiresUnreferencedCode(FormMappingHelpers.RequiresUnreferencedCodeMessage)]
    public override FormDataConverter CreateConverter(Type _, FormDataMapperOptions options)
    {
        // Resolve the element type converter
        var elementTypeConverter = options.ResolveConverter<TElement>() ??
            throw new InvalidOperationException($"Unable to create converter for '{typeof(TCollection).FullName}'.");

        // Arrays
        var type = typeof(TCollection);
        if (type.IsArray && type.GetArrayRank() == 1)
        {
            return new CollectionConverter<
                TElement[],
                ArrayPoolBufferAdapter<TElement[], ArrayCollectionFactory<TElement>, TElement>,
                ArrayPoolBufferAdapter<TElement[], ArrayCollectionFactory<TElement>, TElement>.PooledBuffer,
                TElement>(elementTypeConverter);
        }

        if (!type.IsInterface && !type.IsAbstract && !type.IsGenericTypeDefinition)
        {
            return type switch
            {
                // Special collections
                var _ when type.IsAssignableTo(typeof(Queue<TElement>)) =>
                    new CollectionConverter<Queue<TElement>, QueueBufferAdapter<TElement>, Queue<TElement>, TElement>(elementTypeConverter),
                var _ when type.IsAssignableTo(typeof(Stack<TElement>)) =>
                    new CollectionConverter<Stack<TElement>, StackBufferAdapter<TElement>, Stack<TElement>, TElement>(elementTypeConverter),
                var _ when type.IsAssignableTo(typeof(ReadOnlyCollection<TElement>)) =>
                    new CollectionConverter<ReadOnlyCollection<TElement>, ReadOnlyCollectionBufferAdapter<TElement>, IList<TElement>, TElement>(elementTypeConverter),

                // Concurrent collections
                var _ when type.IsAssignableTo(typeof(ConcurrentBag<TElement>)) =>
                    new CollectionConverter<ConcurrentBag<TElement>, ConcurrentBagBufferAdapter<TElement>, ConcurrentBag<TElement>, TElement>(elementTypeConverter),
                var _ when type.IsAssignableTo(typeof(ConcurrentStack<TElement>)) =>
                    new CollectionConverter<ConcurrentStack<TElement>, ConcurrentStackBufferAdapter<TElement>, ConcurrentStack<TElement>, TElement>(elementTypeConverter),
                var _ when type.IsAssignableTo(typeof(ConcurrentQueue<TElement>)) =>
                    new CollectionConverter<ConcurrentQueue<TElement>, ConcurrentQueueBufferAdapter<TElement>, ConcurrentQueue<TElement>, TElement>(elementTypeConverter),

                // Immutable collections
                var _ when type.IsAssignableTo(typeof(ImmutableArray<TElement>)) =>
                    new CollectionConverter<ImmutableArray<TElement>, ImmutableArrayBufferAdapter<TElement>, ImmutableArray<TElement>.Builder, TElement>(elementTypeConverter),
                var _ when type.IsAssignableTo(typeof(ImmutableList<TElement>)) =>
                    new CollectionConverter<ImmutableList<TElement>, ImmutableListBufferAdapter<TElement>, ImmutableList<TElement>.Builder, TElement>(elementTypeConverter),
                var _ when type.IsAssignableTo(typeof(ImmutableHashSet<TElement>)) =>
                    new CollectionConverter<ImmutableHashSet<TElement>, ImmutableHashSetBufferAdapter<TElement>, ImmutableHashSet<TElement>.Builder, TElement>(elementTypeConverter),
                var _ when type.IsAssignableTo(typeof(ImmutableSortedSet<TElement>)) =>
                    new CollectionConverter<ImmutableSortedSet<TElement>, ImmutableSortedSetBufferAdapter<TElement>, ImmutableSortedSet<TElement>.Builder, TElement>(elementTypeConverter),
                var _ when type.IsAssignableTo(typeof(ImmutableQueue<TElement>)) =>
                    new CollectionConverter<ImmutableQueue<TElement>, ImmutableQueueBufferAdapter<TElement>, ImmutableQueueBufferAdapter<TElement>.PooledBuffer, TElement>(elementTypeConverter),
                var _ when type.IsAssignableTo(typeof(ImmutableStack<TElement>)) =>
                    new CollectionConverter<ImmutableStack<TElement>, ImmutableStackBufferAdapter<TElement>, ImmutableStackBufferAdapter<TElement>.PooledBuffer, TElement>(elementTypeConverter),

                // Some of the types above implement ICollection<T>, but do so in a very inneficient way, so we want to
                // use special converters for them.
                var _ when type.IsAssignableTo(typeof(ICollection<TElement>))
                    => ConcreteTypeCollectionConverterFactory<TCollection, TElement>.Instance.CreateConverter(typeof(TCollection), options),
                _ => throw new InvalidOperationException($"Unable to create converter for '{typeof(TCollection).FullName}'.")
            };
        }

        if (type.IsInterface)
        {
            // At this point we are dealing with an interface. We test from the most specific to the least specific
            // to find the best fit for the well-known set of interfaces we support.
            return type switch
            {
                // System.Collections.Immutable
                var _ when type.IsAssignableTo(typeof(IImmutableSet<TElement>)) =>
                    ImmutableHashSetBufferAdapter<TElement>.CreateInterfaceConverter(elementTypeConverter),
                var _ when type.IsAssignableTo(typeof(IImmutableList<TElement>)) =>
                    ImmutableListBufferAdapter<TElement>.CreateInterfaceConverter(elementTypeConverter),
                var _ when type.IsAssignableTo(typeof(IImmutableQueue<TElement>)) =>
                    ImmutableQueueBufferAdapter<TElement>.CreateInterfaceConverter(elementTypeConverter),
                var _ when type.IsAssignableTo(typeof(IImmutableStack<TElement>)) =>
                    ImmutableStackBufferAdapter<TElement>.CreateInterfaceConverter(elementTypeConverter),

                // System.Collections.Generics
                var _ when type.IsAssignableTo(typeof(IReadOnlySet<TElement>)) =>
                    CreateConverter<IReadOnlySet<TElement>, HashSet<TElement>>(elementTypeConverter),
                var _ when type.IsAssignableTo(typeof(IReadOnlyList<TElement>)) =>
                    CreateConverter<IReadOnlyList<TElement>, List<TElement>>(elementTypeConverter),
                var _ when type.IsAssignableTo(typeof(IReadOnlyCollection<TElement>)) =>
                    CreateConverter<IReadOnlyCollection<TElement>, List<TElement>>(elementTypeConverter),
                var _ when type.IsAssignableTo(typeof(ISet<TElement>)) =>
                    CreateConverter<ISet<TElement>, HashSet<TElement>>(elementTypeConverter),
                var _ when type.IsAssignableTo(typeof(IList<TElement>)) =>
                    CreateConverter<IList<TElement>, List<TElement>>(elementTypeConverter),
                var _ when type.IsAssignableTo(typeof(ICollection<TElement>)) =>
                    CreateConverter<ICollection<TElement>, List<TElement>>(elementTypeConverter),

                // Leave IEnumerable to last, since it's the least specific.
                var _ when type.IsAssignableTo(typeof(IEnumerable<TElement>)) =>
                    CreateConverter<IEnumerable<TElement>, List<TElement>>(elementTypeConverter),

                _ => throw new InvalidOperationException($"Unable to create converter for '{type.FullName}'."),
            };
        }

        throw new InvalidOperationException($"Unable to create converter for '{type.FullName}'.");

        static FormDataConverter CreateConverter<TInterface, TImplementation>(FormDataConverter<TElement> elementTypeConverter)
            where TInterface : IEnumerable<TElement>
            where TImplementation : TInterface, ICollection<TElement>, new()
        {
            return new CollectionConverter<
                TInterface,
                ImplementingCollectionBufferAdapter<TInterface, TImplementation, TElement>,
                TImplementation,
                TElement>(elementTypeConverter);
        }
    }
}
