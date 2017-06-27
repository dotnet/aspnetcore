// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.
using System;

namespace Microsoft.AspNetCore.Razor.Language
{
    public abstract class TagHelperDescriptorBuilder
    {
        public static TagHelperDescriptorBuilder Create(string name, string assemblyName)
        {
            if (name == null)
            {
                throw new ArgumentNullException(nameof(name));
            }

            if (assemblyName == null)
            {
                throw new ArgumentNullException(nameof(assemblyName));
            }

            return new DefaultTagHelperDescriptorBuilder(TagHelperConventions.DefaultKind, name, assemblyName);
        }

        public static TagHelperDescriptorBuilder Create(string kind, string name, string assemblyName)
        {
            if (kind == null)
            {
                throw new ArgumentNullException(nameof(kind));
            }

            if (name == null)
            {
                throw new ArgumentNullException(nameof(name));
            }

            if (assemblyName == null)
            {
                throw new ArgumentNullException(nameof(assemblyName));
            }

            return new DefaultTagHelperDescriptorBuilder(kind, name, assemblyName);
        }

        public abstract TagHelperDescriptorBuilder BindAttribute(Action<BoundAttributeDescriptorBuilder> configure);

        public abstract TagHelperDescriptorBuilder TagMatchingRule(Action<TagMatchingRuleDescriptorBuilder> configure);

        public abstract TagHelperDescriptorBuilder AllowChildTag(string allowedChild);

        public abstract TagHelperDescriptorBuilder TagOutputHint(string hint);

        public abstract TagHelperDescriptorBuilder Documentation(string documentation);

        public abstract TagHelperDescriptorBuilder AddMetadata(string key, string value);

        public abstract TagHelperDescriptorBuilder AddDiagnostic(RazorDiagnostic diagnostic);

        public abstract TagHelperDescriptorBuilder DisplayName(string displayName);

        public abstract TagHelperDescriptorBuilder TypeName(string typeName);

        public abstract TagHelperDescriptor Build();

        public abstract void Reset();
    }
}
