// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.InteropServices;
using System.Text.Json;
using Microsoft.AspNetCore.Components.Testing.Infrastructure;
using Microsoft.AspNetCore.Components.Testing.Tasks;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

namespace Microsoft.AspNetCore.Components.Testing.Tests;

public class GenerateE2EManifestTaskTests : IDisposable
{
    private readonly string _tempDir;

    public GenerateE2EManifestTaskTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), "E2EManifestTests_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tempDir);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
        {
            Directory.Delete(_tempDir, recursive: true);
        }
    }

    // ── build mode + Build scenario (local dev) ──────────────────────
    [Fact]
    public void BuildMode_Build_UsesAbsoluteSourcePaths()
    {
        var projectDir = CreateFakeProjectDir("MyApp");
        var manifestPath = Path.Combine(_tempDir, "manifest.json");

        var task = CreateTask(
            apps: [CreateAppItem("MyApp", projectDir)],
            manifestPath: manifestPath,
            mode: "build",
            isPublishing: false);

        Assert.True(task.Execute());

        var manifest = ReadManifest(manifestPath);
        Assert.Single(manifest.Apps);

        var entry = manifest.GetApp("MyApp");
        Assert.NotNull(entry);
        Assert.Equal("dotnet", entry!.Executable);
        Assert.Equal("run --no-launch-profile", entry.Arguments);
        Assert.Equal(projectDir, entry.WorkingDirectory);
    }

    // ── build mode + Publish scenario ────────────────────────────────
    [Fact]
    public void BuildMode_Publish_UsesRelativeSourcePaths()
    {
        var projectDir = CreateFakeProjectDir("MyApp");
        var manifestPath = Path.Combine(_tempDir, "manifest.json");

        var task = CreateTask(
            apps: [CreateAppItem("MyApp", projectDir)],
            manifestPath: manifestPath,
            mode: "build",
            isPublishing: true);

        Assert.True(task.Execute());

        var manifest = ReadManifest(manifestPath);
        var entry = manifest.GetApp("MyApp");
        Assert.NotNull(entry);
        Assert.Equal("dotnet", entry!.Executable);
        Assert.Equal(Path.Combine("e2e-apps", "MyApp"), entry.WorkingDirectory);
    }

    // ── publish mode + Build scenario (errors — no published output) ─
    [Fact]
    public void PublishMode_Build_FailsWhenNoPublishedOutput()
    {
        var projectDir = CreateFakeProjectDir("MyApp");
        var manifestPath = Path.Combine(_tempDir, "manifest.json");

        var task = CreateTask(
            apps: [CreateAppItem("MyApp", projectDir)],
            manifestPath: manifestPath,
            mode: "publish",
            isPublishing: false);

        Assert.False(task.Execute());
    }

    // ── publish mode + Publish scenario (exe found) ──────────────────
    [Fact]
    public void PublishMode_Publish_WithExe_UsesExeExecutable()
    {
        var projectDir = CreateFakeProjectDir("MyApp");
        var appsOutputDir = Path.Combine(_tempDir, "e2e-apps");
        CreatePublishedApp(appsOutputDir, "MyApp", createExe: true);
        var manifestPath = Path.Combine(_tempDir, "manifest.json");

        var task = CreateTask(
            apps: [CreateAppItem("MyApp", projectDir)],
            manifestPath: manifestPath,
            mode: "publish",
            isPublishing: true,
            appsOutputDir: appsOutputDir);

        Assert.True(task.Execute());

        var manifest = ReadManifest(manifestPath);
        var entry = manifest.GetApp("MyApp");
        Assert.NotNull(entry);
        var expectedExe = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "MyApp.exe" : "MyApp";
        Assert.Equal(expectedExe, entry!.Executable);
        Assert.Equal("", entry.Arguments);
        Assert.Equal(Path.Combine("e2e-apps", "MyApp"), entry.WorkingDirectory);
    }

    // ── publish mode + Publish scenario (dll only) ───────────────────
    [Fact]
    public void PublishMode_Publish_WithDll_UsesDotnetExec()
    {
        var projectDir = CreateFakeProjectDir("MyApp");
        var appsOutputDir = Path.Combine(_tempDir, "e2e-apps");
        CreatePublishedApp(appsOutputDir, "MyApp", createExe: false);
        var manifestPath = Path.Combine(_tempDir, "manifest.json");

        var task = CreateTask(
            apps: [CreateAppItem("MyApp", projectDir)],
            manifestPath: manifestPath,
            mode: "publish",
            isPublishing: true,
            appsOutputDir: appsOutputDir);

        Assert.True(task.Execute());

        var manifest = ReadManifest(manifestPath);
        var entry = manifest.GetApp("MyApp");
        Assert.NotNull(entry);
        Assert.Equal("dotnet", entry!.Executable);
        Assert.Equal("MyApp.dll", entry.Arguments);
    }

    // ── all mode + Build scenario (local dev — build entry only, publish fails) ─
    [Fact]
    public void AllMode_Build_FailsWhenNoPublishedOutput()
    {
        var projectDir = CreateFakeProjectDir("MyApp");
        var manifestPath = Path.Combine(_tempDir, "manifest.json");

        var task = CreateTask(
            apps: [CreateAppItem("MyApp", projectDir)],
            manifestPath: manifestPath,
            mode: "all",
            isPublishing: false);

        Assert.False(task.Execute());
    }

    // ── all mode + Publish scenario ──────────────────────────────────
    [Fact]
    public void AllMode_Publish_CreatesBothEntries()
    {
        var projectDir = CreateFakeProjectDir("MyApp");
        var appsOutputDir = Path.Combine(_tempDir, "e2e-apps");
        // In 'all' mode, published output goes to publish/<name>
        CreatePublishedApp(Path.Combine(appsOutputDir, "publish"), "MyApp", createExe: true);
        var manifestPath = Path.Combine(_tempDir, "manifest.json");

        var task = CreateTask(
            apps: [CreateAppItem("MyApp", projectDir)],
            manifestPath: manifestPath,
            mode: "all",
            isPublishing: true,
            appsOutputDir: appsOutputDir);

        Assert.True(task.Execute());

        var manifest = ReadManifest(manifestPath);
        Assert.Equal(2, manifest.Apps.Count);

        // Build entry
        var buildEntry = manifest.GetApp("MyApp");
        Assert.NotNull(buildEntry);
        Assert.Equal("dotnet", buildEntry!.Executable);
        Assert.Equal("run --no-launch-profile", buildEntry.Arguments);
        Assert.Equal(Path.Combine("e2e-apps", "MyApp"), buildEntry.WorkingDirectory);

        // Publish entry
        var publishEntry = manifest.GetApp("publish/MyApp");
        Assert.NotNull(publishEntry);
        var expectedExe = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "MyApp.exe" : "MyApp";
        Assert.Equal(expectedExe, publishEntry!.Executable);
        Assert.Equal("", publishEntry.Arguments);
        Assert.Equal(Path.Combine("e2e-apps", "publish", "MyApp"), publishEntry.WorkingDirectory);
    }

    // ── Custom E2EAppsRelativeDir ────────────────────────────────────
    [Fact]
    public void BuildMode_Publish_UsesCustomRelativeDir()
    {
        var projectDir = CreateFakeProjectDir("MyApp");
        var manifestPath = Path.Combine(_tempDir, "manifest.json");

        var task = CreateTask(
            apps: [CreateAppItem("MyApp", projectDir)],
            manifestPath: manifestPath,
            mode: "build",
            isPublishing: true,
            appsRelativeDir: "test-apps");

        Assert.True(task.Execute());

        var manifest = ReadManifest(manifestPath);
        var entry = manifest.GetApp("MyApp");
        Assert.NotNull(entry);
        Assert.Equal(Path.Combine("test-apps", "MyApp"), entry!.WorkingDirectory);
    }

    // ── Multiple apps ────────────────────────────────────────────────
    [Fact]
    public void BuildMode_Build_MultipleApps_AllPresent()
    {
        var dir1 = CreateFakeProjectDir("App1");
        var dir2 = CreateFakeProjectDir("App2");
        var manifestPath = Path.Combine(_tempDir, "manifest.json");

        var task = CreateTask(
            apps: [CreateAppItem("App1", dir1), CreateAppItem("App2", dir2)],
            manifestPath: manifestPath,
            mode: "build",
            isPublishing: false);

        Assert.True(task.Execute());

        var manifest = ReadManifest(manifestPath);
        Assert.Equal(2, manifest.Apps.Count);
        Assert.NotNull(manifest.GetApp("App1"));
        Assert.NotNull(manifest.GetApp("App2"));
    }

    // ── PublicUrl metadata flows through ─────────────────────────────
    [Fact]
    public void BuildMode_Build_PublicUrlFlowsThrough()
    {
        var projectDir = CreateFakeProjectDir("MyApp");
        var manifestPath = Path.Combine(_tempDir, "manifest.json");

        var item = CreateAppItem("MyApp", projectDir);
        item.SetMetadata("E2EPublicUrl", "https://localhost:5001");

        var task = CreateTask(
            apps: [item],
            manifestPath: manifestPath,
            mode: "build",
            isPublishing: false);

        Assert.True(task.Execute());

        var manifest = ReadManifest(manifestPath);
        var entry = manifest.GetApp("MyApp");
        Assert.NotNull(entry);
        Assert.Equal("https://localhost:5001", entry!.PublicUrl);
    }

    // ── Helpers ──────────────────────────────────────────────────────

    private string CreateFakeProjectDir(string name)
    {
        var dir = Path.Combine(_tempDir, "src", name);
        Directory.CreateDirectory(dir);
        File.WriteAllText(Path.Combine(dir, name + ".csproj"), "<Project />");
        return dir;
    }

    private static void CreatePublishedApp(string appsOutputDir, string name, bool createExe)
    {
        var dir = Path.Combine(appsOutputDir, name);
        Directory.CreateDirectory(dir);
        if (createExe)
        {
            var exeSuffix = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? ".exe" : "";
            File.WriteAllText(Path.Combine(dir, name + exeSuffix), "fake-exe");
        }
        else
        {
            File.WriteAllText(Path.Combine(dir, name + ".dll"), "fake-dll");
        }
    }

    private static TaskItem CreateAppItem(string name, string projectDir)
    {
        var item = new TaskItem(Path.Combine(projectDir, name + ".csproj"));
        item.SetMetadata("E2EApp", "true");
        return item;
    }

#nullable enable

    private GenerateE2EManifest CreateTask(
        TaskItem[] apps,
        string manifestPath,
        string mode,
        bool isPublishing,
        string? appsOutputDir = null,
        string appsRelativeDir = "e2e-apps")
    {
        return new GenerateE2EManifest
        {
            AppItems = apps,
            ManifestPath = manifestPath,
            E2EAppsOutputDir = appsOutputDir ?? Path.Combine(_tempDir, "e2e-apps"),
            E2EAppsRelativeDir = appsRelativeDir,
            E2EAppMode = mode,
            IsPublishing = isPublishing ? "true" : "false",
            BuildEngine = new MockBuildEngine(),
        };
    }

    private static E2EManifest ReadManifest(string path)
    {
        var json = File.ReadAllText(path);
        var manifest = JsonSerializer.Deserialize<E2EManifest>(json);
        Assert.NotNull(manifest);
        return manifest!;
    }

    private sealed class MockBuildEngine : IBuildEngine
    {
        public bool ContinueOnError => false;
        public int LineNumberOfTaskNode => 0;
        public int ColumnNumberOfTaskNode => 0;
        public string ProjectFileOfTaskNode => "";

        public void LogErrorEvent(BuildErrorEventArgs e) { }
        public void LogWarningEvent(BuildWarningEventArgs e) { }
        public void LogMessageEvent(BuildMessageEventArgs e) { }
        public void LogCustomEvent(CustomBuildEventArgs e) { }
        public bool BuildProjectFile(string projectFileName, string[] targetNames, System.Collections.IDictionary globalProperties, System.Collections.IDictionary targetOutputs) => false;
    }
}
