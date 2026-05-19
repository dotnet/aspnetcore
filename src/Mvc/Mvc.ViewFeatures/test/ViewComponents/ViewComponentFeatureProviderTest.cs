// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Reflection;
using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.AspNetCore.Mvc.ViewComponents;
using Microsoft.AspNetCore.Mvc.ViewFeatures.ViewComponents.ViewComponentsFeatureTest;

namespace Microsoft.AspNetCore.Mvc.ViewFeatures.ViewComponents
{
    public class ViewComponentFeatureProviderTest
    {
        [Fact]
        public void GetDescriptor_DefaultConventions()
        {
            // Arrange
            var manager = new ApplicationPartManager();
            manager.ApplicationParts.Add(new TestPart(typeof(ConventionsViewComponent)));
            manager.FeatureProviders.Add(new ViewComponentFeatureProvider());

            var feature = new ViewComponentFeature();

            // Act
            manager.PopulateFeature(feature);

            // Assert
            Assert.Equal(new[] { typeof(ConventionsViewComponent).GetTypeInfo() }, feature.ViewComponents.ToArray());
        }

        [Fact]
        public void GetDescriptor_WithAttribute()
        {
            // Arrange
            var manager = new ApplicationPartManager();
            manager.ApplicationParts.Add(new TestPart(typeof(AttributeViewComponent)));
            manager.FeatureProviders.Add(new ViewComponentFeatureProvider());

            var feature = new ViewComponentFeature();

            // Act
            manager.PopulateFeature(feature);

            // Assert
            Assert.Equal(new[] { typeof(AttributeViewComponent).GetTypeInfo() }, feature.ViewComponents.ToArray());
        }

        private class TestPart : ApplicationPart, IApplicationPartTypeProvider
        {
            public TestPart(params Type[] types)
            {
                Types = types.Select(t => t.GetTypeInfo());
            }

            public override string Name => "Test";

            public IEnumerable<TypeInfo> Types { get; }
        }
    }
}

// These tests need to be public for the test to be valid
namespace Microsoft.AspNetCore.Mvc.ViewFeatures.ViewComponents.ViewComponentsFeatureTest
{
    public class ConventionsViewComponent
    {
        public string Invoke() => "Hello world";
    }

    [ViewComponent(Name = "AttributesAreGreat")]
    public class AttributeViewComponent
    {
        public Task<string> InvokeAsync() => Task.FromResult("Hello world");
    }
}
