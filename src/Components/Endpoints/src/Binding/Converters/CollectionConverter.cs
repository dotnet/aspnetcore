// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Globalization;

namespace Microsoft.AspNetCore.Components.Endpoints.Binding;

// The algorithm for collections is always the same. There are two main cases:
// The collection can be modified, so we can add items to it.
// The collection cannot be modified, so we need to use a buffer and copy the items to it once we are done.
// When adding to the buffer, there are two cases:
// The buffer implements ICollection<T>, so we can add items to it.
// The buffer does not implement ICollection<T>, so we need to use custom code to add items to it.
// These aspects are captured in the TCollectionPolicy type parameter.
// Instead of creating a hierachy with virtual members, we are using generics and virtual interface dispatch to achieve the same result.
// This allows us to avoid virtual dispatch at runtime, and enables us to easily adapt to different types of collections.

internal abstract class CollectionConverter<TCollection> : FormDataConverter<TCollection>
{
}

internal class CollectionConverter<TCollection, TCollectionPolicy, TBuffer, TElement> : CollectionConverter<TCollection>
    where TCollectionPolicy : ICollectionBufferAdapter<TCollection, TBuffer, TElement>
{
    // Indexes up to 100 are pre-allocated to avoid allocations for common cases.
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

    private readonly FormDataConverter<TElement> _elementConverter;

    public CollectionConverter(FormDataConverter<TElement> elementConverter)
    {
        ArgumentNullException.ThrowIfNull(elementConverter);
        _elementConverter = elementConverter;
    }

    internal override bool TryRead(
        ref FormDataReader context,
        Type type,
        FormDataMapperOptions options,
        [NotNullWhen(true)] out TCollection? result,
        out bool found)
    {
        TElement currentElement;
        TBuffer buffer;
        bool foundCurrentElement;
        bool currentElementSuccess;
        bool succeded;
        // Even though we have indexes, we special case 0 an 1 and use literals directly. We leave them in the indexes
        // collection because it makes other indexes align.
        context.PushPrefix("[0]");
        succeded = _elementConverter.TryRead(ref context, typeof(TElement), options, out currentElement!, out found);
        context.PopPrefix("[0]");
        if (!found)
        {
            result = default;
            return succeded;
        }

        // We already know we found an element;
        found = true;
        // At this point we have at least found one element, we can create the collection and assign it to it.
        buffer = TCollectionPolicy.CreateBuffer();
        buffer = TCollectionPolicy.Add(ref buffer, currentElement!);

        // Read element 1 and set conditions to enter the loop
        context.PushPrefix("[1]");
        currentElementSuccess = _elementConverter.TryRead(ref context, typeof(TElement), options, out currentElement!, out foundCurrentElement);
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
            buffer = TCollectionPolicy.Add(ref buffer, currentElement!);

            // Get the precomputed prefix and try and bind the element.
            var prefix = Indexes[index];
            context.PushPrefix(prefix);
            currentElementSuccess = _elementConverter.TryRead(ref context, typeof(TElement), options, out currentElement!, out foundCurrentElement);
            succeded = succeded && currentElementSuccess;
            context.PopPrefix(prefix);
        }

        if (!foundCurrentElement)
        {
            result = TCollectionPolicy.ToResult(buffer);
            return succeded;
        }

        // We have a "large" collection. Loop until we stop finding elements or reach the max collection size.
        var maxCollectionSize = options.MaxCollectionSize;

        // We need to compute the prefix for the index, since it's not precomputed.
        // The biggest UInt32 representation is 4294967295, which is 10 characters, so 16 chars is more than enough to
        // hold the representation.
        Span<char> computedPrefix = stackalloc char[16];
        computedPrefix[0] = '[';

        // index is 100 here.
        for (; index < maxCollectionSize && foundCurrentElement; index++)
        {
            // Add the current element
            buffer = TCollectionPolicy.Add(ref buffer, currentElement!);

            // We need to compute the prefix for the index, since it's not precomputed.
            if (!index.TryFormat(computedPrefix[1..], out var charsWritten, provider: CultureInfo.InvariantCulture))
            {
                succeded = false;
                break;
            }

            computedPrefix[charsWritten + 1] = ']';
            context.PushPrefix(computedPrefix[..(charsWritten + 2)]);
            currentElementSuccess = _elementConverter.TryRead(ref context, typeof(TElement), options, out currentElement!, out foundCurrentElement);
            succeded = succeded && currentElementSuccess;
            context.PopPrefix(computedPrefix[..(charsWritten + 2)]);
        }

        if (!foundCurrentElement)
        {
            result = TCollectionPolicy.ToResult(buffer);
            return succeded;
        }
        else
        {
            // We reached the max collection size.
            buffer = TCollectionPolicy.Add(ref buffer, currentElement!);
            result = TCollectionPolicy.ToResult(buffer);

            // Signal failure because we have stopped binding.
            // We will include an error in the list of reported errors.
            return false;
        }
    }
}
