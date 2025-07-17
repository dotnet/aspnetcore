// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net.Http;
using System.Text.Json.Nodes;

namespace Microsoft.AspNetCore.OpenApi.SourceGenerators.Tests;

[UsesVerify]
public class XmlCommentDocumentationIdTests
{
    [Fact]
    public async Task CanMergeXmlCommentsWithDifferentDocumentationIdFormats()
    {
        // This test verifies that XML comments from referenced assemblies (without return type suffix)
        // are properly merged with in-memory symbols (with return type suffix)
        var source = """
using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using ReferencedLibrary;

var builder = WebApplication.CreateBuilder();

builder.Services.AddOpenApi();

var app = builder.Build();

app.MapPost("/test-method", ReferencedLibrary.TestApi.TestMethod);

app.Run();
""";

        var referencedLibrarySource = """
using System;
using System.Threading.Tasks;

namespace ReferencedLibrary;

public static class TestApi
{
    /// <summary>
    /// This method should have its XML comment merged properly.
    /// </summary>
    /// <param name="id">The identifier for the test.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public static Task TestMethod(int id)
    {
        return Task.CompletedTask;
    }
}
""";

        var references = new Dictionary<string, List<string>>
        {
            { "ReferencedLibrary", [referencedLibrarySource] }
        };

        var generator = new XmlCommentGenerator();
        await SnapshotTestHelper.Verify(source, generator, references, out var compilation, out var additionalAssemblies);
        await SnapshotTestHelper.VerifyOpenApi(compilation, additionalAssemblies, document =>
        {
            var path = document.Paths["/test-method"].Operations[HttpMethod.Post];
            
            // Verify that the XML comment from the referenced library was properly merged
            // This would fail before the fix because the documentation IDs didn't match
            Assert.NotNull(path.Summary);
            Assert.Equal("This method should have its XML comment merged properly.", path.Summary);
            
            // Verify the parameter comment is also available
            Assert.NotNull(path.Parameters);
            Assert.Single(path.Parameters);
            Assert.Equal("The identifier for the test.", path.Parameters[0].Description);
        });
    }

    [Theory]
    [InlineData("M:Sample.MyMethod(System.Int32)~System.Threading.Tasks.Task", "M:Sample.MyMethod(System.Int32)")]
    [InlineData("M:Sample.MyMethod(System.Int32)", "M:Sample.MyMethod(System.Int32)")]
    [InlineData("M:Sample.op_Implicit(System.Int32)~Sample.MyClass", "M:Sample.op_Implicit(System.Int32)~Sample.MyClass")]
    [InlineData("M:Sample.op_Explicit(System.Int32)~Sample.MyClass", "M:Sample.op_Explicit(System.Int32)~Sample.MyClass")]
    [InlineData("T:Sample.MyClass", "T:Sample.MyClass")]
    [InlineData("P:Sample.MyClass.MyProperty", "P:Sample.MyClass.MyProperty")]
    public void NormalizeDocId_ReturnsExpectedResult(string input, string expected)
    {
        var result = XmlCommentGenerator.NormalizeDocId(input);
        Assert.Equal(expected, result);
    }
}