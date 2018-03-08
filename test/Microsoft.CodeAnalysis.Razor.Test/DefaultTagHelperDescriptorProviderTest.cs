// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Linq;
using System.Reflection;
using Microsoft.AspNetCore.Razor.Language;
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
    }
}
