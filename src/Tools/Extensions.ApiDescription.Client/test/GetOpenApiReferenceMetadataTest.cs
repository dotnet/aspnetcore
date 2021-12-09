// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Build.Utilities;
using Xunit;

namespace Microsoft.Extensions.ApiDescription.Client;

public class GetOpenApiReferenceMetadataTest
{
    [Fact]
    public void Execute_AddsExpectedMetadata()
    {
        // Arrange
        var identity = Path.Combine("TestProjects", "files", "NSwag.json");
        var @namespace = "Console.Client";
        var outputPath = Path.Combine("obj", "NSwagClient.cs");
        var inputMetadata = new Dictionary<string, string> { { "CodeGenerator", "NSwagCSharp" } };
        var task = new GetOpenApiReferenceMetadata
        {
            Extension = ".cs",
            Inputs = new[] { new TaskItem(identity, inputMetadata) },
            Namespace = @namespace,
            OutputDirectory = "obj",
        };

        var expectedMetadata = new SortedDictionary<string, string>(StringComparer.Ordinal)
            {
                { "ClassName", "NSwagClient" },
                { "CodeGenerator", "NSwagCSharp" },
                { "FirstForGenerator", "true" },
                { "Namespace", @namespace },
                { "OriginalItemSpec", identity },
                { "OutputPath", outputPath },
                {
                    "SerializedMetadata",
                    $"Identity={identity}|FirstForGenerator=true|" +
                    $"CodeGenerator=NSwagCSharp|OutputPath={outputPath}|Namespace={@namespace}|" +
                    $"OriginalItemSpec={identity}|ClassName=NSwagClient"
                },
            };

        // Act
        var result = task.Execute();

        // Assert
        Assert.True(result);
        Assert.False(task.Log.HasLoggedErrors);
        var output = Assert.Single(task.Outputs);
        Assert.Equal(identity, output.ItemSpec);
        var metadata = Assert.IsAssignableFrom<IDictionary<string, string>>(output.CloneCustomMetadata());

        // The dictionary CloneCustomMetadata returns doesn't provide a useful KeyValuePair enumerator.
        var orderedMetadata = new SortedDictionary<string, string>(StringComparer.Ordinal);
        foreach (var key in metadata.Keys)
        {
            orderedMetadata.Add(key, metadata[key]);
        }

        Assert.Equal(expectedMetadata, orderedMetadata);
    }

    [Fact]
    public void Execute_DoesNotOverrideClassName()
    {
        // Arrange
        var identity = Path.Combine("TestProjects", "files", "NSwag.json");
        var className = "ThisIsClassy";
        var @namespace = "Console.Client";
        var outputPath = Path.Combine("obj", $"NSwagClient.cs");
        var inputMetadata = new Dictionary<string, string>
            {
                { "CodeGenerator", "NSwagCSharp" },
                { "ClassName", className },
            };

        var task = new GetOpenApiReferenceMetadata
        {
            Extension = ".cs",
            Inputs = new[] { new TaskItem(identity, inputMetadata) },
            Namespace = @namespace,
            OutputDirectory = "obj",
        };

        var expectedMetadata = new SortedDictionary<string, string>(StringComparer.Ordinal)
            {
                { "ClassName", className },
                { "CodeGenerator", "NSwagCSharp" },
                { "FirstForGenerator", "true" },
                { "Namespace", @namespace },
                { "OriginalItemSpec", identity },
                { "OutputPath", outputPath },
                {
                    "SerializedMetadata",
                    $"Identity={identity}|FirstForGenerator=true|" +
                    $"CodeGenerator=NSwagCSharp|OutputPath={outputPath}|Namespace={@namespace}|" +
                    $"OriginalItemSpec={identity}|ClassName={className}"
                },
            };

        // Act
        var result = task.Execute();

        // Assert
        Assert.True(result);
        Assert.False(task.Log.HasLoggedErrors);
        var output = Assert.Single(task.Outputs);
        Assert.Equal(identity, output.ItemSpec);
        var metadata = Assert.IsAssignableFrom<IDictionary<string, string>>(output.CloneCustomMetadata());

        // The dictionary CloneCustomMetadata returns doesn't provide a useful KeyValuePair enumerator.
        var orderedMetadata = new SortedDictionary<string, string>(StringComparer.Ordinal);
        foreach (var key in metadata.Keys)
        {
            orderedMetadata.Add(key, metadata[key]);
        }

        Assert.Equal(expectedMetadata, orderedMetadata);
    }

    [Fact]
    public void Execute_DoesNotOverrideNamespace()
    {
        // Arrange
        var defaultNamespace = "Console.Client";
        var identity = Path.Combine("TestProjects", "files", "NSwag.json");
        var @namespace = "NotConsole.NotClient";
        var outputPath = Path.Combine("obj", "NSwagClient.cs");
        var inputMetadata = new Dictionary<string, string>
            {
                { "CodeGenerator", "NSwagCSharp" },
                { "Namespace", @namespace },
            };

        var task = new GetOpenApiReferenceMetadata
        {
            Extension = ".cs",
            Inputs = new[] { new TaskItem(identity, inputMetadata) },
            Namespace = defaultNamespace,
            OutputDirectory = "obj",
        };

        var expectedMetadata = new SortedDictionary<string, string>(StringComparer.Ordinal)
            {
                { "ClassName", "NSwagClient" },
                { "CodeGenerator", "NSwagCSharp" },
                { "FirstForGenerator", "true" },
                { "Namespace", @namespace },
                { "OriginalItemSpec", identity },
                { "OutputPath", outputPath },
                {
                    "SerializedMetadata",
                    $"Identity={identity}|FirstForGenerator=true|" +
                    $"CodeGenerator=NSwagCSharp|OutputPath={outputPath}|Namespace={@namespace}|" +
                    $"OriginalItemSpec={identity}|ClassName=NSwagClient"
                },
            };

        // Act
        var result = task.Execute();

        // Assert
        Assert.True(result);
        Assert.False(task.Log.HasLoggedErrors);
        var output = Assert.Single(task.Outputs);
        Assert.Equal(identity, output.ItemSpec);
        var metadata = Assert.IsAssignableFrom<IDictionary<string, string>>(output.CloneCustomMetadata());

        // The dictionary CloneCustomMetadata returns doesn't provide a useful KeyValuePair enumerator.
        var orderedMetadata = new SortedDictionary<string, string>(StringComparer.Ordinal);
        foreach (var key in metadata.Keys)
        {
            orderedMetadata.Add(key, metadata[key]);
        }

        Assert.Equal(expectedMetadata, orderedMetadata);
    }

    [Fact]
    public void Execute_DoesNotOverrideOutputPath_IfRooted()
    {
        // Arrange
        var identity = Path.Combine("TestProjects", "files", "NSwag.json");
        var className = "ThisIsClassy";
        var @namespace = "Console.Client";
        var outputPath = Path.Combine(Path.GetTempPath(), $"{className}.cs");
        var inputMetadata = new Dictionary<string, string>
            {
                { "CodeGenerator", "NSwagCSharp" },
                { "OutputPath", outputPath }
            };

        var task = new GetOpenApiReferenceMetadata
        {
            Extension = ".cs",
            Inputs = new[] { new TaskItem(identity, inputMetadata) },
            Namespace = @namespace,
            OutputDirectory = "bin",
        };

        var expectedMetadata = new SortedDictionary<string, string>(StringComparer.Ordinal)
            {
                { "ClassName", className },
                { "CodeGenerator", "NSwagCSharp" },
                { "FirstForGenerator", "true" },
                { "Namespace", @namespace },
                { "OriginalItemSpec", identity },
                { "OutputPath", outputPath },
                {
                    "SerializedMetadata",
                    $"Identity={identity}|FirstForGenerator=true|" +
                    $"CodeGenerator=NSwagCSharp|OutputPath={outputPath}|Namespace={@namespace}|" +
                    $"OriginalItemSpec={identity}|ClassName={className}"
                },
            };

        // Act
        var result = task.Execute();

        // Assert
        Assert.True(result);
        Assert.False(task.Log.HasLoggedErrors);
        var output = Assert.Single(task.Outputs);
        Assert.Equal(identity, output.ItemSpec);
        var metadata = Assert.IsAssignableFrom<IDictionary<string, string>>(output.CloneCustomMetadata());

        // The dictionary CloneCustomMetadata returns doesn't provide a useful KeyValuePair enumerator.
        var orderedMetadata = new SortedDictionary<string, string>(StringComparer.Ordinal);
        foreach (var key in metadata.Keys)
        {
            orderedMetadata.Add(key, metadata[key]);
        }

        Assert.Equal(expectedMetadata, orderedMetadata);
    }

    [Fact]
    public void Execute_LogsError_IfCodeGeneratorMissing()
    {
        // Arrange
        var identity1 = Path.Combine("TestProjects", "files", "NSwag.json");
        var identity2 = Path.Combine("TestProjects", "files", "swashbuckle.json");
        var error1 = Resources.FormatInvalidEmptyMetadataValue("CodeGenerator", "OpenApiReference", identity1);
        var error2 = Resources.FormatInvalidEmptyMetadataValue("CodeGenerator", "OpenApiProjectReference", identity2);
        var @namespace = "Console.Client";
        var inputMetadata1 = new Dictionary<string, string>
            {
                { "ExtraMetadata", "this is extra" },
            };
        var inputMetadata2 = new Dictionary<string, string>
            {
                { "Options", "-quiet" },
                { "SourceProject", "ConsoleProject.csproj" },
            };

        var buildEngine = new MockBuildEngine();
        var task = new GetOpenApiReferenceMetadata
        {
            BuildEngine = buildEngine,
            Extension = ".cs",
            Inputs = new[]
            {
                    new TaskItem(identity1, inputMetadata1),
                    new TaskItem(identity2, inputMetadata2),
                },
            Namespace = @namespace,
            OutputDirectory = "obj",
        };

        // Act
        var result = task.Execute();

        // Assert
        Assert.False(result);
        Assert.True(task.Log.HasLoggedErrors);
        Assert.Equal(2, buildEngine.Errors);
        Assert.Equal(0, buildEngine.Messages);
        Assert.Equal(0, buildEngine.Warnings);
        Assert.Contains(error1, buildEngine.Log, StringComparison.OrdinalIgnoreCase);
        Assert.Contains(error2, buildEngine.Log, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Execute_LogsError_IfOutputPathDuplicated()
    {
        // Arrange
        var identity = Path.Combine("TestProjects", "files", "NSwag.json");
        var codeGenerator = "NSwagCSharp";
        var error = Resources.FormatDuplicateFileOutputPaths(Path.Combine("obj", "NSwagClient.cs"));
        var @namespace = "Console.Client";
        var inputMetadata1 = new Dictionary<string, string>
            {
                { "CodeGenerator", codeGenerator },
                { "ExtraMetadata", "this is extra" },
            };
        var inputMetadata2 = new Dictionary<string, string>
            {
                { "CodeGenerator", codeGenerator },
                { "Options", "-quiet" },
            };

        var buildEngine = new MockBuildEngine();
        var task = new GetOpenApiReferenceMetadata
        {
            BuildEngine = buildEngine,
            Extension = ".cs",
            Inputs = new[]
            {
                    new TaskItem(identity, inputMetadata1),
                    new TaskItem(identity, inputMetadata2),
                },
            Namespace = @namespace,
            OutputDirectory = "obj",
        };

        // Act
        var result = task.Execute();

        // Assert
        Assert.False(result);
        Assert.True(task.Log.HasLoggedErrors);
        Assert.Equal(1, buildEngine.Errors);
        Assert.Equal(0, buildEngine.Messages);
        Assert.Equal(0, buildEngine.Warnings);
        Assert.Contains(error, buildEngine.Log, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Execute_SetsClassName_BasedOnOutputPath()
    {
        // Arrange
        var identity = Path.Combine("TestProjects", "files", "NSwag.json");
        var className = "ThisIsClassy";
        var @namespace = "Console.Client";
        var outputPath = $"{className}.cs";
        var expectedOutputPath = Path.Combine("bin", outputPath);
        var inputMetadata = new Dictionary<string, string>
            {
                { "CodeGenerator", "NSwagCSharp" },
                { "OutputPath", outputPath }
            };

        var task = new GetOpenApiReferenceMetadata
        {
            Extension = ".cs",
            Inputs = new[] { new TaskItem(identity, inputMetadata) },
            Namespace = @namespace,
            OutputDirectory = "bin",
        };

        var expectedMetadata = new SortedDictionary<string, string>(StringComparer.Ordinal)
            {
                { "ClassName", className },
                { "CodeGenerator", "NSwagCSharp" },
                { "FirstForGenerator", "true" },
                { "Namespace", @namespace },
                { "OriginalItemSpec", identity },
                { "OutputPath", expectedOutputPath },
                {
                    "SerializedMetadata",
                    $"Identity={identity}|FirstForGenerator=true|" +
                    $"CodeGenerator=NSwagCSharp|OutputPath={expectedOutputPath}|Namespace={@namespace}|" +
                    $"OriginalItemSpec={identity}|ClassName={className}"
                },
            };

        // Act
        var result = task.Execute();

        // Assert
        Assert.True(result);
        Assert.False(task.Log.HasLoggedErrors);
        var output = Assert.Single(task.Outputs);
        Assert.Equal(identity, output.ItemSpec);
        var metadata = Assert.IsAssignableFrom<IDictionary<string, string>>(output.CloneCustomMetadata());

        // The dictionary CloneCustomMetadata returns doesn't provide a useful KeyValuePair enumerator.
        var orderedMetadata = new SortedDictionary<string, string>(StringComparer.Ordinal);
        foreach (var key in metadata.Keys)
        {
            orderedMetadata.Add(key, metadata[key]);
        }

        Assert.Equal(expectedMetadata, orderedMetadata);
    }

    [Theory]
    [InlineData("aa-bb.cs", "aa_bb")]
    [InlineData("aa.bb.cc.ts", "aa_bb_cc")]
    [InlineData("aa\u20DF\u20DF.tsx", "aa__")] // UnicodeCategory.EnclosingMark (combining enclosing diamond)
    [InlineData("aa\u2005bb\u2005cc.cs", "aa_bb_cc")] // UnicodeCategory.SpaceSeparator (four-per-em space)
    [InlineData("aa\u0096\u0096bb.cs", "aa__bb")] // UnicodeCategory.Control (start of guarded area)
    [InlineData("aa\uFF1C\uFF1C\uFF1Cbb.cs", "aa___bb")] // UnicodeCategory.MathSymbol (fullwidth less-than sign)
    public void Execute_SetsClassName_BasedOnSanitizedOutputPath(string outputPath, string className)
    {
        // Arrange
        var identity = Path.Combine("TestProjects", "files", "NSwag.json");
        var @namespace = "Console.Client";
        var expectedOutputPath = Path.Combine("bin", outputPath);
        var inputMetadata = new Dictionary<string, string>
            {
                { "CodeGenerator", "NSwagCSharp" },
                { "OutputPath", outputPath }
            };

        var task = new GetOpenApiReferenceMetadata
        {
            Extension = ".cs",
            Inputs = new[] { new TaskItem(identity, inputMetadata) },
            Namespace = @namespace,
            OutputDirectory = "bin",
        };

        var expectedMetadata = new SortedDictionary<string, string>(StringComparer.Ordinal)
            {
                { "ClassName", className },
                { "CodeGenerator", "NSwagCSharp" },
                { "FirstForGenerator", "true" },
                { "Namespace", @namespace },
                { "OriginalItemSpec", identity },
                { "OutputPath", expectedOutputPath },
                {
                    "SerializedMetadata",
                    $"Identity={identity}|FirstForGenerator=true|" +
                    $"CodeGenerator=NSwagCSharp|OutputPath={expectedOutputPath}|Namespace={@namespace}|" +
                    $"OriginalItemSpec={identity}|ClassName={className}"
                },
            };

        // Act
        var result = task.Execute();

        // Assert
        Assert.True(result);
        Assert.False(task.Log.HasLoggedErrors);
        var output = Assert.Single(task.Outputs);
        Assert.Equal(identity, output.ItemSpec);
        var metadata = Assert.IsAssignableFrom<IDictionary<string, string>>(output.CloneCustomMetadata());

        // The dictionary CloneCustomMetadata returns doesn't provide a useful KeyValuePair enumerator.
        var orderedMetadata = new SortedDictionary<string, string>(StringComparer.Ordinal);
        foreach (var key in metadata.Keys)
        {
            orderedMetadata.Add(key, metadata[key]);
        }

        Assert.Equal(expectedMetadata, orderedMetadata);
    }

    [Fact]
    public void Execute_SetsFirstForGenerator_UsesCorrectExtension()
    {
        // Arrange
        var identity12 = Path.Combine("TestProjects", "files", "NSwag.json");
        var identity3 = Path.Combine("TestProjects", "files", "swashbuckle.json");
        var className12 = "NSwagClient";
        var className3 = "swashbuckleClient";
        var codeGenerator13 = "NSwagCSharp";
        var codeGenerator2 = "NSwagTypeScript";
        var inputMetadata1 = new Dictionary<string, string> { { "CodeGenerator", codeGenerator13 } };
        var inputMetadata2 = new Dictionary<string, string> { { "CodeGenerator", codeGenerator2 } };
        var inputMetadata3 = new Dictionary<string, string> { { "CodeGenerator", codeGenerator13 } };
        var @namespace = "Console.Client";
        var outputPath1 = Path.Combine("obj", $"{className12}.cs");
        var outputPath2 = Path.Combine("obj", $"{className12}.ts");
        var outputPath3 = Path.Combine("obj", $"{className3}.cs");

        var task = new GetOpenApiReferenceMetadata
        {
            Extension = ".cs",
            Inputs = new[]
            {
                    new TaskItem(identity12, inputMetadata1),
                    new TaskItem(identity12, inputMetadata2),
                    new TaskItem(identity3, inputMetadata3),
                },
            Namespace = @namespace,
            OutputDirectory = "obj",
        };

        var expectedMetadata1 = new SortedDictionary<string, string>(StringComparer.Ordinal)
            {
                { "ClassName", className12 },
                { "CodeGenerator", codeGenerator13 },
                { "FirstForGenerator", "true" },
                { "Namespace", @namespace },
                { "OriginalItemSpec", identity12 },
                { "OutputPath", outputPath1 },
                {
                    "SerializedMetadata",
                    $"Identity={identity12}|FirstForGenerator=true|" +
                    $"CodeGenerator={codeGenerator13}|OutputPath={outputPath1}|Namespace={@namespace}|" +
                    $"OriginalItemSpec={identity12}|ClassName={className12}"
                },
            };
        var expectedMetadata2 = new SortedDictionary<string, string>(StringComparer.Ordinal)
            {
                { "ClassName", className12 },
                { "CodeGenerator", codeGenerator2 },
                { "FirstForGenerator", "true" },
                { "Namespace", @namespace },
                { "OriginalItemSpec", identity12 },
                { "OutputPath", outputPath2 },
                {
                    "SerializedMetadata",
                    $"Identity={identity12}|FirstForGenerator=true|" +
                    $"CodeGenerator={codeGenerator2}|OutputPath={outputPath2}|Namespace={@namespace}|" +
                    $"OriginalItemSpec={identity12}|ClassName={className12}"
                },
            };
        var expectedMetadata3 = new SortedDictionary<string, string>(StringComparer.Ordinal)
            {
                { "ClassName", className3 },
                { "CodeGenerator", codeGenerator13 },
                { "FirstForGenerator", "false" },
                { "Namespace", @namespace },
                { "OriginalItemSpec", identity3 },
                { "OutputPath", outputPath3 },
                {
                    "SerializedMetadata",
                    $"Identity={identity3}|FirstForGenerator=false|" +
                    $"CodeGenerator={codeGenerator13}|OutputPath={outputPath3}|Namespace={@namespace}|" +
                    $"OriginalItemSpec={identity3}|ClassName={className3}"
                },
            };

        // Act
        var result = task.Execute();

        // Assert
        Assert.True(result);
        Assert.False(task.Log.HasLoggedErrors);
        Assert.Collection(
            task.Outputs,
            output =>
            {
                Assert.Equal(identity12, output.ItemSpec);
                var metadata = Assert.IsAssignableFrom<IDictionary<string, string>>(output.CloneCustomMetadata());
                var orderedMetadata = new SortedDictionary<string, string>(StringComparer.Ordinal);
                foreach (var key in metadata.Keys)
                {
                    orderedMetadata.Add(key, metadata[key]);
                }

                Assert.Equal(expectedMetadata1, orderedMetadata);
            },
            output =>
            {
                Assert.Equal(identity12, output.ItemSpec);
                var metadata = Assert.IsAssignableFrom<IDictionary<string, string>>(output.CloneCustomMetadata());
                var orderedMetadata = new SortedDictionary<string, string>(StringComparer.Ordinal);
                foreach (var key in metadata.Keys)
                {
                    orderedMetadata.Add(key, metadata[key]);
                }

                Assert.Equal(expectedMetadata2, orderedMetadata);
            },
            output =>
            {
                Assert.Equal(identity3, output.ItemSpec);
                var metadata = Assert.IsAssignableFrom<IDictionary<string, string>>(output.CloneCustomMetadata());
                var orderedMetadata = new SortedDictionary<string, string>(StringComparer.Ordinal);
                foreach (var key in metadata.Keys)
                {
                    orderedMetadata.Add(key, metadata[key]);
                }

                Assert.Equal(expectedMetadata3, orderedMetadata);
            });
    }
}
