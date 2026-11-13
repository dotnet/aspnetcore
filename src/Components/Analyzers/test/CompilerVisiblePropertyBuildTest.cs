// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using System.Diagnostics;
using System.Reflection;
using System.Runtime.Versioning;
using System.Security;
using Microsoft.AspNetCore.Components.Analyzers;

namespace Microsoft.AspNetCore.Components.Analyzers.Test;

public class CompilerVisiblePropertyBuildTest
{
    private const string PropertyName = "BlazorDisableThrowNavigationException";
    private const string Diagnostic = "BL0020";
    private const string GlobalJsonFileName = "global.json";
    private const string TargetsFileName = "Microsoft.AspNetCore.Components.Analyzers.targets";
    private const string ProjectFileName = "Probe.csproj";
    private const string SourceFileName = "Program.cs";

    private const string Source = """
        namespace Microsoft.AspNetCore.Builder
        {
            public static class RazorComponentsEndpointRouteBuilderExtensions
            {
                public static object MapRazorComponents<TComponent>(this object endpoints) => endpoints;
            }
        }

        namespace TestApplication
        {
            using Microsoft.AspNetCore.Builder;

            public class App { }

            public static class Program
            {
                public static void Main()
                {
                    new object().MapRazorComponents<App>();
                }
            }
        }
        """;

    [Theory]
    [InlineData(null, true)]
    [InlineData("false", true)]
    [InlineData("true", false)]
    public async Task PackageTargetsExposeNavigationSwitchToCompiler(string? propertyValue, bool expectDiagnostic)
    {
        var directory = Path.Combine(Path.GetTempPath(), $"{nameof(CompilerVisiblePropertyBuildTest)}-{Guid.NewGuid():N}");
        Directory.CreateDirectory(directory);

        try
        {
            var analyzerPath = SecurityElement.Escape(typeof(NavigateToReturnAnalyzer).Assembly.Location);
            var targetsPath = SecurityElement.Escape(Path.Combine(AppContext.BaseDirectory, TargetsFileName));
            var targetFramework = typeof(CompilerVisiblePropertyBuildTest).Assembly.GetCustomAttribute<TargetFrameworkAttribute>()?.FrameworkName;
            Assert.NotNull(targetFramework);
            var version = new FrameworkName(targetFramework).Version;
            var framework = $"net{version.Major}.{version.Minor}";
            var project = $$"""
                <Project Sdk="Microsoft.NET.Sdk">
                  <PropertyGroup>
                    <TargetFramework>{{framework}}</TargetFramework>
                    <OutputType>Exe</OutputType>
                    <EnableNETAnalyzers>false</EnableNETAnalyzers>
                  </PropertyGroup>
                  <ItemGroup>
                    <Analyzer Include="{{analyzerPath}}" />
                  </ItemGroup>
                  <Import Project="{{targetsPath}}" />
                </Project>
                """;

            await File.WriteAllTextAsync(Path.Combine(directory, ProjectFileName), project);
            await File.WriteAllTextAsync(Path.Combine(directory, SourceFileName), Source);
            File.Copy(Path.Combine(AppContext.BaseDirectory, GlobalJsonFileName), Path.Combine(directory, GlobalJsonFileName));

            var dotnetRoot = Environment.GetEnvironmentVariable("DOTNET_ROOT");
            var dotnetName = OperatingSystem.IsWindows() ? "dotnet.exe" : "dotnet";
            var dotnet = string.IsNullOrEmpty(dotnetRoot) ? dotnetName : Path.Combine(dotnetRoot, dotnetName);
            var startInfo = new ProcessStartInfo(dotnet)
            {
                WorkingDirectory = directory,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
            };
            startInfo.Environment.Remove(PropertyName);
            startInfo.ArgumentList.Add("build");
            startInfo.ArgumentList.Add(ProjectFileName);
            startInfo.ArgumentList.Add("--disable-build-servers");
            startInfo.ArgumentList.Add("--nologo");
            startInfo.ArgumentList.Add("--verbosity");
            startInfo.ArgumentList.Add("quiet");
            if (propertyValue is not null)
            {
                startInfo.ArgumentList.Add($"-p:{PropertyName}={propertyValue}");
            }

            using var process = Process.Start(startInfo);
            Assert.NotNull(process);

            var output = process.StandardOutput.ReadToEndAsync();
            var error = process.StandardError.ReadToEndAsync();
            try
            {
                await process.WaitForExitAsync().WaitAsync(TimeSpan.FromMinutes(2));
            }
            catch
            {
                if (!process.HasExited)
                {
                    process.Kill(entireProcessTree: true);
                    await process.WaitForExitAsync();
                }
                throw;
            }

            var buildOutput = await output + await error;
            Assert.True(process.ExitCode == 0, buildOutput);
            Assert.Equal(expectDiagnostic, buildOutput.Contains(Diagnostic, StringComparison.Ordinal));
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }
}
