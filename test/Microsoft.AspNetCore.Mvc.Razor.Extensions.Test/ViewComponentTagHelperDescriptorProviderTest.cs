// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Linq;
using System.Reflection;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Razor;
using Xunit;

namespace Microsoft.AspNetCore.Mvc.Razor.Extensions
{
    // This is just a basic integration test. There are detailed tests for the VCTH visitor and descriptor factory.
    public class ViewComponentTagHelperDescriptorProviderTest
    {
        private static readonly Assembly _assembly = typeof(ViewComponentTagHelperDescriptorProviderTest).GetTypeInfo().Assembly;

        [Fact]
        public void DescriptorProvider_FindsVCTH()
        {
            // Arrange
            var code = @"
        public class StringParameterViewComponent
        {
            public string Invoke(string foo, string bar) => null;
        }
";

            var testCompilation = TestCompilation.Create(_assembly, CSharpSyntaxTree.ParseText(code));

            var context = TagHelperDescriptorProviderContext.Create();
            context.SetCompilation(testCompilation);

            var provider = new ViewComponentTagHelperDescriptorProvider()
            {
                Engine = RazorEngine.CreateEmpty(b => { }),
                ForceEnabled = true,
            };

            var expectedDescriptor = TagHelperDescriptorBuilder.Create(
                "__Generated__StringParameterViewComponentTagHelper",
                TestCompilation.AssemblyName)
                .DisplayName("StringParameterViewComponentTagHelper")
                .TagMatchingRule(rule =>
                    rule
                    .RequireTagName("vc:string-parameter")
                    .RequireAttribute(attribute => attribute.Name("foo"))
                    .RequireAttribute(attribute => attribute.Name("bar")))
                .BindAttribute(attribute =>
                    attribute
                    .Name("foo")
                    .PropertyName("foo")
                    .TypeName(typeof(string).FullName)
                    .DisplayName("string StringParameterViewComponentTagHelper.foo"))
                .BindAttribute(attribute =>
                    attribute
                    .Name("bar")
                    .PropertyName("bar")
                    .TypeName(typeof(string).FullName)
                    .DisplayName("string StringParameterViewComponentTagHelper.bar"))
                .AddMetadata(ViewComponentTypes.ViewComponentNameKey, "StringParameter")
                .Build();

            // Act
            provider.Execute(context);

            // Assert
            var descriptor = context.Results.FirstOrDefault(d => TagHelperDescriptorComparer.CaseSensitive.Equals(d, expectedDescriptor));
            Assert.NotNull(descriptor);
        }
    }
}
