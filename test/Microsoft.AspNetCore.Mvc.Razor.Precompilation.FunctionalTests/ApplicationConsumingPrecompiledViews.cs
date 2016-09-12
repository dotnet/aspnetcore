// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Server.IntegrationTesting;
using Microsoft.DotNet.Cli.Utils;
using Xunit;

namespace Microsoft.AspNetCore.Mvc.Razor.Precompilation
{
    public class ApplicationConsumingPrecompiledViews
        : IClassFixture<ApplicationConsumingPrecompiledViews.ApplicationConsumingPrecompiledViewsFixture>
    {
        public ApplicationConsumingPrecompiledViews(ApplicationConsumingPrecompiledViewsFixture fixture)
        {
            Fixture = fixture;
        }

        public ApplicationTestFixture Fixture { get; }

        public static TheoryData SupportedFlavorsTheoryData => RuntimeFlavors.SupportedFlavorsTheoryData;

        [Theory]
        [MemberData(nameof(SupportedFlavorsTheoryData))]
        public async Task ConsumingClassLibrariesWithPrecompiledViewsWork(RuntimeFlavor flavor)
        {
            // Arrange
            using (var deployer = Fixture.CreateDeployment(flavor))
            {
                var deploymentResult = deployer.Deploy();

                // Act
                var response = await Fixture.HttpClient.GetStringWithRetryAsync(
                    deploymentResult.ApplicationBaseUri + "Manage/Home",
                    Fixture.Logger);

                // Assert
                TestEmbeddedResource.AssertContent("ApplicationConsumingPrecompiledViews.Manage.Home.Index.txt", response);
            }
        }

        public class ApplicationConsumingPrecompiledViewsFixture : ApplicationTestFixture
        {
            private readonly string _packOutputDirectory = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());

            public ApplicationConsumingPrecompiledViewsFixture()
                : base("ApplicationUsingPrecompiledViewClassLibrary")
            {
                ClassLibraryPath = Path.GetFullPath(Path.Combine(ApplicationPath, "..", "ClassLibraryWithPrecompiledViews"));
            }

            private string ClassLibraryPath { get; }

            protected override void Restore()
            {
                CreateClassLibraryPackage();
                RestoreProject(ApplicationPath, new[] { _packOutputDirectory });
            }

            private void CreateClassLibraryPackage()
            {
                RestoreProject(ClassLibraryPath);
                ExecuteForClassLibrary(Command.CreateDotNet("build", new[] { ClassLibraryPath, "-c", "Release" }));

                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    // Don't run precompile tool for net451 on xplat.
                    ExecuteForClassLibrary(Command.CreateDotNet(
                        "razor-precompile",
                        GetPrecompileArguments("net451")));
                }

                ExecuteForClassLibrary(Command.CreateDotNet(
                    "razor-precompile",
                    GetPrecompileArguments("netcoreapp1.0")));

                var timestamp = "z" + DateTime.UtcNow.Ticks.ToString().PadLeft(18, '0');
                var packCommand = Command
                    .CreateDotNet("pack", new[] { "--no-build", "-c", "Release", "-o", _packOutputDirectory })
                    .EnvironmentVariable("DOTNET_BUILD_VERSION", timestamp);

                ExecuteForClassLibrary(packCommand);
            }

            private void ExecuteForClassLibrary(ICommand command)
            {
                Console.WriteLine($"Running {command.CommandName} {command.CommandArgs}");
                command
                    .WorkingDirectory(ClassLibraryPath)
                    .EnvironmentVariable(NuGetPackagesEnvironmentKey, TempRestoreDirectory)
                    .EnvironmentVariable(DotnetSkipFirstTimeExperience, "true")
                    .ForwardStdErr(Console.Error)
                    .ForwardStdOut(Console.Out)
                    .Execute()
                    .EnsureSuccessful();
            }

            private string[] GetPrecompileArguments(string targetFramework)
            {
                return new[]
                {
                    ClassLibraryPath,
                    "-c",
                    "Release",
                    "-f",
                    $"{targetFramework}",
                    "-o",
                    $"obj/precompiled/{targetFramework}",
                };
            }

            public override void Dispose()
            {
                TryDeleteDirectory(_packOutputDirectory);
                base.Dispose();
            }
        }
    }
}
