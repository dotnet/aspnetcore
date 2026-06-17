// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Runtime.CompilerServices;

namespace Microsoft.AspNetCore.Components.Endpoints.FormMapping;

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
    private static readonly Type _elementType = typeof(TElement);

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

    [RequiresDynamicCode(FormMappingHelpers.RequiresDynamicCodeMessage)]
    [RequiresUnreferencedCode(FormMappingHelpers.RequiresUnreferencedCodeMessage)]
    internal override bool TryRead(
        ref FormDataReader context,
        Type type,
        FormDataMapperOptions options,
        [NotNullWhen(true)] out TCollection? result,
        out bool found)
    {
        TElement currentElement;
        TBuffer? buffer = default;
        bool foundCurrentElement;
        bool currentElementSuccess;
        bool succeded;
        // Even though we have indexes, we special case 0 an 1 and use literals directly. We leave them in the indexes
        // collection because it makes other indexes align.
        try
        {
            context.PushPrefix("[0]");
            succeded = _elementConverter.TryRead(ref context, _elementType, options, out currentElement!, out found);
        }
        finally
        {
            context.PopPrefix("[0]");
        }

        if (!found)
        {
            return TryReadSingleValueCollection(ref context, out result, ref found, ref buffer, ref succeded);
        }

        // We already know we found an element;
        found = true;
        // At this point we have at least found one element, we can create the collection and assign it to it.
        buffer = TCollectionPolicy.CreateBuffer();
        buffer = TCollectionPolicy.Add(ref buffer, currentElement!);

        // Read element 1 and set conditions to enter the loop
        try
        {
            context.PushPrefix("[1]");
            currentElementSuccess = _elementConverter.TryRead(ref context, _elementType, options, out currentElement!, out foundCurrentElement);
            succeded = succeded && currentElementSuccess;
        }
        catch
        {
            if (buffer != null)
            {
                // Ensure the buffer is cleaned up if we fail.
                result = TCollectionPolicy.ToResult(buffer);
            }
            throw;
        }
        finally
        {
            context.PopPrefix("[1]");
        }

        var maxCollectionSize = options.MaxCollectionSize;

        // We need to iterate while we keep finding values, even if some of them have errors. This is because we don't want data to be lost just
        // because we are not able to parse it. For example, if [5] = "asdf", we don't want to loose the values for [6], [7], etc. that can be
        // valid.
        // There will be a limit to how many errors we collect, at which point we will stop capturing errors.
        // Similarly, over 100 elements, we'll start doing more work to compute the index prefix. We chose 100 because that's the default
        // max collection size that we will support.
        var index = 2;
        var lastElementWithComputedIndex = 100 < maxCollectionSize ? 100 : maxCollectionSize;
        for (; index < lastElementWithComputedIndex && foundCurrentElement; index++)
        {
            // Add the current element
            buffer = TCollectionPolicy.Add(ref buffer, currentElement!);

            // Get the precomputed prefix and try and bind the element.
            var prefix = Indexes[index];
            try
            {
                context.PushPrefix(prefix);
                currentElementSuccess = _elementConverter.TryRead(ref context, _elementType, options, out currentElement!, out foundCurrentElement);
                succeded = succeded && currentElementSuccess;
            }
            catch
            {
                if (buffer != null)
                {
                    // Ensure the buffer is cleaned up if we fail.
                    result = TCollectionPolicy.ToResult(buffer);
                }
                throw;
            }
            finally
            {
                context.PopPrefix(prefix);
            }
        }

        if (!foundCurrentElement)
        {
            result = TCollectionPolicy.ToResult(buffer);
            if (!succeded)
            {
                context.AttachInstanceToErrors(result!);
            }
            return succeded;
        }

        // We need to compute the prefix for the index, since it's not precomputed.
        // The biggest UInt32 representation is 4294967295, which is 10 characters, so 16 chars is more than enough to
        // hold the representation.
        Span<char> computedPrefix = stackalloc char[16];
        computedPrefix[0] = '[';

        // index is 100 here.
        // We want to go 1 element over of max collection size, so we can report an error if we find it.
        for (; index <= maxCollectionSize && foundCurrentElement; index++)
        {
            // Add the current element
            buffer = TCollectionPolicy.Add(ref buffer, currentElement!);

            // We need to compute the prefix for the index, since it's not precomputed.
            if (!index.TryFormat(computedPrefix[1..], out var charsWritten, provider: CultureInfo.InvariantCulture))
            {
                succeded = false;
                break;
            }

            try
            {
                computedPrefix[charsWritten + 1] = ']';
                context.PushPrefix(computedPrefix[..(charsWritten + 2)]);
                currentElementSuccess = _elementConverter.TryRead(ref context, _elementType, options, out currentElement!, out foundCurrentElement);
                succeded = succeded && currentElementSuccess;
            }
            catch
            {
                if (buffer != null)
                {
                    // Ensure the buffer is cleaned up if we fail.
                    result = TCollectionPolicy.ToResult(buffer);
                }
                throw;
            }
            finally
            {
                context.PopPrefix(computedPrefix[..(charsWritten + 2)]);
            }
        }

        result = TCollectionPolicy.ToResult(buffer);
        if (index > maxCollectionSize && foundCurrentElement)
        {
            // Signal failure because we have stopped binding.
            context.AddMappingError(
                FormattableStringFactory.Create(FormDataResources.MaxCollectionSizeReached, "collection", maxCollectionSize),
                null);

            context.AttachInstanceToErrors(result!);
            return false;
        }
        else
        {
            if (!succeded)
            {
                context.AttachInstanceToErrors(result!);
            }
            return succeded;
        }
    }

    private bool TryReadSingleValueCollection(ref FormDataReader context, out TCollection? result, ref bool found, ref TBuffer? buffer, ref bool succeded)
    {
        if (_elementConverter is ISingleValueConverter<TElement> singleValueConverter &&
            singleValueConverter.CanConvertSingleValue() &&
            context.TryGetValues(out var values))
        {
            found = true;
            buffer = TCollectionPolicy.CreateBuffer();

            for (var i = 0; i < values.Count; i++)
            {
                var value = values[i];
                try
                {
                    if (!singleValueConverter.TryConvertValue(ref context, value!, out var elementValue))
                    {
                        succeded = false;
                    }
                    else
                    {
                        buffer = TCollectionPolicy.Add(ref buffer, elementValue);
                    }
                }
                catch (Exception ex)
                {
                    succeded = false;
                    context.AddMappingError(ex, value);
                }
            };

            result = TCollectionPolicy.ToResult(buffer);
        }
        else
        {
            result = default;
        }

        return succeded;
    }
}
