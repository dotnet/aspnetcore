// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Razor.Language.Components;
using Xunit;

namespace Microsoft.AspNetCore.Razor.Language.IntegrationTests
{
    public class ComponentDiscoveryIntegrationTest : RazorIntegrationTestBase
    {
        internal override string FileKind => FileKinds.Component;

        internal override bool UseTwoPhaseCompilation => true;
        
        [Fact]
        public void ComponentDiscovery_CanFindComponent_DefinedinCSharp()
        {
            // Arrange
            AdditionalSyntaxTrees.Add(Parse(@"
using Microsoft.AspNetCore.Components;

namespace Test
{
    public class MyComponent : ComponentBase
    {
    }
}
"));

            // Act
            var result = CompileToCSharp(string.Empty);

            // Assert
            var bindings = result.CodeDocument.GetTagHelperContext();
            Assert.Contains(bindings.TagHelpers, t => t.Name == "Test.MyComponent");
        }

        [Fact]
        public void ComponentDiscovery_CanFindComponent_WithNamespace_DefinedinCSharp()
        {
            // Arrange
            AdditionalSyntaxTrees.Add(Parse(@"
using Microsoft.AspNetCore.Components;

namespace Test.AnotherNamespace
{
    public class MyComponent : ComponentBase
    {
    }
}
"));

            // Act
            var result = CompileToCSharp(string.Empty);

            // Assert
            var bindings = result.CodeDocument.GetTagHelperContext();

            Assert.Contains(bindings.TagHelpers, t =>
            {
                return t.Name == "Test.AnotherNamespace.MyComponent" &&
                    t.IsComponentFullyQualifiedNameMatch();
            });

            Assert.DoesNotContain(bindings.TagHelpers, t =>
            {
                return t.Name == "Test.AnotherNamespace.MyComponent" &&
                    !t.IsComponentFullyQualifiedNameMatch();
            });
        }

        [Fact]
        public void ComponentDiscovery_CanFindComponent_DefinedinCshtml()
        {
            // Arrange

            // Act
            var result = CompileToCSharp("UniqueName.cshtml", string.Empty);

            // Assert
            var bindings = result.CodeDocument.GetTagHelperContext();
            Assert.Contains(bindings.TagHelpers, t => t.Name == "Test.UniqueName");
        }

        [Fact]
        public void ComponentDiscovery_CanFindComponent_WithTypeParameter()
        {
            // Arrange

            // Act
            var result = CompileToCSharp("UniqueName.cshtml", @"
@typeparam TItem
@functions {
    [Parameter] public TItem Item { get; set; }
}");

            // Assert
            var bindings = result.CodeDocument.GetTagHelperContext();
            Assert.Contains(bindings.TagHelpers, t => t.Name == "Test.UniqueName<TItem>");
        }

        [Fact]
        public void ComponentDiscovery_CanFindComponent_WithMultipleTypeParameters()
        {
            // Arrange

            // Act
            var result = CompileToCSharp("UniqueName.cshtml", @"
@typeparam TItem1
@typeparam TItem2
@typeparam TItem3
@functions {
    [Parameter] public TItem1 Item { get; set; }
}");

            // Assert
            var bindings = result.CodeDocument.GetTagHelperContext();
            Assert.Contains(bindings.TagHelpers, t => t.Name == "Test.UniqueName<TItem1, TItem2, TItem3>");
        }
    }
}
