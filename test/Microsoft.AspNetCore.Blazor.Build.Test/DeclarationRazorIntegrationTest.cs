// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Reflection;
using System.Text;
using Microsoft.AspNetCore.Blazor.Components;
using Microsoft.AspNetCore.Blazor.Razor;
using Microsoft.AspNetCore.Blazor.Test.Helpers;
using Microsoft.AspNetCore.Razor.Language;
using Xunit;

namespace Microsoft.AspNetCore.Blazor.Build.Test
{
    public class DeclarationRazorIntegrationTest : RazorIntegrationTestBase
    {
        internal override RazorConfiguration Configuration => BlazorExtensionInitializer.DeclarationConfiguration;

        [Fact]
        public void DeclarationConfiguration_IncludesFunctions()
        {
            // Arrange & Act
            var component = CompileToComponent(@"
@functions {
    public string Value { get; set; }
}");

            // Assert
            var property = component.GetType().GetProperty("Value");
            Assert.NotNull(property);
            Assert.Same(typeof(string), property.PropertyType);
        }

        [Fact]
        public void DeclarationConfiguration_IncludesInject()
        {
            // Arrange & Act
            var component = CompileToComponent(@"
@inject string Value
");

            // Assert
            var property = component.GetType().GetProperty("Value", BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.NotNull(property);
            Assert.Same(typeof(string), property.PropertyType);
        }

        [Fact]
        public void DeclarationConfiguration_IncludesUsings()
        {
            // Arrange & Act
            var component = CompileToComponent(@"
@using System.Text
@inject StringBuilder Value
");

            // Assert
            var property = component.GetType().GetProperty("Value", BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.NotNull(property);
            Assert.Same(typeof(StringBuilder), property.PropertyType);
        }

        [Fact]
        public void DeclarationConfiguration_IncludesInherits()
        {
            // Arrange & Act
            var component = CompileToComponent($@"
@inherits {FullTypeName<BaseClass>()}
");

            // Assert
            Assert.Same(typeof(BaseClass), component.GetType().BaseType);
        }

        [Fact]
        public void DeclarationConfiguration_IncludesImplements()
        {
            // Arrange & Act
            var component = CompileToComponent($@"
@implements {FullTypeName<IDoCoolThings>()}
");

            // Assert
            var type = component.GetType();
            Assert.Contains(typeof(IDoCoolThings), component.GetType().GetInterfaces());
        }

        [Fact]
        public void DeclarationConfiguration_RenderMethodIsEmpty()
        {
            // Arrange & Act
            var component = CompileToComponent(@"
<html>
@{ var message = ""hi""; }
<span class=""@(5 + 7)"">@message</span>
</html>
");

            var frames = GetRenderTree(component);

            // Assert
            Assert.Empty(frames);
        }

        public class BaseClass : BlazorComponent
        {
        }

        public interface IDoCoolThings
        {
        }
    }
}
