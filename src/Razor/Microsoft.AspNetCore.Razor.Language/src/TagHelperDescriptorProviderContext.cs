// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;

namespace Microsoft.AspNetCore.Razor.Language;

public abstract class TagHelperDescriptorProviderContext
{
    public virtual bool ExcludeHidden { get; set; }

    public virtual bool IncludeDocumentation { get; set; }

    public abstract ItemCollection Items { get; }

    public abstract ICollection<TagHelperDescriptor> Results { get; }

    public static TagHelperDescriptorProviderContext Create()
    {
        return new DefaultContext(new List<TagHelperDescriptor>());
    }

    public static TagHelperDescriptorProviderContext Create(ICollection<TagHelperDescriptor> results)
    {
        if (results == null)
        {
            throw new ArgumentNullException(nameof(results));
        }

        return new DefaultContext(results);
    }

    private class DefaultContext : TagHelperDescriptorProviderContext
    {
        public DefaultContext(ICollection<TagHelperDescriptor> results)
        {
            Results = results;

            Items = new ItemCollection();
        }

        public override ItemCollection Items { get; }

        public override ICollection<TagHelperDescriptor> Results { get; }
    }
}
