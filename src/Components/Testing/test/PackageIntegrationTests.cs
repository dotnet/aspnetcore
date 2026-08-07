// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO.Compression;
using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using System.Text.Json;
using System.Xml.Linq;

namespace Microsoft.AspNetCore.Components.Testing.Tests;

public class PackageIntegrationTests
{
    private const string GeneratorEntry = "analyzers/dotnet/cs/Microsoft.AspNetCore.Components.Testing.Generators.dll";
    private const string TaskEntry = "tasks/netstandard2.0/Microsoft.AspNetCore.Components.Testing.Tasks.dll";
    private const string PropsEntry = "buildTransitive/net10.0/Microsoft.AspNetCore.Components.Testing.props";
    private const string TargetsEntry = "buildTransitive/net10.0/Microsoft.AspNetCore.Components.Testing.targets";

    public static bool HasPackageBuildOutputs => string.IsNullOrEmpty(Environment.GetEnvironmentVariable("HELIX_WORKITEM_ROOT"));

    [Fact(Skip = "Package build outputs are not available in the Helix work item.", SkipUnless = nameof(HasPackageBuildOutputs))]
    public void Package_HasExpectedBuildAssets()
    {
        using var package = ZipFile.OpenRead(TestData.PackagePath);

        AssertManagedAssembly(package, GeneratorEntry, "Microsoft.AspNetCore.Components.Testing.Generators");
        AssertManagedAssembly(package, TaskEntry, "Microsoft.AspNetCore.Components.Testing.Tasks");
        AssertMsBuildProject(package, PropsEntry);
        AssertMsBuildProject(package, TargetsEntry);
    }

    [Fact(Skip = "Package build outputs are not available in the Helix work item.", SkipUnless = nameof(HasPackageBuildOutputs))]
    public void PackageConsumer_Build_UsesGeneratorAndCreatesLocalManifest()
    {
        Assert.True(File.Exists(TestData.ConsumerAssemblyPath), $"Consumer assembly was not built: {TestData.ConsumerAssemblyPath}");

        var generatedSourcePath = Directory
            .EnumerateFiles(TestData.GeneratedSourcesPath, "*.cs", SearchOption.AllDirectories)
            .Single(path => Path.GetFileName(path).Contains("PackageConsumerUITest", StringComparison.Ordinal));
        var generatedSource = File.ReadAllText(generatedSourcePath);

        Assert.Contains("[TestClass]", generatedSource);
        Assert.Contains("__UITestInitializeAsync", generatedSource);
        Assert.Contains("__UITestCleanupAsync", generatedSource);

        var entry = ReadSingleManifestEntry(TestData.BuildManifestPath);
        Assert.Equal("dotnet", entry.Executable);
        Assert.Equal("run --no-build --no-restore --no-launch-profile", entry.Arguments);
        Assert.Equal("https://package-consumer.invalid", entry.PublicUrl);
        Assert.Equal(
            Path.GetFullPath(Path.Combine(TestData.ConsumerRoot, "App")),
            Path.GetFullPath(entry.WorkingDirectory));
    }

    [Fact(Skip = "Package build outputs are not available in the Helix work item.", SkipUnless = nameof(HasPackageBuildOutputs))]
    public void PackageConsumer_Publish_ManifestPointsToPublishedApp()
    {
        var entry = ReadSingleManifestEntry(TestData.PublishManifestPath);

        Assert.Equal("https://package-consumer.invalid", entry.PublicUrl);
        Assert.Equal(
            Path.Combine("e2e-apps", "PackageConsumer.App"),
            entry.WorkingDirectory);
        Assert.DoesNotContain("run", entry.Arguments, StringComparison.Ordinal);

        var publishedAppDirectory = Path.Combine(TestData.PublishOutputPath, entry.WorkingDirectory);
        var executablePath = entry.Executable == "dotnet"
            ? Path.Combine(publishedAppDirectory, entry.Arguments)
            : Path.Combine(publishedAppDirectory, entry.Executable);

        Assert.True(File.Exists(executablePath), $"Manifest executable does not exist: {executablePath}");
        Assert.True(File.Exists(Path.Combine(publishedAppDirectory, "PackageConsumer.App.deps.json")));
        Assert.True(File.Exists(Path.Combine(publishedAppDirectory, "PackageConsumer.App.runtimeconfig.json")));
    }

    private static void AssertManagedAssembly(ZipArchive package, string entryName, string expectedAssemblyName)
    {
        var entry = GetEntry(package, entryName);
        Assert.True(entry.Length > 0, $"Package entry '{entryName}' should not be empty.");

        using var stream = entry.Open();
        using var assemblyBytes = new MemoryStream();
        stream.CopyTo(assemblyBytes);
        assemblyBytes.Position = 0;
        using var peReader = new PEReader(assemblyBytes);
        Assert.True(peReader.HasMetadata, $"Package entry '{entryName}' should be a managed assembly.");

        var metadata = peReader.GetMetadataReader();
        var definition = metadata.GetAssemblyDefinition();
        Assert.Equal(expectedAssemblyName, metadata.GetString(definition.Name));
    }

    private static void AssertMsBuildProject(ZipArchive package, string entryName)
    {
        var entry = GetEntry(package, entryName);
        using var stream = entry.Open();
        var document = XDocument.Load(stream);

        Assert.Equal("Project", document.Root?.Name.LocalName);
        Assert.NotEmpty(document.Root!.Elements());
    }

    private static ZipArchiveEntry GetEntry(ZipArchive package, string entryName)
        => Assert.Single(
            package.Entries,
            entry => string.Equals(entry.FullName.Replace('\\', '/'), entryName, StringComparison.OrdinalIgnoreCase));

    private static ManifestEntry ReadSingleManifestEntry(string manifestPath)
    {
        Assert.True(File.Exists(manifestPath), $"Manifest was not generated: {manifestPath}");

        using var document = JsonDocument.Parse(File.ReadAllText(manifestPath));
        var apps = document.RootElement.GetProperty("apps").EnumerateObject().ToArray();
        var app = Assert.Single(apps);
        Assert.Equal("PackageConsumer.App", app.Name);

        var value = app.Value;
        return new ManifestEntry(
            value.GetProperty("executable").GetString()!,
            value.GetProperty("arguments").GetString()!,
            value.GetProperty("workingDirectory").GetString()!,
            value.GetProperty("publicUrl").GetString()!);
    }

    private sealed record ManifestEntry(
        string Executable,
        string Arguments,
        string WorkingDirectory,
        string PublicUrl);

    private static class TestData
    {
        private static readonly IReadOnlyDictionary<string, string> Metadata =
            typeof(PackageIntegrationTests).Assembly
                .GetCustomAttributes<AssemblyMetadataAttribute>()
                .Where(attribute => attribute.Value is not null)
                .ToDictionary(attribute => attribute.Key, attribute => attribute.Value!, StringComparer.Ordinal);

        public static string PackagePath => GetMetadata("ComponentsTestingPackagePath");

        public static string ConsumerRoot => GetMetadata("PackageConsumerRoot");

        public static string TargetFramework => GetMetadata("PackageConsumerTargetFramework");

        public static string Configuration => GetMetadata("PackageConsumerConfiguration");

        public static string ConsumerOutputPath =>
            Path.Combine(ConsumerRoot, "Tests", "bin", Configuration, TargetFramework);

        public static string ConsumerAssemblyPath =>
            Path.Combine(ConsumerOutputPath, "PackageConsumer.Tests.dll");

        public static string GeneratedSourcesPath =>
            Path.Combine(ConsumerRoot, "Tests", "obj", "generated");

        public static string BuildManifestPath =>
            Path.Combine(ConsumerOutputPath, "PackageConsumer.Tests.e2e-manifest.json");

        public static string PublishOutputPath =>
            Path.Combine(ConsumerOutputPath, "publish");

        public static string PublishManifestPath =>
            Path.Combine(PublishOutputPath, "PackageConsumer.Tests.e2e-manifest.json");

        private static string GetMetadata(string key)
            => Metadata.TryGetValue(key, out var value)
                ? value
                : throw new InvalidOperationException($"Missing assembly metadata '{key}'.");
    }
}
