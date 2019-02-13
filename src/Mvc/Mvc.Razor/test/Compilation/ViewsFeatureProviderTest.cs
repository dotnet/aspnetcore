// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Xunit;

namespace Microsoft.AspNetCore.Mvc.Razor.Compilation
{
#pragma warning disable CS0618 // Type or member is obsolete
    public class ViewsFeatureProviderTest
    {
        [Fact]
        public void PopulateFeature_ReturnsEmptySequenceIfNoAssemblyPartHasViewAssembly()
        {
            // Arrange
            var applicationPartManager = new ApplicationPartManager();
            applicationPartManager.ApplicationParts.Add(
                new AssemblyPart(typeof(ViewsFeatureProviderTest).GetTypeInfo().Assembly));
            applicationPartManager.FeatureProviders.Add(new ViewsFeatureProvider());
            var feature = new ViewsFeature();

            // Act
            applicationPartManager.PopulateFeature(feature);

            // Assert
            Assert.Empty(feature.ViewDescriptors);
        }

        [Fact]
        public void PopulateFeature_ReturnsViewsFromAllAvailableApplicationParts()
        {
            // Arrange
            var part1 = new AssemblyPart(typeof(object).GetTypeInfo().Assembly);
            var part2 = new AssemblyPart(GetType().GetTypeInfo().Assembly);
            var featureProvider = new TestableViewsFeatureProvider(new Dictionary<AssemblyPart, IEnumerable<RazorViewAttribute>>
            {
                {
                    part1,
                    new[]
                    {
                        new RazorViewAttribute("/Views/test/Index.cshtml", typeof(object)),
                    }
                },
                {
                    part2,
                    new[]
                    {
                        new RazorViewAttribute("/Areas/Admin/Views/Index.cshtml", typeof(string)),
                        new RazorViewAttribute("/Areas/Admin/Views/About.cshtml", typeof(int)),
                    }
                },
            });

            var applicationPartManager = new ApplicationPartManager();
            applicationPartManager.ApplicationParts.Add(part1);
            applicationPartManager.ApplicationParts.Add(part2);
            applicationPartManager.FeatureProviders.Add(featureProvider);
            var feature = new ViewsFeature();

            // Act
            applicationPartManager.PopulateFeature(feature);

            // Assert
            Assert.Collection(feature.ViewDescriptors.OrderBy(f => f.RelativePath, StringComparer.Ordinal),
                view =>
                {
                    Assert.Equal("/Areas/Admin/Views/About.cshtml", view.RelativePath);
                    Assert.Equal(typeof(int), view.ViewAttribute.ViewType);
                },
                view =>
                {
                    Assert.Equal("/Areas/Admin/Views/Index.cshtml", view.RelativePath);
                    Assert.Equal(typeof(string), view.ViewAttribute.ViewType);
                },
                view =>
                {
                    Assert.Equal("/Views/test/Index.cshtml", view.RelativePath);
                    Assert.Equal(typeof(object), view.ViewAttribute.ViewType);
                });
        }

        [Fact]
        public void PopulateFeature_ThrowsIfSingleAssemblyContainsMultipleAttributesWithTheSamePath()
        {
            // Arrange
            var path1 = "/Views/test/Index.cshtml";
            var path2 = "/views/test/index.cshtml";
            var expected = string.Join(
                Environment.NewLine,
                "The following precompiled view paths differ only in case, which is not supported:",
                path1,
                path2);
            var part = new AssemblyPart(typeof(object).GetTypeInfo().Assembly);
            var featureProvider = new TestableViewsFeatureProvider(new Dictionary<AssemblyPart, IEnumerable<RazorViewAttribute>>
            {
                {
                    part,
                    new[]
                    {
                        new RazorViewAttribute(path1, typeof(object)),
                        new RazorViewAttribute(path2, typeof(object)),
                    }
                },
            });

            var applicationPartManager = new ApplicationPartManager();
            applicationPartManager.ApplicationParts.Add(part);
            applicationPartManager.FeatureProviders.Add(featureProvider);
            var feature = new ViewsFeature();

            // Act & Assert
            var ex = Assert.Throws<InvalidOperationException>(() => applicationPartManager.PopulateFeature(feature));
            Assert.Equal(expected, ex.Message);
        }

        [Fact]
        public void PopulateFeature_ReturnsEmptySequenceIfNoDynamicAssemblyPartHasViewAssembly()
        {
            // Arrange
            var name = new AssemblyName($"DynamicAssembly-{Guid.NewGuid()}");
            var assembly = AssemblyBuilder.DefineDynamicAssembly(name,
                AssemblyBuilderAccess.RunAndCollect);

            var applicationPartManager = new ApplicationPartManager();
            applicationPartManager.ApplicationParts.Add(new AssemblyPart(assembly));
            applicationPartManager.FeatureProviders.Add(new ViewsFeatureProvider());
            var feature = new ViewsFeature();

            // Act
            applicationPartManager.PopulateFeature(feature);

            // Assert
            Assert.Empty(feature.ViewDescriptors);
        }

        [Fact]
        public void PopulateFeature_DoesNotFail_IfAssemblyHasEmptyLocation()
        {
            // Arrange
            var assembly = new AssemblyWithEmptyLocation();
            var applicationPartManager = new ApplicationPartManager();
            applicationPartManager.ApplicationParts.Add(new AssemblyPart(assembly));
            applicationPartManager.FeatureProviders.Add(new ViewsFeatureProvider());
            var feature = new ViewsFeature();

            // Act
            applicationPartManager.PopulateFeature(feature);

            // Assert
            Assert.Empty(feature.ViewDescriptors);
        }

        private class TestableViewsFeatureProvider : ViewsFeatureProvider
        {
            private readonly Dictionary<AssemblyPart, IEnumerable<RazorViewAttribute>> _attributeLookup;

            public TestableViewsFeatureProvider(Dictionary<AssemblyPart, IEnumerable<RazorViewAttribute>> attributeLookup)
            {
                _attributeLookup = attributeLookup;
            }

            protected override IEnumerable<RazorViewAttribute> GetViewAttributes(AssemblyPart assemblyPart)
            {
                return _attributeLookup[assemblyPart];
            }
        }

        private class AssemblyWithEmptyLocation : Assembly
        {
            public override string Location => string.Empty;

            public override string FullName => typeof(ViewsFeatureProviderTest).GetTypeInfo().Assembly.FullName;

            public override IEnumerable<TypeInfo> DefinedTypes
            {
                get
                {
                    throw new NotImplementedException();
                }
            }

            public override IEnumerable<Module> Modules
            {
                get
                {
                    throw new NotImplementedException();
                }
            }
        }
    }
#pragma warning restore CS0618 // Type or member is obsolete
}
