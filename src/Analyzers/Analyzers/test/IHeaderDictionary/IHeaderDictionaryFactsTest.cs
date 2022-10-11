// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.CodeAnalysis;

namespace Microsoft.AspNetCore.Analyzers.IHeaderDictionary;

public class IHeaderDictionaryFactsTest
{
    private const string IHeaderDictionaryWithAddSource = @"
using Microsoft.AspNetCore.Http;

namespace IHeaderDictionaryFactsTest;

public class Test
{
    public void Method()
    {
        var context = new DefaultHttpContext();
        context.Request.Headers.Add(""Accept"", ""text/html"");
    }
}
";

    [Fact]
    public void IsIHeaderDictionary_FindsIHeaderDictionaryType()
    {
        // Arrange
        var compilation = TestCompilation.Create(IHeaderDictionaryWithAddSource);

        var symbols = new IHeaderDictionarySymbols(compilation);
        var type = compilation.GetTypeByMetadataName("Microsoft.AspNetCore.Http.IHeaderDictionary");

        // Act
        var result = IHeaderDictionaryFacts.IsIHeaderDictionary(symbols, type);

        // Arrange
        Assert.True(result);
    }

    [Fact]
    public void IsIHeaderDictionary_RejectsNonIHeaderDictionaryType()
    {
        // Arrange
        var compilation = TestCompilation.Create(IHeaderDictionaryWithAddSource);

        var symbols = new IHeaderDictionarySymbols(compilation);
        var type = compilation.GetTypeByMetadataName("Microsoft.AspNetCore.Http.HttpContext.DefaultHttpContext");

        // Act
        var result = IHeaderDictionaryFacts.IsIHeaderDictionary(symbols, type);

        // Arrange
        Assert.False(result);
    }

    [Fact]
    public void IsAdd_FindsAddMethod()
    {
        // Arrange
        var compilation = TestCompilation.Create(IHeaderDictionaryWithAddSource);

        var symbols = new IHeaderDictionarySymbols(compilation);
        var method = compilation.GetTypeByMetadataName("Microsoft.AspNetCore.Http.IHeaderDictionary").Interfaces
            .Single(i => i.Name == "IDictionary")
            .GetMembers("Add")
            .Cast<IMethodSymbol>()
            .Single();

        // Act
        var result = IHeaderDictionaryFacts.IsAdd(symbols, method);

        // Arrange
        Assert.True(result);
    }

    [Fact]
    public void IsAdd_RejectsNonAddMethod()
    {
        // Arrange
        var compilation = TestCompilation.Create(IHeaderDictionaryWithAddSource);

        var symbols = new IHeaderDictionarySymbols(compilation);
        var method = compilation.GetTypeByMetadataName("Microsoft.AspNetCore.Http.IHeaderDictionary").Interfaces
            .Single(i => i.Name == "IDictionary")
            .GetMembers("Remove")
            .Cast<IMethodSymbol>()
            .Single();

        // Act
        var result = IHeaderDictionaryFacts.IsAdd(symbols, method);

        // Arrange
        Assert.False(result);
    }
}
