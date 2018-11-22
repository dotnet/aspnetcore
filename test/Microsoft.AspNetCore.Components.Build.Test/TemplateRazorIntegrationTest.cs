// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Xunit;

namespace Microsoft.AspNetCore.Components.Build.Test
{
    public class TemplateRazorIntegrationTest : RazorIntegrationTestBase
    {
        // Razor doesn't parse this as a template, we don't need much special handling for
        // it because it will just be invalid in general.
        [Fact]
        public void Template_ImplicitExpressionInMarkupAttribute_CreatesDiagnostic()
        {
            // Arrange

            // Act
            var generated = CompileToCSharp(@"<div attr=""@<div></div>"" />");

            // Assert
            var diagnostic = Assert.Single(generated.Diagnostics);
            Assert.Equal("RZ1005", diagnostic.Id);
        }

        [Fact]
        public void Template_ExplicitExpressionInMarkupAttribute_CreatesDiagnostic()
        {
            // Arrange

            // Act
            var generated = CompileToCSharp(@"<div attr=""@(@<div></div>)"" />");

            // Assert
            var diagnostic = Assert.Single(generated.Diagnostics);
            Assert.Equal("BL9994", diagnostic.Id);
        }

        // Razor doesn't parse this as a template, we don't need much special handling for
        // it because it will just be invalid in general.
        [Fact]
        public void Template_ImplicitExpressionInComponentAttribute_CreatesDiagnostic()
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
            var generated = CompileToCSharp(@"<MyComponent attr=""@<div></div>"" />");

            // Assert
            var diagnostic = Assert.Single(generated.Diagnostics);
            Assert.Equal("RZ1005", diagnostic.Id);
        }

        [Fact]
        public void Template_ExplicitExpressionInComponentAttribute_CreatesDiagnostic()
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
            var generated = CompileToCSharp(@"<MyComponent attr=""@(@<div></div>)"" />");

            // Assert
            var diagnostic = Assert.Single(generated.Diagnostics);
            Assert.Equal("BL9994", diagnostic.Id);
        }

        [Fact]
        public void Template_ExplicitExpressionInRef_CreatesDiagnostic()
        {
            // Arrange

            // Act
            var generated = CompileToCSharp(@"<div ref=""@(@<div></div>)"" />");

            // Assert
            var diagnostic = Assert.Single(generated.Diagnostics);
            Assert.Equal("BL9994", diagnostic.Id);
        }


        [Fact]
        public void Template_ExplicitExpressionInBind_CreatesDiagnostic()
        {
            // Arrange

            // Act
            var generated = CompileToCSharp(@"<input type=""text"" bind=""@(@<div></div>)"" />");

            // Assert
            var diagnostic = Assert.Single(generated.Diagnostics);
            Assert.Equal("BL9994", diagnostic.Id);
        }

        [Fact]
        public void Template_ExplicitExpressionInEventHandler_CreatesDiagnostic()
        {
            // Arrange

            // Act
            var generated = CompileToCSharp(@"<input type=""text"" onchange=""@(@<div></div>)"" />");

            // Assert
            var diagnostic = Assert.Single(generated.Diagnostics);
            Assert.Equal("BL9994", diagnostic.Id);
        }
    }
}