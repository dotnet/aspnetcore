// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.IO;
using System.Text;
using Microsoft.AspNetCore.Components.Test.Helpers;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.CodeAnalysis.CSharp;
using Xunit;

namespace Microsoft.AspNetCore.Components.Build.Test
{
    public class ComponentDiscoveryRazorIntegrationTest : RazorIntegrationTestBase
    {
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
            var result = CompileToCSharp("@addTagHelper *, TestAssembly");

            // Assert
            var bindings = result.CodeDocument.GetTagHelperContext();
            Assert.Single(bindings.TagHelpers, t => t.Name == "Test.MyComponent");
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
            var result = CompileToCSharp("@addTagHelper *, TestAssembly");

            // Assert
            var bindings = result.CodeDocument.GetTagHelperContext();
            Assert.Single(bindings.TagHelpers, t => t.Name == "Test.AnotherNamespace.MyComponent");
        }

        [Fact]
        public void ComponentDiscovery_CanFindComponent_DefinedinCshtml()
        {
            // Arrange

            // Act
            var result = CompileToCSharp("UniqueName.cshtml", "@addTagHelper *, TestAssembly");

            // Assert
            var bindings = result.CodeDocument.GetTagHelperContext();
            Assert.Single(bindings.TagHelpers, t => t.Name == "Test.UniqueName");
        }

        [Fact]
        public void ComponentDiscovery_CanFindComponent_BuiltIn()
        {
            // Arrange

            // Act
            var result = CompileToCSharp("@addTagHelper *, Microsoft.AspNetCore.Components");

            // Assert
            var bindings = result.CodeDocument.GetTagHelperContext();
            Assert.Single(bindings.TagHelpers, t => t.Name == "Microsoft.AspNetCore.Components.Routing.NavLink");
        }

        [Fact]
        public void ComponentDiscovery_CanFindComponent_WithTypeParameter()
        {
            // Arrange

            // Act
            var result = CompileToCSharp("UniqueName.cshtml", @"
@addTagHelper *, TestAssembly
@typeparam TItem
@functions {
    [Parameter] TItem Item { get; set; }
}");

            // Assert
            var bindings = result.CodeDocument.GetTagHelperContext();
            Assert.Single(bindings.TagHelpers, t => t.Name == "Test.UniqueName<TItem>");
        }

        [Fact]
        public void ComponentDiscovery_CanFindComponent_WithMultipleTypeParameters()
        {
            // Arrange

            // Act
            var result = CompileToCSharp("UniqueName.cshtml", @"
@addTagHelper *, TestAssembly
@typeparam TItem1
@typeparam TItem2
@typeparam TItem3
@functions {
    [Parameter] TItem1 Item { get; set; }
}");

            // Assert
            var bindings = result.CodeDocument.GetTagHelperContext();
            Assert.Single(bindings.TagHelpers, t => t.Name == "Test.UniqueName<TItem1, TItem2, TItem3>");
        }
    }
}
