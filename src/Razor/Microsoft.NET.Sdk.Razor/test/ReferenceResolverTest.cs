// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace Microsoft.AspNetCore.Razor.Tasks
{
    public class ReferenceResolverTest
    {
        internal static readonly string[] MvcAssemblies = new[]
        {
            "Microsoft.AspNetCore.Mvc",
            "Microsoft.AspNetCore.Mvc.Abstractions",
            "Microsoft.AspNetCore.Mvc.ApiExplorer",
            "Microsoft.AspNetCore.Mvc.Core",
            "Microsoft.AspNetCore.Mvc.Cors",
            "Microsoft.AspNetCore.Mvc.DataAnnotations",
            "Microsoft.AspNetCore.Mvc.Formatters.Json",
            "Microsoft.AspNetCore.Mvc.Formatters.Xml",
            "Microsoft.AspNetCore.Mvc.Localization",
            "Microsoft.AspNetCore.Mvc.NewtonsoftJson",
            "Microsoft.AspNetCore.Mvc.Razor",
            "Microsoft.AspNetCore.Mvc.RazorPages",
            "Microsoft.AspNetCore.Mvc.TagHelpers",
            "Microsoft.AspNetCore.Mvc.ViewFeatures",
        };

        [Fact]
        public void Resolve_ReturnsEmptySequence_IfNoAssemblyReferencesMvc()
        {
            // Arrange
            var resolver = new TestReferencesToMvcResolver(new[]
            {
                CreateAssemblyItem("Microsoft.AspNetCore.Blazor"),
                CreateAssemblyItem("Microsoft.AspNetCore.Components"),
                CreateAssemblyItem("Microsoft.JSInterop"),
                CreateAssemblyItem("System.Net.Http"),
                CreateAssemblyItem("System.Runtime"),
            });

            resolver.Add("Microsoft.AspNetCore.Blazor", "Microsoft.AspNetCore.Components", "Microsoft.JSInterop");
            resolver.Add("Microsoft.AspNetCore.Components", "Microsoft.JSInterop", "System.Net.Http", "System.Runtime");
            resolver.Add("System.Net.Http", "System.Runtime");

            // Act
            var assemblies = resolver.ResolveAssemblies();

            // Assert
            Assert.Empty(assemblies);
        }

        [Fact]
        public void Resolve_ReturnsEmptySequence_IfNoDependencyReferencesMvc()
        {
            // Arrange
            var resolver = new TestReferencesToMvcResolver(new[]
            {
                CreateAssemblyItem("MyApp.Models"),
                CreateAssemblyItem("Microsoft.AspNetCore.Mvc", isFrameworkReference: true),
                CreateAssemblyItem("Microsoft.AspNetCore.Hosting", isFrameworkReference: true),
                CreateAssemblyItem("Microsoft.AspNetCore.HttpAbstractions", isFrameworkReference: true),
                CreateAssemblyItem("Microsoft.AspNetCore.KestrelHttpServer", isFrameworkReference: true),
                CreateAssemblyItem("Microsoft.AspNetCore.StaticFiles", isFrameworkReference: true),
                CreateAssemblyItem("Microsoft.Extensions.Primitives", isFrameworkReference: true),
                CreateAssemblyItem("System.Net.Http", isFrameworkReference: true),
                CreateAssemblyItem("Microsoft.EntityFrameworkCore"),
            });

            resolver.Add("MyApp.Models", "Microsoft.EntityFrameworkCore");
            resolver.Add("Microsoft.AspNetCore.Mvc", "Microsoft.AspNetCore.HttpAbstractions");
            resolver.Add("Microsoft.AspNetCore.KestrelHttpServer", "Microsoft.AspNetCore.Hosting", "Microsoft.AspNetCore.HttpAbstractions");
            resolver.Add("Microsoft.AspNetCore.StaticFiles", "Microsoft.AspNetCore.HttpAbstractions", "Microsoft.Extensions.Primitives");
            resolver.Add("Microsoft.AspNetCore.Hosting", "Microsoft.AspNetCore.HttpAbstractions");
            resolver.Add("Microsoft.AspNetCore.HttpAbstractions", "Microsoft.Extensions.Primitives");

            // Act
            var assemblies = resolver.ResolveAssemblies();

            // Assert
            Assert.Empty(assemblies);
        }

        [Fact]
        public void Resolve_ReturnsReferences_ThatReferenceMvc()
        {
            // Arrange
            var resolver = new TestReferencesToMvcResolver(new[]
            {
                CreateAssemblyItem("Microsoft.AspNetCore.Mvc", isFrameworkReference: true),
                CreateAssemblyItem("Microsoft.AspNetCore.Mvc.TagHelpers", isFrameworkReference: true),
                CreateAssemblyItem("MyTagHelpers"),
                CreateAssemblyItem("MyControllers"),
                CreateAssemblyItem("MyApp.Models"),
                CreateAssemblyItem("Microsoft.AspNetCore.Hosting", isFrameworkReference: true),
                CreateAssemblyItem("Microsoft.AspNetCore.HttpAbstractions", isFrameworkReference: true),
                CreateAssemblyItem("Microsoft.AspNetCore.KestrelHttpServer", isFrameworkReference: true),
                CreateAssemblyItem("Microsoft.AspNetCore.StaticFiles", isFrameworkReference: true),
                CreateAssemblyItem("Microsoft.Extensions.Primitives", isFrameworkReference: true),
                CreateAssemblyItem("Microsoft.EntityFrameworkCore"),
            });

            resolver.Add("MyTagHelpers", "Microsoft.AspNetCore.Mvc.TagHelpers");
            resolver.Add("MyControllers", "Microsoft.AspNetCore.Mvc");
            resolver.Add("MyApp.Models", "Microsoft.EntityFrameworkCore");
            resolver.Add("Microsoft.AspNetCore.Mvc", "Microsoft.AspNetCore.HttpAbstractions", "Microsoft.AspNetCore.Mvc.TagHelpers");
            resolver.Add("Microsoft.AspNetCore.KestrelHttpServer", "Microsoft.AspNetCore.Hosting", "Microsoft.AspNetCore.HttpAbstractions");
            resolver.Add("Microsoft.AspNetCore.StaticFiles", "Microsoft.AspNetCore.HttpAbstractions", "Microsoft.Extensions.Primitives");
            resolver.Add("Microsoft.AspNetCore.Hosting", "Microsoft.AspNetCore.HttpAbstractions");
            resolver.Add("Microsoft.AspNetCore.HttpAbstractions", "Microsoft.Extensions.Primitives");

            // Act
            var assemblies = resolver.ResolveAssemblies();

            // Assert
            Assert.Equal(new[] { "MyControllers", "MyTagHelpers" }, assemblies.OrderBy(a => a));
        }

        [Fact]
        public void Resolve_ReturnsItemsThatTransitivelyReferenceMvc()
        {
            // Arrange
            var resolver = new TestReferencesToMvcResolver(new[]
            {
                CreateAssemblyItem("MyCMS"),
                CreateAssemblyItem("MyCMS.Core"),
                CreateAssemblyItem("Microsoft.AspNetCore.Mvc.ViewFeatures", isFrameworkReference: true),
            });

            resolver.Add("MyCMS", "MyCMS.Core");
            resolver.Add("MyCMS.Core", "Microsoft.AspNetCore.Mvc.ViewFeatures");

            // Act
            var assemblies = resolver.ResolveAssemblies();

            // Assert
            Assert.Equal(new[] { "MyCMS", "MyCMS.Core" }, assemblies.OrderBy(a => a));
        }

        [Fact]
        public void Resolve_Works_WhenAssemblyReferencesAreRecursive()
        {
            // Test for https://github.com/dotnet/aspnetcore/issues/12693
            // Arrange
            var resolver = new TestReferencesToMvcResolver(new[]
            {
                CreateAssemblyItem("PresentationFramework"),
                CreateAssemblyItem("ReachFramework"),
                CreateAssemblyItem("MyCMS"),
                CreateAssemblyItem("MyCMS.Core"),
                CreateAssemblyItem("Microsoft.AspNetCore.Mvc.ViewFeatures", isFrameworkReference: true),
            });

            resolver.Add("PresentationFramework", "ReachFramework");
            resolver.Add("ReachFramework", "PresentationFramework");

            resolver.Add("MyCMS", "MyCMS.Core");
            resolver.Add("MyCMS.Core", "Microsoft.AspNetCore.Mvc.ViewFeatures");

            // Act
            var assemblies = resolver.ResolveAssemblies();

            // Assert
            Assert.Equal(new[] { "MyCMS", "MyCMS.Core" }, assemblies.OrderBy(a => a));
        }

        [Fact]
        public void Resolve_Works_WhenAssemblyReferencesAreRecursive_ButAlsoReferencesMvc()
        {
            // Arrange
            var resolver = new TestReferencesToMvcResolver(new[]
            {
                CreateAssemblyItem("MyCoolLibrary"),
                CreateAssemblyItem("PresentationFramework"),
                CreateAssemblyItem("ReachFramework"),
                CreateAssemblyItem("MyCMS"),
                CreateAssemblyItem("MyCMS.Core"),
                CreateAssemblyItem("Microsoft.AspNetCore.Mvc.ViewFeatures", isFrameworkReference: true),
            });

            resolver.Add("MyCoolLibrary", "PresentationFramework");
            resolver.Add("PresentationFramework", "ReachFramework");
            resolver.Add("ReachFramework", "PresentationFramework", "MyCMS");

            resolver.Add("MyCMS", "MyCMS.Core");
            resolver.Add("MyCMS.Core", "Microsoft.AspNetCore.Mvc.ViewFeatures");

            // Act
            var assemblies = resolver.ResolveAssemblies();

            // Assert
            Assert.Equal(new[] { "MyCMS", "MyCMS.Core", "MyCoolLibrary", "PresentationFramework", "ReachFramework" }, assemblies.OrderBy(a => a));
        }

        public AssemblyItem CreateAssemblyItem(string name, bool isFrameworkReference = false)
        {
            return new AssemblyItem
            {
                AssemblyName = name,
                IsFrameworkReference = isFrameworkReference,
                Path = name,
            };
        }

        private class TestReferencesToMvcResolver : ReferenceResolver
        {
            private readonly Dictionary<string, string[]> _references = new Dictionary<string, string[]>();

            public TestReferencesToMvcResolver(AssemblyItem[] referenceItems)
                : base(MvcAssemblies, referenceItems)
            {
            }

            public void Add(string assembly, params string[] references)
            {
                _references.Add(assembly, references);
            }

            protected override IReadOnlyList<AssemblyItem> GetReferences(string file)
            {
                if (_references.TryGetValue(file, out var result))
                {
                    return result.Select(r => Lookup[r]).ToArray();
                }

                return Array.Empty<AssemblyItem>();
            }
        }
    }
}
