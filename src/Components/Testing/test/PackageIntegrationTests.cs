// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO.Compression;
using System.Diagnostics;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
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
    private const string NativeAotGeneratorEntry = "tools/netstandard2.0/Microsoft.AspNetCore.Components.Testing.NativeAot.dll";
    private const string NativeAotPropsEntry = "buildTransitive/net10.0/Microsoft.AspNetCore.Components.Testing.NativeAot.props";
    private const string NativeAotTargetsEntry = "buildTransitive/net10.0/Microsoft.AspNetCore.Components.Testing.NativeAot.targets";

    public static bool HasPackageBuildOutputs => File.Exists(TestData.PackagePath);

    public static bool HasNativeAotPackageBuildOutputs =>
        HasPackageBuildOutputs && TestData.NativeAotEnabled;

    [Fact(Skip = "Package build outputs are not available in the published test payload.", SkipUnless = nameof(HasPackageBuildOutputs))]
    public void Package_HasExpectedBuildAssets()
    {
        using var package = ZipFile.OpenRead(TestData.PackagePath);

        AssertManagedAssembly(package, GeneratorEntry, "Microsoft.AspNetCore.Components.Testing.Generators");
        AssertManagedAssembly(package, TaskEntry, "Microsoft.AspNetCore.Components.Testing.Tasks");
        AssertMsBuildProject(package, PropsEntry);
        AssertMsBuildProject(package, TargetsEntry);
        AssertPlaywrightDependenciesAligned(package);
    }

    [Fact(Skip = "Package build outputs are not available in the published test payload.", SkipUnless = nameof(HasPackageBuildOutputs))]
    public void NativeAotPackage_IsBuildOnlyAndHasExpectedAssets()
    {
        using var package = ZipFile.OpenRead(TestData.NativeAotPackagePath);

        AssertManagedAssembly(
            package,
            NativeAotGeneratorEntry,
            "Microsoft.AspNetCore.Components.Testing.NativeAot");
        AssertMsBuildProject(package, NativeAotPropsEntry);
        AssertMsBuildProject(package, NativeAotTargetsEntry);
        Assert.DoesNotContain(
            package.Entries,
            entry => (entry.FullName.StartsWith("lib/", StringComparison.OrdinalIgnoreCase) ||
                entry.FullName.StartsWith("ref/", StringComparison.OrdinalIgnoreCase)) &&
                entry.FullName.EndsWith(".dll", StringComparison.OrdinalIgnoreCase));
    }

    [Fact(Skip = "Package build outputs are not available in the published test payload.", SkipUnless = nameof(HasPackageBuildOutputs))]
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

    [Fact(Skip = "Package build outputs are not available in the published test payload.", SkipUnless = nameof(HasPackageBuildOutputs))]
    public void PackageConsumer_NormalBuild_LeavesNativeAotHarnessInert()
    {
        var generatedSources = Directory.Exists(TestData.NormalAppGeneratedSourcesPath)
            ? Directory.EnumerateFiles(TestData.NormalAppGeneratedSourcesPath, "*.cs", SearchOption.AllDirectories)
            : [];

        Assert.DoesNotContain(
            generatedSources,
            path => File.ReadAllText(path).Contains("NativeAotTestHarnessHostingStartup", StringComparison.Ordinal));
        Assert.False(
            File.Exists(Path.Combine(TestData.AppOutputPath, "Microsoft.AspNetCore.Components.Testing.NativeAot.dll")),
            "The build-only Native AOT package must not contribute a runtime assembly.");
    }

    [Fact(Skip = "Package build outputs are not available in the published test payload.", SkipUnless = nameof(HasPackageBuildOutputs))]
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

    [Fact(Skip = "Enable with EnableNativeAotPackageIntegrationTests=true and a supported PackageConsumerRuntimeIdentifier.", SkipUnless = nameof(HasNativeAotPackageBuildOutputs))]
    public void PackageConsumer_NativeAotPublish_EmitsCompiledHarnessAndNativeManifest()
    {
        var generatedSourcePath = Directory
            .EnumerateFiles(TestData.NativeAotAppGeneratedSourcesPath, "NativeAotTestHarness.g.cs", SearchOption.AllDirectories)
            .Single();
        var generatedSource = File.ReadAllText(generatedSourcePath);
        Assert.Contains("NativeAotTestHarnessHostingStartup", generatedSource);
        Assert.Contains("\"E2E_READY_URL\"", generatedSource);
        Assert.Contains("\"TEST_PARENT_PID\"", generatedSource);
        Assert.DoesNotContain("Microsoft.AspNetCore.Components.Testing.Infrastructure", generatedSource);

        var entry = ReadSingleManifestEntry(TestData.NativeAotManifestPath);
        Assert.Equal("compiled", entry.HarnessMode);
        Assert.Equal("", entry.Arguments);
        Assert.NotEqual("dotnet", entry.Executable);

        var executablePath = Path.Combine(
            TestData.NativeAotPublishOutputPath,
            entry.WorkingDirectory,
            entry.Executable);
        Assert.True(File.Exists(executablePath), $"Native executable does not exist: {executablePath}");
    }

    [Fact(Skip = "Enable with EnableNativeAotPackageIntegrationTests=true and a supported PackageConsumerRuntimeIdentifier.", SkipUnless = nameof(HasNativeAotPackageBuildOutputs), Timeout = 60_000)]
    public async Task PackageConsumer_NativeAotApp_LaunchesSignalsReadinessAndServesHttp()
    {
        var entry = ReadSingleManifestEntry(TestData.NativeAotManifestPath);
        var executablePath = Path.Combine(
            TestData.NativeAotPublishOutputPath,
            entry.WorkingDirectory,
            entry.Executable);
        var appPort = GetAvailablePort();

        using var readinessListener = new TcpListener(IPAddress.Loopback, 0);
        readinessListener.Start();
        var readinessPort = ((IPEndPoint)readinessListener.LocalEndpoint).Port;
        var startInfo = new ProcessStartInfo(executablePath)
        {
            WorkingDirectory = Path.GetDirectoryName(executablePath)!,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
        };
        startInfo.Environment["ASPNETCORE_URLS"] = $"http://127.0.0.1:{appPort}";
        startInfo.Environment["E2E_READY_URL"] = $"http://127.0.0.1:{readinessPort}/ready";
        startInfo.Environment["TEST_PARENT_PID"] = Environment.ProcessId.ToString(System.Globalization.CultureInfo.InvariantCulture);
        startInfo.Environment.Remove("DOTNET_STARTUP_HOOKS");
        startInfo.Environment.Remove("ASPNETCORE_HOSTINGSTARTUPASSEMBLIES");

        using var process = Process.Start(startInfo)
            ?? throw new InvalidOperationException("Failed to launch the Native AOT package consumer.");
        try
        {
            var readinessTask = AcceptReadinessPostAsync(readinessListener);
            var completed = await Task.WhenAny(
                readinessTask,
                process.WaitForExitAsync(TestContext.Current.CancellationToken),
                Task.Delay(TimeSpan.FromSeconds(30), TestContext.Current.CancellationToken));
            if (completed != readinessTask)
            {
                var output = await process.StandardOutput.ReadToEndAsync(TestContext.Current.CancellationToken);
                var error = await process.StandardError.ReadToEndAsync(TestContext.Current.CancellationToken);
                Assert.Fail($"Native app exited or timed out before readiness. stdout: {output}\nstderr: {error}");
            }

            await readinessTask;
            using var client = new HttpClient();
            var response = await client.GetStringAsync(
                $"http://127.0.0.1:{appPort}/",
                TestContext.Current.CancellationToken);
            Assert.Equal("Package consumer app", response);
        }
        finally
        {
            readinessListener.Stop();
            if (!process.HasExited)
            {
                process.Kill(entireProcessTree: true);
                await process.WaitForExitAsync(TestContext.Current.CancellationToken);
            }
        }
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

    private static void AssertPlaywrightDependenciesAligned(ZipArchive package)
    {
        var nuspec = Assert.Single(package.Entries, entry => entry.FullName.EndsWith(".nuspec", StringComparison.Ordinal));
        using var stream = nuspec.Open();
        var document = XDocument.Load(stream);
        var dependencies = document
            .Descendants()
            .Where(element => element.Name.LocalName is "dependency")
            .ToDictionary(
                element => (string)element.Attribute("id")!,
                element => (string)element.Attribute("version")!,
                StringComparer.OrdinalIgnoreCase);

        Assert.Equal(dependencies["Microsoft.Playwright"], dependencies["Microsoft.Playwright.TestAdapter"]);
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
            value.GetProperty("publicUrl").GetString()!,
            value.GetProperty("harnessMode").GetString()!);
    }

    private sealed record ManifestEntry(
        string Executable,
        string Arguments,
        string WorkingDirectory,
        string PublicUrl,
        string HarnessMode);

    private static int GetAvailablePort()
    {
        using var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        return ((IPEndPoint)listener.LocalEndpoint).Port;
    }

    private static async Task AcceptReadinessPostAsync(TcpListener listener)
    {
        using var client = await listener.AcceptTcpClientAsync(TestContext.Current.CancellationToken);
        await using var stream = client.GetStream();
        using var reader = new StreamReader(stream, leaveOpen: true);
        var requestLine = await reader.ReadLineAsync(TestContext.Current.CancellationToken);
        Assert.StartsWith("POST /ready ", requestLine);

        while (!string.IsNullOrEmpty(await reader.ReadLineAsync(TestContext.Current.CancellationToken)))
        {
        }

        var response = "HTTP/1.1 200 OK\r\nContent-Length: 0\r\nConnection: close\r\n\r\n"u8.ToArray();
        await stream.WriteAsync(response, TestContext.Current.CancellationToken);
    }

    private static class TestData
    {
        private static readonly IReadOnlyDictionary<string, string> Metadata =
            typeof(PackageIntegrationTests).Assembly
                .GetCustomAttributes<AssemblyMetadataAttribute>()
                .Where(attribute => attribute.Value is not null)
                .ToDictionary(attribute => attribute.Key, attribute => attribute.Value!, StringComparer.Ordinal);

        public static string PackagePath => GetMetadata("ComponentsTestingPackagePath");

        public static string NativeAotPackagePath => GetMetadata("ComponentsTestingNativeAotPackagePath");

        public static string ConsumerRoot => GetMetadata("PackageConsumerRoot");

        public static string TargetFramework => GetMetadata("PackageConsumerTargetFramework");

        public static string Configuration => GetMetadata("PackageConsumerConfiguration");

        public static bool NativeAotEnabled =>
            string.Equals(GetMetadata("PackageConsumerNativeAotEnabled"), "true", StringComparison.OrdinalIgnoreCase);

        public static string ConsumerOutputPath =>
            Path.Combine(ConsumerRoot, "Tests", "bin", Configuration, TargetFramework);

        public static string AppOutputPath =>
            Path.Combine(ConsumerRoot, "App", "bin", Configuration, TargetFramework);

        public static string ConsumerAssemblyPath =>
            Path.Combine(ConsumerOutputPath, "PackageConsumer.Tests.dll");

        public static string GeneratedSourcesPath =>
            Path.Combine(ConsumerRoot, "Tests", "obj", "generated");

        public static string NormalAppGeneratedSourcesPath =>
            Path.Combine(ConsumerRoot, "App", "obj", "generated-normal");

        public static string NativeAotAppGeneratedSourcesPath =>
            Path.Combine(ConsumerRoot, "App", "obj", "generated-nativeaot");

        public static string BuildManifestPath =>
            Path.Combine(ConsumerOutputPath, "PackageConsumer.Tests.e2e-manifest.json");

        public static string PublishOutputPath =>
            Path.Combine(ConsumerOutputPath, "publish");

        public static string PublishManifestPath =>
            Path.Combine(PublishOutputPath, "PackageConsumer.Tests.e2e-manifest.json");

        public static string NativeAotPublishOutputPath =>
            GetMetadata("PackageConsumerNativeAotPublishOutput");

        public static string NativeAotManifestPath =>
            Path.Combine(NativeAotPublishOutputPath, "PackageConsumer.Tests.e2e-manifest.json");

        private static string GetMetadata(string key)
            => Metadata.TryGetValue(key, out var value)
                ? value
                : throw new InvalidOperationException($"Missing assembly metadata '{key}'.");
    }
}
