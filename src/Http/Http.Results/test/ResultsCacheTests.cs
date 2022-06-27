// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Http.HttpResults;

using Mono.TextTemplating;

public class ResultsCacheTests
{
    [Fact]
    public void GeneratedCodeIsUpToDate()
    {
        var currentContentPath = Path.Combine(AppContext.BaseDirectory, "Shared", "GeneratedContent", "ResultsCache.StatusCodes.cs");
        var templatePath = Path.Combine(AppContext.BaseDirectory, "Shared", "GeneratedContent", "ResultsCache.StatusCodes.tt");

        var generator = new TemplateGenerator();
        var compiledTemplate = generator.CompileTemplate(File.ReadAllText(templatePath));

        var generatedContent = compiledTemplate.Process();
        var currentContent = File.ReadAllText(currentContentPath);

        Assert.Equal(currentContent, generatedContent);
    }
}
