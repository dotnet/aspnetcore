// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.IO;
using System.Text;
using Microsoft.AspNetCore.Blazor.Test.Helpers;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.CodeAnalysis.CSharp;
using Xunit;

namespace Microsoft.AspNetCore.Blazor.Build.Test
{
    public class ComponentDiscoveryRazorIntegrationTest : RazorIntegrationTestBase
    {
        internal override bool UseTwoPhaseCompilation => true;

        [Fact]
        public void ComponentDiscovery_CanFindComponent_DefinedinCSharp()
        {
            // Arrange
            AdditionalSyntaxTrees.Add(Parse(@"
using Microsoft.AspNetCore.Blazor.Components;

namespace Test
{
    public class MyComponent : BlazorComponent
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
using Microsoft.AspNetCore.Blazor.Components;

namespace Test.AnotherNamespace
{
    public class MyComponent : BlazorComponent
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
            var result = CompileToCSharp("@addTagHelper *, Microsoft.AspNetCore.Blazor");

            // Assert
            var bindings = result.CodeDocument.GetTagHelperContext();
            Assert.Single(bindings.TagHelpers, t => t.Name == "Microsoft.AspNetCore.Blazor.Routing.NavLink");
        }
    }
}
