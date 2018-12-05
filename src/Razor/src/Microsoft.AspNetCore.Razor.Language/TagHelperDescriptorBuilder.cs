// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.
using System;
using System.Collections.Generic;

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

        public abstract string Name { get; }

        public abstract string AssemblyName { get; }

        public abstract string Kind { get; }

        public abstract string DisplayName { get; set; }

        public abstract string TagOutputHint { get; set; }

        public abstract string Documentation { get; set; }

        public abstract IDictionary<string, string> Metadata { get; }

        public abstract RazorDiagnosticCollection Diagnostics { get; }

        public abstract IReadOnlyList<AllowedChildTagDescriptorBuilder> AllowedChildTags { get; }

        public abstract IReadOnlyList<BoundAttributeDescriptorBuilder> BoundAttributes { get; }

        public abstract IReadOnlyList<TagMatchingRuleDescriptorBuilder> TagMatchingRules { get; }

        public abstract void AllowChildTag(Action<AllowedChildTagDescriptorBuilder> configure);

        public abstract void BindAttribute(Action<BoundAttributeDescriptorBuilder> configure);

        public abstract void TagMatchingRule(Action<TagMatchingRuleDescriptorBuilder> configure);

        public abstract TagHelperDescriptor Build();

        public abstract void Reset();
    }
}
