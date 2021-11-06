// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Internal;

namespace Microsoft.AspNetCore.Razor.Language;

internal sealed class TagHelperDescriptorComparer : IEqualityComparer<TagHelperDescriptor>
{
    /// <summary>
    /// A default instance of the <see cref="TagHelperDescriptorComparer"/>.
    /// </summary>
    public static readonly TagHelperDescriptorComparer Default = new TagHelperDescriptorComparer();

    private TagHelperDescriptorComparer()
    {
    }

    public bool Equals(TagHelperDescriptor descriptorX, TagHelperDescriptor descriptorY)
    {
        if (object.ReferenceEquals(descriptorX, descriptorY))
        {
            return true;
        }

        if (descriptorX == null ^ descriptorY == null)
        {
            return false;
        }

        if (!string.Equals(descriptorX.Kind, descriptorY.Kind, StringComparison.Ordinal))
        {
            return false;
        }

        if (!string.Equals(descriptorX.AssemblyName, descriptorY.AssemblyName, StringComparison.Ordinal))
        {
            return false;
        }

        if (!string.Equals(descriptorX.Name, descriptorY.Name, StringComparison.Ordinal))
        {
            return false;
        }

        if (!Enumerable.SequenceEqual(
            descriptorX.BoundAttributes.OrderBy(attribute => attribute.Name, StringComparer.Ordinal),
            descriptorY.BoundAttributes.OrderBy(attribute => attribute.Name, StringComparer.Ordinal),
            BoundAttributeDescriptorComparer.Default))
        {
            return false;
        }

        if (!Enumerable.SequenceEqual(
            descriptorX.TagMatchingRules.OrderBy(rule => rule.TagName, StringComparer.Ordinal),
            descriptorY.TagMatchingRules.OrderBy(rule => rule.TagName, StringComparer.Ordinal),
            TagMatchingRuleDescriptorComparer.Default))
        {
            return false;
        }

        if (!(descriptorX.AllowedChildTags == descriptorY.AllowedChildTags ||
            (descriptorX.AllowedChildTags != null &&
            descriptorY.AllowedChildTags != null &&
            Enumerable.SequenceEqual(
                descriptorX.AllowedChildTags.OrderBy(childTag => childTag.Name, StringComparer.Ordinal),
                descriptorY.AllowedChildTags.OrderBy(childTag => childTag.Name, StringComparer.Ordinal),
                AllowedChildTagDescriptorComparer.Default))))
        {
            return false;
        }

        if (descriptorX.CaseSensitive != descriptorY.CaseSensitive)
        {
            return false;
        }

        if (!string.Equals(descriptorX.Documentation, descriptorY.Documentation, StringComparison.Ordinal))
        {
            return false;
        }

        if (!string.Equals(descriptorX.DisplayName, descriptorY.DisplayName, StringComparison.Ordinal))
        {
            return false;
        }

        if (!string.Equals(descriptorX.TagOutputHint, descriptorY.TagOutputHint, StringComparison.Ordinal))
        {
            return false;
        }

        if (!Enumerable.SequenceEqual(descriptorX.Diagnostics, descriptorY.Diagnostics))
        {
            return false;
        }

        if (!Enumerable.SequenceEqual(
            descriptorX.Metadata.OrderBy(metadataX => metadataX.Key, StringComparer.Ordinal),
            descriptorY.Metadata.OrderBy(metadataY => metadataY.Key, StringComparer.Ordinal)))
        {
            return false;
        }

        return true;
    }

    /// <inheritdoc />
    public int GetHashCode(TagHelperDescriptor descriptor)
    {
        if (descriptor == null)
        {
            throw new ArgumentNullException(nameof(descriptor));
        }

        var hash = HashCodeCombiner.Start();
        hash.Add(descriptor.Kind, StringComparer.Ordinal);
        hash.Add(descriptor.AssemblyName, StringComparer.Ordinal);
        hash.Add(descriptor.Name, StringComparer.Ordinal);
        hash.Add(descriptor.DisplayName, StringComparer.Ordinal);
        hash.Add(descriptor.CaseSensitive ? 1 : 0);

        if (descriptor.BoundAttributes != null)
        {
            for (var i = 0; i < descriptor.BoundAttributes.Count; i++)
            {
                hash.Add(descriptor.BoundAttributes[i]);
            }
        }

        if (descriptor.TagMatchingRules != null)
        {
            for (var i = 0; i < descriptor.TagMatchingRules.Count; i++)
            {
                hash.Add(descriptor.TagMatchingRules[i]);
            }
        }

        if (descriptor.AllowedChildTags != null)
        {
            for (var i = 0; i < descriptor.AllowedChildTags.Count; i++)
            {
                hash.Add(descriptor.AllowedChildTags[i]);
            }
        }

        if (descriptor.Diagnostics != null)
        {
            for (var i = 0; i < descriptor.Diagnostics.Count; i++)
            {
                hash.Add(descriptor.Diagnostics[i]);
            }
        }

        if (descriptor.Metadata != null)
        {
            // 🐇 Avoid enumerator allocations for Dictionary<TKey, TValue>
            if (descriptor.Metadata is Dictionary<string, string> metadata)
            {
                foreach (var kvp in metadata)
                {
                    hash.Add(kvp.Key, StringComparer.Ordinal);
                    hash.Add(kvp.Value, StringComparer.Ordinal);
                }
            }
            else
            {
                foreach (var kvp in descriptor.Metadata)
                {
                    hash.Add(kvp.Key, StringComparer.Ordinal);
                    hash.Add(kvp.Value, StringComparer.Ordinal);
                }
            }
        }

        return hash.CombinedHash;
    }
}
