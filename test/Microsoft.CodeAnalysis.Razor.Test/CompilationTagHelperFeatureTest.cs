// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Linq;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.AspNetCore.Razor.TagHelpers;
using Microsoft.CodeAnalysis.CSharp;
using Moq;
using Xunit;

namespace Microsoft.CodeAnalysis.Razor
{
    public class CompilationTagHelperFeatureTest
    {
        [Fact]
        public void IsValidCompilation_ReturnsFalseIfITagHelperInterfaceCannotBeFound()
        {
            // Arrange
            var references = new[]
            {
                MetadataReference.CreateFromFile(typeof(string).Assembly.Location),
            };
            var compilation = CSharpCompilation.Create("Test", references: references);

            // Act 
            var result = CompilationTagHelperFeature.IsValidCompilation(compilation);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void IsValidCompilation_ReturnsFalseIfSystemStringCannotBeFound()
        {
            // Arrange
            var references = new[]
            {
                MetadataReference.CreateFromFile(typeof(ITagHelper).Assembly.Location),
            };
            var compilation = CSharpCompilation.Create("Test", references: references);

            // Act 
            var result = CompilationTagHelperFeature.IsValidCompilation(compilation);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void IsValidCompilation_ReturnsTrueIfWellKnownTypesAreFound()
        {
            // Arrange
            var references = new[]
            {
                MetadataReference.CreateFromFile(typeof(string).Assembly.Location),
                MetadataReference.CreateFromFile(typeof(ITagHelper).Assembly.Location),
            };
            var compilation = CSharpCompilation.Create("Test", references: references);

            // Act 
            var result = CompilationTagHelperFeature.IsValidCompilation(compilation);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void GetDescriptors_DoesNotSetCompilation_IfCompilationIsInvalid()
        {
            // Arrange
            Compilation compilation = null;
            var provider = new Mock<ITagHelperDescriptorProvider>();
            provider.Setup(c => c.Execute(It.IsAny<TagHelperDescriptorProviderContext>()))
                .Callback<TagHelperDescriptorProviderContext>(c => compilation = c.GetCompilation())
                .Verifiable();

            var engine = RazorProjectEngine.Create(
                configure =>
                {
                    configure.Features.Add(new DefaultMetadataReferenceFeature());
                    configure.Features.Add(provider.Object);
                    configure.Features.Add(new CompilationTagHelperFeature());
                });

            var feature = engine.EngineFeatures.OfType<CompilationTagHelperFeature>().First();

            // Act 
            var result = feature.GetDescriptors();

            // Assert
            Assert.Empty(result);
            provider.Verify();
            Assert.Null(compilation);
        }

        [Fact]
        public void GetDescriptors_SetsCompilation_IfCompilationIsValid()
        {
            // Arrange
            Compilation compilation = null;
            var provider = new Mock<ITagHelperDescriptorProvider>();
            provider.Setup(c => c.Execute(It.IsAny<TagHelperDescriptorProviderContext>()))
                .Callback<TagHelperDescriptorProviderContext>(c => compilation = c.GetCompilation())
                .Verifiable();

            var references = new[]
            {
                MetadataReference.CreateFromFile(typeof(string).Assembly.Location),
                MetadataReference.CreateFromFile(typeof(ITagHelper).Assembly.Location),
            };

            var engine = RazorProjectEngine.Create(
                configure =>
                {
                    configure.Features.Add(new DefaultMetadataReferenceFeature { References = references });
                    configure.Features.Add(provider.Object);
                    configure.Features.Add(new CompilationTagHelperFeature());
                });

            var feature = engine.EngineFeatures.OfType<CompilationTagHelperFeature>().First();
            
            // Act 
            var result = feature.GetDescriptors();

            // Assert
            Assert.Empty(result);
            provider.Verify();
            Assert.NotNull(compilation);
        }
    }
}
