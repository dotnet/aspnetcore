// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.IO;
using System.Linq;
using System.Reflection;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.AspNetCore.Razor.TagHelpers;
using Microsoft.CodeAnalysis.CSharp;
using Xunit;

namespace Microsoft.CodeAnalysis.Razor
{
    public class DefaultTagHelperDescriptorProviderTest
    {
        private static readonly Assembly _assembly = typeof(DefaultTagHelperDescriptorProviderTest).GetTypeInfo().Assembly;

        [Fact]
        public void Execute_DoesNotAddEditorBrowsableNeverDescriptorsAtDesignTime()
        {
            // Arrange
            var editorBrowsableTypeName = "Microsoft.CodeAnalysis.Razor.Workspaces.Test.EditorBrowsableTagHelper";
            var compilation = TestCompilation.Create(_assembly);
            var descriptorProvider = new DefaultTagHelperDescriptorProvider();

            var context = TagHelperDescriptorProviderContext.Create();
            context.SetCompilation(compilation);
            context.ExcludeHidden = true;

            // Act 
            descriptorProvider.Execute(context);

            // Assert
            Assert.NotNull(compilation.GetTypeByMetadataName(editorBrowsableTypeName));
            var nullDescriptors = context.Results.Where(descriptor => descriptor == null);
            Assert.Empty(nullDescriptors);
            var editorBrowsableDescriptor = context.Results.Where(descriptor => descriptor.GetTypeName() == editorBrowsableTypeName);
            Assert.Empty(editorBrowsableDescriptor);
        }

        [Fact]
        public void Execute_NoOpsIfCompilationSymbolIsNotSet()
        {
            // Arrange
            var descriptorProvider = new DefaultTagHelperDescriptorProvider();

            var context = TagHelperDescriptorProviderContext.Create();

            // Act 
            descriptorProvider.Execute(context);

            // Assert
            Assert.Empty(context.Results);
        }

        [Fact]
        public void Execute_NoOpsIfTagHelperInterfaceCannotBeFound()
        {
            // Arrange
            var references = new[]
            {
                MetadataReference.CreateFromFile(typeof(string).Assembly.Location),
            };
            var compilation = CSharpCompilation.Create("Test", references: references);

            var descriptorProvider = new DefaultTagHelperDescriptorProvider();

            var context = TagHelperDescriptorProviderContext.Create();
            context.SetCompilation(compilation);

            // Act 
            descriptorProvider.Execute(context);

            // Assert
            Assert.Empty(context.Results);
        }

        [Fact]
        public void Execute_NoOpsIfStringCannotBeFound()
        {
            // Arrange
            var references = new[]
            {
                MetadataReference.CreateFromFile(typeof(ITagHelper).Assembly.Location),
            };
            var compilation = CSharpCompilation.Create("Test", references: references);

            var descriptorProvider = new DefaultTagHelperDescriptorProvider();

            var context = TagHelperDescriptorProviderContext.Create();
            context.SetCompilation(compilation);

            // Act 
            descriptorProvider.Execute(context);

            // Assert
            Assert.Empty(context.Results);
        }

        [Fact]
        public void Execute_DiscoversTagHelpersFromCompilation()
        {
            // Arrange
            var references = new[]
            {
                MetadataReference.CreateFromFile(typeof(string).Assembly.Location),
                MetadataReference.CreateFromFile(typeof(ITagHelper).Assembly.Location),
            };
            var projectDirectory = TestProject.GetProjectDirectory(GetType());
            var tagHelperContent = File.ReadAllText(Path.Combine(projectDirectory, "TagHelperTypes.cs"));
            var syntaxTree = CSharpSyntaxTree.ParseText(tagHelperContent);
            var compilation = CSharpCompilation.Create("Test", references: references, syntaxTrees: new[] { syntaxTree });

            var descriptorProvider = new DefaultTagHelperDescriptorProvider();

            var context = TagHelperDescriptorProviderContext.Create();
            context.SetCompilation(compilation);

            // Act 
            descriptorProvider.Execute(context);

            // Assert
            Assert.Collection(
                context.Results.OrderBy(r => r.Name),
                tagHelper => Assert.Equal(typeof(Valid_InheritedTagHelper).FullName, tagHelper.Name),
                tagHelper => Assert.Equal(typeof(Valid_PlainTagHelper).FullName, tagHelper.Name));
        }
    }
}
