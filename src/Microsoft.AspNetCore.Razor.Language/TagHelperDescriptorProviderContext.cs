// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;

namespace Microsoft.AspNetCore.Razor.Language
{
    public abstract class TagHelperDescriptorProviderContext
    {
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
}
