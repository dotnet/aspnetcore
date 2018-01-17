// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.AspNetCore.Razor.Hosting;
using Moq;
using Xunit;

namespace Microsoft.AspNetCore.Mvc.Razor.Compilation
{
    public class ViewsFeatureProviderTest
    {
        [Fact]
        public void PopulateFeature_ReturnsEmptySequenceIfNoAssemblyPartHasViewAssembly()
        {
            // Arrange
            var partManager = new ApplicationPartManager();
            partManager.ApplicationParts.Add(new AssemblyPart(typeof(ViewsFeatureProviderTest).Assembly));
            partManager.FeatureProviders.Add(new ViewsFeatureProvider());
            var feature = new ViewsFeature();

            // Act
            partManager.PopulateFeature(feature);

            // Assert
            Assert.Empty(feature.ViewDescriptors);
        }

        [Fact]
        public void PopulateFeature_ReturnsViewsFromAllAvailableApplicationParts()
        {
            // Arrange
            var part1 = new AssemblyPart(typeof(object).Assembly);
            var part2 = new AssemblyPart(GetType().Assembly);

            var items = new Dictionary<AssemblyPart, IReadOnlyList<RazorCompiledItem>>
            {
                {
                    part1,
                    new[]
                    {
                        new TestRazorCompiledItem(typeof(object), "mvc.1.0.view", "/Views/test/Index.cshtml", new object[]{ }),

                        // This one doesn't have a RazorViewAttribute
                        new TestRazorCompiledItem(typeof(StringBuilder), "mvc.1.0.view", "/Views/test/About.cshtml", new object[]{ }),
                    }
                },
                {
                    part2,
                    new[]
                    {
                        new TestRazorCompiledItem(typeof(string), "mvc.1.0.view", "/Areas/Admin/Views/Index.cshtml", new object[]{ }),
                    }
                },
            };

            var attributes = new Dictionary<AssemblyPart, IEnumerable<RazorViewAttribute>>
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

                        // This one doesn't have a RazorCompiledItem
                        new RazorViewAttribute("/Areas/Admin/Views/About.cshtml", typeof(int)),
                    }
                },
            };

            var featureProvider = new TestableViewsFeatureProvider(items, attributes);
            var partManager = new ApplicationPartManager();
            partManager.ApplicationParts.Add(part1);
            partManager.ApplicationParts.Add(part2);
            partManager.FeatureProviders.Add(featureProvider);
            var feature = new ViewsFeature();

            // Act
            partManager.PopulateFeature(feature);

            // Assert
            Assert.Collection(feature.ViewDescriptors.OrderBy(f => f.RelativePath, StringComparer.Ordinal),
                view =>
                {
                    Assert.Empty(view.ExpirationTokens);
                    Assert.True(view.IsPrecompiled);
                    Assert.Null(view.Item);
                    Assert.Equal("/Areas/Admin/Views/About.cshtml", view.RelativePath);
                    Assert.Equal(typeof(int), view.Type);
                    Assert.Equal("/Areas/Admin/Views/About.cshtml", view.ViewAttribute.Path);
                    Assert.Equal(typeof(int), view.ViewAttribute.ViewType);
                },
                view =>
                {
                    // This one doesn't have a RazorCompiledItem
                    Assert.Empty(view.ExpirationTokens);
                    Assert.True(view.IsPrecompiled);
                    Assert.Equal("/Areas/Admin/Views/Index.cshtml", view.Item.Identifier);
                    Assert.Equal("mvc.1.0.view", view.Item.Kind);
                    Assert.Equal(typeof(string), view.Item.Type);
                    Assert.Equal("/Areas/Admin/Views/Index.cshtml", view.RelativePath);
                    Assert.Equal(typeof(string), view.Type);
                    Assert.Equal("/Areas/Admin/Views/Index.cshtml", view.ViewAttribute.Path);
                    Assert.Equal(typeof(string), view.ViewAttribute.ViewType);
                },
                view =>
                {
                    // This one doesn't have a RazorViewAttribute
                    Assert.Empty(view.ExpirationTokens);
                    Assert.True(view.IsPrecompiled);
                    Assert.Equal("/Views/test/About.cshtml", view.Item.Identifier);
                    Assert.Equal("mvc.1.0.view", view.Item.Kind);
                    Assert.Equal(typeof(StringBuilder), view.Item.Type);
                    Assert.Equal("/Views/test/About.cshtml", view.RelativePath);
                    Assert.Equal(typeof(StringBuilder), view.Type);
                    Assert.Null(view.ViewAttribute);
                },
                view =>
                {
                    Assert.Empty(view.ExpirationTokens);
                    Assert.True(view.IsPrecompiled);
                    Assert.Equal("/Views/test/Index.cshtml", view.Item.Identifier);
                    Assert.Equal("mvc.1.0.view", view.Item.Kind);
                    Assert.Equal(typeof(object), view.Item.Type);
                    Assert.Equal("/Views/test/Index.cshtml", view.RelativePath);
                    Assert.Equal(typeof(object), view.Type);
                    Assert.Equal("/Views/test/Index.cshtml", view.ViewAttribute.Path);
                    Assert.Equal(typeof(object), view.ViewAttribute.ViewType);
                });
        }

        [Fact]
        public void PopulateFeature_PrefersViewsFromPartsWithHigherPrecedence()
        {
            // Arrange
            var part1 = new AssemblyPart(typeof(ViewsFeatureProvider).Assembly);
            var item1 = new TestRazorCompiledItem(typeof(StringBuilder), "mvc.1.0.view", "/Areas/Admin/Views/Shared/_Layout.cshtml", new object[] { });

            var part2 = new AssemblyPart(GetType().Assembly);
            var item2 = new TestRazorCompiledItem(typeof(string), "mvc.1.0.view", "/Areas/Admin/Views/Shared/_Layout.cshtml", new object[] { });
            var item3 = new TestRazorCompiledItem(typeof(string), "mvc.1.0.view", "/Areas/Admin/Views/Shared/_Partial.cshtml", new object[] { });

            var items = new Dictionary<AssemblyPart, IReadOnlyList<RazorCompiledItem>>
            {
                { part1, new[] { item1 } },
                { part2, new[] { item2, item3, } },
            };

            var featureProvider = new TestableViewsFeatureProvider(items, attributes: new Dictionary<AssemblyPart, IEnumerable<RazorViewAttribute>>());
            var partManager = new ApplicationPartManager();
            partManager.ApplicationParts.Add(part1);
            partManager.ApplicationParts.Add(part2);
            partManager.FeatureProviders.Add(featureProvider);
            var feature = new ViewsFeature();

            // Act
            partManager.PopulateFeature(feature);

            // Assert
            Assert.Collection(feature.ViewDescriptors.OrderBy(f => f.RelativePath, StringComparer.Ordinal),
                view => Assert.Same(item1, view.Item),
                view => Assert.Same(item3, view.Item));
        }

        [Fact]
        public void PopulateFeature_ReturnsEmptySequenceIfNoDynamicAssemblyPartHasViewAssembly()
        {
            // Arrange
            var name = new AssemblyName($"DynamicAssembly-{Guid.NewGuid()}");
            var assembly = AssemblyBuilder.DefineDynamicAssembly(name, AssemblyBuilderAccess.RunAndCollect);

            var partManager = new ApplicationPartManager();
            partManager.ApplicationParts.Add(new AssemblyPart(assembly));
            partManager.FeatureProviders.Add(new ViewsFeatureProvider());
            var feature = new ViewsFeature();

            // Act
            partManager.PopulateFeature(feature);

            // Assert
            Assert.Empty(feature.ViewDescriptors);
        }

        [Fact]
        public void PopulateFeature_ReadsAttributesFromTheCurrentAssembly()
        {
            // Arrange
            var item1 = new RazorCompiledItemAttribute(typeof(string), "mvc.1.0.view", "view");
            var assembly = new AssemblyWithEmptyLocation(
                new RazorViewAttribute[] { new RazorViewAttribute("view", typeof(string)) },
                new RazorCompiledItemAttribute[] { item1 });

            var partManager = new ApplicationPartManager();
            partManager.ApplicationParts.Add(new AssemblyPart(assembly));
            partManager.FeatureProviders.Add(new ViewsFeatureProvider());
            var feature = new ViewsFeature();

            // Act
            partManager.PopulateFeature(feature);

            // Assert
            var descriptor = Assert.Single(feature.ViewDescriptors);
            Assert.Equal(typeof(string), descriptor.Item.Type);
            Assert.Equal("mvc.1.0.view", descriptor.Item.Kind);
            Assert.Equal("view", descriptor.Item.Identifier);
        }

        [Fact]
        public void PopulateFeature_LegacyBehaviorDoesNotFail_IfAssemblyHasEmptyLocation()
        {
            // Arrange
            var assembly = new AssemblyWithEmptyLocation();
            var partManager = new ApplicationPartManager();
            partManager.ApplicationParts.Add(new AssemblyPart(assembly));
            partManager.FeatureProviders.Add(new OverrideViewsFeatureProvider());
            var feature = new ViewsFeature();

            // Act
            partManager.PopulateFeature(feature);

            // Assert
            Assert.Empty(feature.ViewDescriptors);
        }

        [Fact]
        public void PopulateFeature_PreservesOldBehavior_IfGetViewAttributesWasOverriden()
        {
            // Arrange
            var assembly = new AssemblyWithEmptyLocation(
                new RazorViewAttribute[] { new RazorViewAttribute("view", typeof(string)) },
                new RazorCompiledItemAttribute[] { });

            var partManager = new ApplicationPartManager();
            partManager.ApplicationParts.Add(new AssemblyPart(assembly));
            partManager.FeatureProviders.Add(new OverrideViewsFeatureProvider());
            var feature = new ViewsFeature();

            // Act
            partManager.PopulateFeature(feature);

            // Assert
            Assert.Empty(feature.ViewDescriptors);
        }

        internal class OverrideViewsFeatureProvider : ViewsFeatureProvider
        {
            protected override IEnumerable<RazorViewAttribute> GetViewAttributes(AssemblyPart assemblyPart)
                => base.GetViewAttributes(assemblyPart);
        }

        private class TestRazorCompiledItem : RazorCompiledItem
        {
            public TestRazorCompiledItem(Type type, string kind, string identifier, object[] metadata)
            {
                Type = type;
                Kind = kind;
                Identifier = identifier;
                Metadata = metadata;
            }

            public override string Identifier { get; }

            public override string Kind { get; }

            public override IReadOnlyList<object> Metadata { get; }

            public override Type Type { get; }
        }

        private class TestableViewsFeatureProvider : ViewsFeatureProvider
        {
            private readonly Dictionary<AssemblyPart, IEnumerable<RazorViewAttribute>> _attributes;
            private readonly Dictionary<AssemblyPart, IReadOnlyList<RazorCompiledItem>> _items;

            public TestableViewsFeatureProvider(
                Dictionary<AssemblyPart, IReadOnlyList<RazorCompiledItem>> items,
                Dictionary<AssemblyPart, IEnumerable<RazorViewAttribute>> attributes)
            {
                _items = items;
                _attributes = attributes;
            }

            protected override IEnumerable<RazorViewAttribute> GetViewAttributes(AssemblyPart assemblyPart)
            {
                if (_attributes.TryGetValue(assemblyPart, out var attributes))
                {
                    return attributes;
                }

                return Enumerable.Empty<RazorViewAttribute>();
            }

            internal override IReadOnlyList<RazorCompiledItem> LoadItems(AssemblyPart assemblyPart)
            {
                return _items[assemblyPart];
            }
        }

        private class AssemblyWithEmptyLocation : Assembly
        {
            private readonly RazorViewAttribute[] _razorViewAttributes;
            private readonly RazorCompiledItemAttribute[] _razorCompiledItemAttributes;

            public AssemblyWithEmptyLocation()
                : this(Array.Empty<RazorViewAttribute>(), Array.Empty<RazorCompiledItemAttribute>())
            {
            }

            public AssemblyWithEmptyLocation(
                RazorViewAttribute[] razorViewAttributes,
                RazorCompiledItemAttribute[] razorCompiledItemAttributes)
            {
                _razorViewAttributes = razorViewAttributes;
                _razorCompiledItemAttributes = razorCompiledItemAttributes;
            }

            public override string Location => string.Empty;

            public override string FullName => typeof(ViewsFeatureProviderTest).Assembly.FullName;

            public override object[] GetCustomAttributes(Type attributeType, bool inherit)
            {
                if (attributeType == typeof(RazorViewAttribute))
                {
                    return _razorViewAttributes;
                }
                else
                {
                    return _razorCompiledItemAttributes;
                }
            }

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
}
