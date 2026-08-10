// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.IO.Compression;
using System.Reflection;
using System.Xml.Linq;

namespace Microsoft.AspNetCore.Components.Endpoints.Generators.Tests;

[TestClass]
public sealed class RazorComponentsMetadataGeneratorPackageTests
{
    private const string PackageId = "Microsoft.AspNetCore.Components.Endpoints.Generators";
    private const string AnalyzerPath = $"analyzers/dotnet/cs/{PackageId}.dll";

    [TestMethod]
    public void PackageContainsOnlyPrereleaseAnalyzerAsset()
    {
        using var package = OpenPackage();
        var entries = package.Archive.Entries
            .Select(entry => entry.FullName)
            .ToArray();

        Assert.Contains(AnalyzerPath, entries);
        Assert.DoesNotContain(path => path.StartsWith("lib/", StringComparison.OrdinalIgnoreCase), entries);
        Assert.DoesNotContain(path => path.StartsWith("ref/", StringComparison.OrdinalIgnoreCase), entries);
        Assert.DoesNotContain(path => path.StartsWith("runtimes/", StringComparison.OrdinalIgnoreCase), entries);
        CollectionAssert.AreEqual(
            new[] { AnalyzerPath },
            entries.Where(path => path.EndsWith(".dll", StringComparison.OrdinalIgnoreCase)).ToArray());

        var nuspec = package.ReadNuspec();
        var ns = nuspec.Root!.Name.Namespace;
        var metadata = nuspec.Root.Element(ns + "metadata")!;
        var version = metadata.Element(ns + "version")!.Value;

        Assert.Contains('-', version);
        Assert.IsNull(metadata.Element(ns + "dependencies"));
    }

    [TestMethod]
    public void ExternalConsumerRestoresPackageAndRunsGeneratorWithoutFlowingDependency()
    {
        using var package = OpenPackage();
        var nuspec = package.ReadNuspec();
        var ns = nuspec.Root!.Name.Namespace;
        var version = nuspec.Root.Element(ns + "metadata")!.Element(ns + "version")!.Value;
        var testDirectory = Path.Combine(
            Path.GetTempPath(),
            $"{nameof(RazorComponentsMetadataGeneratorPackageTests)}-{Guid.NewGuid():N}");

        try
        {
            Directory.CreateDirectory(testDirectory);
            File.WriteAllText(
                Path.Combine(testDirectory, "ExternalConsumer.csproj"),
                $$"""
                <Project Sdk="Microsoft.NET.Sdk">
                  <PropertyGroup>
                    <TargetFramework>{{GetMetadata("DefaultNetCoreTargetFramework")}}</TargetFramework>
                    <EmitCompilerGeneratedFiles>true</EmitCompilerGeneratedFiles>
                    <CompilerGeneratedFilesOutputPath>$(BaseIntermediateOutputPath)generated</CompilerGeneratedFilesOutputPath>
                    <NoWarn>ASPNETCORE9004</NoWarn>
                  </PropertyGroup>
                  <ItemGroup>
                    <KnownFrameworkReference Update="Microsoft.AspNetCore.App"
                                             DefaultRuntimeFrameworkVersion="{{GetMetadata("AspNetCorePackageVersion")}}"
                                             LatestRuntimeFrameworkVersion="{{GetMetadata("AspNetCorePackageVersion")}}"
                                             TargetingPackVersion="{{GetMetadata("AspNetCorePackageVersion")}}" />
                    <FrameworkReference Include="Microsoft.AspNetCore.App" />
                    <PackageReference Include="{{PackageId}}" Version="{{version}}" PrivateAssets="all" />
                  </ItemGroup>
                </Project>
                """);
            File.WriteAllText(
                Path.Combine(testDirectory, "TestMetadata.cs"),
                """
                namespace ExternalConsumer;

                public sealed partial class TestMetadata
                    : Microsoft.AspNetCore.Components.Web.RazorComponentsMetadataContext
                {
                }
                """);

            RunDotNet(
                testDirectory,
                $"build ExternalConsumer.csproj --nologo -v:minimal -p:RestoreSources=\"{package.Directory}\"");

            var generatedFiles = Directory.GetFiles(
                Path.Combine(testDirectory, "obj", "generated"),
                "*.cs",
                SearchOption.AllDirectories);
            Assert.Contains(
                path => path.EndsWith("ExternalConsumer.TestMetadata.Metadata.g.cs", StringComparison.Ordinal),
                generatedFiles);

            var packageOutput = Path.Combine(testDirectory, "packages");
            RunDotNet(
                testDirectory,
                $"pack ExternalConsumer.csproj --no-restore --nologo -v:minimal -p:PackageOutputPath=\"{packageOutput}\"");

            var consumerPackagePath = Assert.ContainsSingle(Directory.GetFiles(packageOutput, "ExternalConsumer.*.nupkg"));
            using var consumerPackage = ZipFile.OpenRead(consumerPackagePath);
            var consumerNuspecEntry = Assert.ContainsSingle(
                entry => entry.FullName.EndsWith(".nuspec", StringComparison.OrdinalIgnoreCase),
                consumerPackage.Entries);
            using var consumerNuspecStream = consumerNuspecEntry.Open();
            var consumerNuspec = XDocument.Load(consumerNuspecStream);

            Assert.DoesNotContain(
                element => string.Equals(
                    element.Attribute("id")?.Value,
                    PackageId,
                    StringComparison.OrdinalIgnoreCase),
                consumerNuspec.Descendants());
        }
        finally
        {
            if (Directory.Exists(testDirectory))
            {
                Directory.Delete(testDirectory, recursive: true);
            }
        }
    }

    private static GeneratorPackage OpenPackage()
    {
        var packageDirectory = GetMetadata("ArtifactsShippingPackagesDir");
        var packagePath = Directory
            .GetFiles(packageDirectory, $"{PackageId}.*.nupkg")
            .Where(path => !path.EndsWith(".symbols.nupkg", StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(File.GetLastWriteTimeUtc)
            .FirstOrDefault();

        Assert.IsTrue(
            packagePath is not null,
            $"Could not find the built {PackageId} package under '{packageDirectory}'.");

        return new GeneratorPackage(packagePath);
    }

    private static void RunDotNet(string workingDirectory, string arguments)
    {
        using var process = Process.Start(new ProcessStartInfo
        {
            FileName = GetMetadata("DotNetHostPath"),
            Arguments = arguments,
            WorkingDirectory = workingDirectory,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
        })!;
        var standardOutput = process.StandardOutput.ReadToEnd();
        var standardError = process.StandardError.ReadToEnd();
        process.WaitForExit();

        Assert.IsTrue(
            process.ExitCode == 0,
            $"""
            dotnet {arguments} failed with exit code {process.ExitCode}.
            Standard output:
            {standardOutput}
            Standard error:
            {standardError}
            """);
    }

    private static string GetMetadata(string key)
        => typeof(RazorComponentsMetadataGeneratorPackageTests).Assembly
            .GetCustomAttributes<AssemblyMetadataAttribute>()
            .Single(attribute => string.Equals(attribute.Key, key, StringComparison.Ordinal))
            .Value!;

    private sealed class GeneratorPackage : IDisposable
    {
        public GeneratorPackage(string path)
        {
            Directory = Path.GetDirectoryName(path)!;
            Archive = ZipFile.OpenRead(path);
        }

        public string Directory { get; }

        public ZipArchive Archive { get; }

        public XDocument ReadNuspec()
        {
            var nuspecEntry = Assert.ContainsSingle(
                entry => entry.FullName.EndsWith(".nuspec", StringComparison.OrdinalIgnoreCase),
                Archive.Entries);
            using var stream = nuspecEntry.Open();

            return XDocument.Load(stream);
        }

        public void Dispose()
        {
            Archive.Dispose();
        }
    }
}
