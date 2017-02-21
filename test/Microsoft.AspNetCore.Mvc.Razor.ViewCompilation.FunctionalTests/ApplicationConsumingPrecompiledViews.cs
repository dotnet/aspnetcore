// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Xml.Linq;
using Microsoft.AspNetCore.Testing.xunit;
using Microsoft.DotNet.Cli.Utils;
using Xunit;

namespace Microsoft.AspNetCore.Mvc.Razor.ViewCompilation
{
    public class ApplicationConsumingPrecompiledViews
        : IClassFixture<ApplicationConsumingPrecompiledViews.ApplicationConsumingPrecompiledViewsFixture>
    {
        public ApplicationConsumingPrecompiledViews(ApplicationConsumingPrecompiledViewsFixture fixture)
        {
            Fixture = fixture;
        }

        public ApplicationTestFixture Fixture { get; }

        [ConditionalFact]
        [OSSkipCondition(OperatingSystems.Linux,
            SkipReason = "https://github.com/NuGet/Home/issues/4243, https://github.com/NuGet/Home/issues/4240")]
        [OSSkipCondition(OperatingSystems.MacOSX,
            SkipReason = "https://github.com/NuGet/Home/issues/4243, https://github.com/NuGet/Home/issues/4240")]
        public async Task ConsumingClassLibrariesWithPrecompiledViewsWork()
        {
            // Arrange
            using (var deployer = Fixture.CreateDeployment())
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
                var nuGetConfigPath = Path.Combine(ApplicationPaths.SolutionDirectory, "NuGet.config");
                var nugetConfig = XDocument.Parse(File.ReadAllText(nuGetConfigPath));
                var sources = nugetConfig
                    .Root
                    .Element("packageSources")
                    .Elements("add")
                    .Select(s => s.Attribute("value").Value)
                    .ToList();
                sources.Add(_packOutputDirectory);

                RestoreProject(ApplicationPath, sources);
            }

            private void CreateClassLibraryPackage()
            {
                RestoreProject(ClassLibraryPath);
                ExecuteForClassLibrary(Command.CreateDotNet(
                    "build",
                    new[] { "-c", "Release" }));
                var packCommand = Command
                    .CreateDotNet("pack", new[] { "-c", "Release", "-o", _packOutputDirectory });

                ExecuteForClassLibrary(packCommand);
            }

            private void ExecuteForClassLibrary(ICommand command)
            {
                Console.WriteLine($"Running {command.CommandName} {command.CommandArgs} in {ClassLibraryPath}");
                command
                    .WorkingDirectory(ClassLibraryPath)
                    .EnvironmentVariable(NuGetPackagesEnvironmentKey, TempRestoreDirectory)
                    .EnvironmentVariable(DotnetSkipFirstTimeExperience, "true")
                    .ForwardStdErr(Console.Error)
                    .ForwardStdOut(Console.Out)
                    .Execute()
                    .EnsureSuccessful();
            }

            public override void Dispose()
            {
                TryDeleteDirectory(_packOutputDirectory);
                base.Dispose();
            }
        }
    }
}
