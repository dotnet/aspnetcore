// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace Microsoft.AspNetCore.Razor.Language;

public class DefaultRazorCodeDocumentTest
{
    [Fact]
    public void Ctor()
    {
        // Arrange
        var source = TestRazorSourceDocument.Create();

        var imports = new RazorSourceDocument[]
        {
                TestRazorSourceDocument.Create(),
        };

        // Act
        var code = new DefaultRazorCodeDocument(source, imports);

        // Assert
        Assert.Same(source, code.Source);
        Assert.NotNull(code.Items);

        Assert.NotSame(imports, code.Imports);
        Assert.Collection(imports, d => Assert.Same(imports[0], d));
    }

    [Fact]
    public void Ctor_AllowsNullForImports()
    {
        // Arrange
        var source = TestRazorSourceDocument.Create();

        // Act
        var code = new DefaultRazorCodeDocument(source, imports: null);

        // Assert
        Assert.Same(source, code.Source);
        Assert.NotNull(code.Items);
        Assert.Empty(code.Imports);
    }
}
