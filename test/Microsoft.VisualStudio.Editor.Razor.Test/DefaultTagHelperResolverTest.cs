// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Reflection;
using Microsoft.AspNetCore.Razor.TagHelpers;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Xunit;

namespace Microsoft.VisualStudio.Editor.Razor
{
    public class DefaultTagHelperResolverTest
    {
        private static readonly Assembly Assembly = typeof(DefaultTagHelperResolverTest).GetTypeInfo().Assembly;

        [Fact]
        public void GetTagHelpers_DiscoversViewComponentTagHelpers()
        {
            // Arrange
            var code = @"
public class TestViewComponent
{
    public string Invoke(string foo, string bar) => null;
}";
            var syntaxTree = CSharpSyntaxTree.ParseText(code);
            var compilation = TestCompilation.Create(Assembly, syntaxTree);
            var tagHelperResolver = new DefaultTagHelperResolver()
            {
                ForceEnableViewComponentDiscovery = true
            };

            // Act
            var result = tagHelperResolver.GetTagHelpers(compilation);

            // Assert
            Assert.Empty(result.Diagnostics);
            Assert.Equal(1, result.Descriptors.Count);
        }

        [Fact]
        public void GetTagHelpers_DiscoversTagHelpers()
        {
            // Arrange
            var code = $@"
public class TestTagHelper : {typeof(TagHelper).FullName}
{{
}}";
            var syntaxTree = CSharpSyntaxTree.ParseText(code);
            var compilation = TestCompilation.Create(Assembly, syntaxTree);
            var tagHelperResolver = new DefaultTagHelperResolver()
            {
                ForceEnableViewComponentDiscovery = true
            };

            // Act
            var result = tagHelperResolver.GetTagHelpers(compilation);

            // Assert
            Assert.Empty(result.Diagnostics);
            Assert.Equal(1, result.Descriptors.Count);
        }
    }
}
