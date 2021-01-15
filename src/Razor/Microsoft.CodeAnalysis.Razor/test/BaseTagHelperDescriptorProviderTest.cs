// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.AspNetCore.Razor.Language.Components;
using Microsoft.CodeAnalysis.CSharp;
using Xunit;

namespace Microsoft.CodeAnalysis.Razor
{
    public abstract class TagHelperDescriptorProviderTestBase
    {
        static TagHelperDescriptorProviderTestBase()
        {
            BaseCompilation = TestCompilation.Create(typeof(ComponentTagHelperDescriptorProviderTest).Assembly);
            CSharpParseOptions = new CSharpParseOptions(LanguageVersion.CSharp7_3);
        }

        protected static Compilation BaseCompilation { get; }

        protected static CSharpParseOptions CSharpParseOptions { get; }

        protected static CSharpSyntaxTree Parse(string text)
        {
            return (CSharpSyntaxTree)CSharpSyntaxTree.ParseText(text, CSharpParseOptions);
        }

        // For simplicity in testing, exclude the built-in components. We'll add more and we
        // don't want to update the tests when that happens.
        protected static TagHelperDescriptor[] ExcludeBuiltInComponents(TagHelperDescriptorProviderContext context)
        {
            var results =
             context.Results
                .Where(c => c.AssemblyName != "Microsoft.AspNetCore.Razor.Test.ComponentShim")
                .Where(c => !c.DisplayName.StartsWith("Microsoft.AspNetCore.Components.Web", StringComparison.Ordinal))
                .Where(c => c.GetTypeName() != "Microsoft.AspNetCore.Components.Bind")
                .OrderBy(c => c.Name)
                .ToArray();

            return results;
        }

        protected static TagHelperDescriptor[] AssertAndExcludeFullyQualifiedNameMatchComponents(
            TagHelperDescriptor[] components,
            int expectedCount)
        {
            var componentLookup = new Dictionary<string, List<TagHelperDescriptor>>();
            var fullyQualifiedNameMatchComponents = components.Where(c => c.IsComponentFullyQualifiedNameMatch()).ToArray();
            Assert.Equal(expectedCount, fullyQualifiedNameMatchComponents.Length);

            var shortNameMatchComponents = components.Where(c => !c.IsComponentFullyQualifiedNameMatch()).ToArray();

            // For every fully qualified name component, we want to make sure we have a corresponding short name component.
            foreach (var fullNameComponent in fullyQualifiedNameMatchComponents)
            {
                Assert.Contains(shortNameMatchComponents, component =>
                {
                    return component.Name == fullNameComponent.Name &&
                        component.Kind == fullNameComponent.Kind &&
                        component.BoundAttributes.SequenceEqual(fullNameComponent.BoundAttributes, BoundAttributeDescriptorComparer.Default);
                });
            }

            return shortNameMatchComponents;
        }
    }
}
