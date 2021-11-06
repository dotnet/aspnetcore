// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Internal;

namespace Microsoft.AspNetCore.Razor.Language;

internal class TagMatchingRuleDescriptorComparer : IEqualityComparer<TagMatchingRuleDescriptor>
{
    /// <summary>
    /// A default instance of the <see cref="TagMatchingRuleDescriptorComparer"/>.
    /// </summary>
    public static readonly TagMatchingRuleDescriptorComparer Default = new TagMatchingRuleDescriptorComparer();

    private TagMatchingRuleDescriptorComparer()
    {
    }

    public virtual bool Equals(TagMatchingRuleDescriptor ruleX, TagMatchingRuleDescriptor ruleY)
    {
        if (object.ReferenceEquals(ruleX, ruleY))
        {
            return true;
        }

        if (ruleX == null ^ ruleY == null)
        {
            return false;
        }

        return
            string.Equals(ruleX.TagName, ruleY.TagName, StringComparison.Ordinal) &&
            string.Equals(ruleX.ParentTag, ruleY.ParentTag, StringComparison.Ordinal) &&
            ruleX.CaseSensitive == ruleY.CaseSensitive &&
            ruleX.TagStructure == ruleY.TagStructure &&
            Enumerable.SequenceEqual(ruleX.Attributes, ruleY.Attributes, RequiredAttributeDescriptorComparer.Default);
    }

    public virtual int GetHashCode(TagMatchingRuleDescriptor rule)
    {
        if (rule == null)
        {
            throw new ArgumentNullException(nameof(rule));
        }

        var hash = HashCodeCombiner.Start();
        hash.Add(rule.TagName, StringComparer.Ordinal);
        hash.Add(rule.ParentTag, StringComparer.Ordinal);

        if (rule.Attributes != null)
        {
            for (var i = 0; i < rule.Attributes.Count; ++i)
            {
                hash.Add(rule.Attributes[i]);
            }
        }

        return hash.CombinedHash;
    }
}
