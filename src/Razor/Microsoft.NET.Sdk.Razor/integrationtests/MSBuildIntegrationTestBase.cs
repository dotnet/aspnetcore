// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Moq;

namespace Microsoft.AspNetCore.Razor.Design.IntegrationTests
{
    public abstract class MSBuildIntegrationTestBase
    {
        private static readonly AsyncLocal<ProjectDirectory> _project = new AsyncLocal<ProjectDirectory>();

        protected MSBuildIntegrationTestBase(BuildServerTestFixtureBase buildServer)
        {
            BuildServer = buildServer;
        }

#if DEBUG
        protected string Configuration => "Debug";
#elif RELEASE
        protected string Configuration => "Release";
#else
#error Configuration not supported
#endif

        protected string IntermediateOutputPath => Path.Combine("obj", Configuration, Project.TargetFramework);

        protected string OutputPath => Path.Combine("bin", Configuration, Project.TargetFramework);

        protected string PublishOutputPath => Path.Combine(OutputPath, "publish");

        protected string GetRidSpecificPublishOutputPath(string rid) => Path.Combine(OutputPath, rid, "publish");

        // Used by the test framework to set the project that we're working with
        internal static ProjectDirectory Project
        {
            get { return _project.Value; }
            set { _project.Value = value; }
        }

        protected string RazorIntermediateOutputPath => Path.Combine(IntermediateOutputPath, "Razor");

        protected string RazorComponentIntermediateOutputPath => Path.Combine(IntermediateOutputPath, "RazorDeclaration");

        protected BuildServerTestFixtureBase BuildServer { get; set; }

        internal Task<MSBuildResult> DotnetMSBuild(
            string target,
            string args = null,
            bool suppressTimeout = false,
            bool suppressBuildServer = false,
            string buildServerPipeName = null,
            MSBuildProcessKind msBuildProcessKind = MSBuildProcessKind.Dotnet)
        {
            var timeout = suppressTimeout ? (TimeSpan?)Timeout.InfiniteTimeSpan : null;

            var buildArgumentList = new List<string>
            {
                // Disable node-reuse. We don't want msbuild processes to stick around
                // once the test is completed.
                "/nr:false",

                // Always generate a bin log for debugging purposes
                "/bl",

                // Let the test app know it is running as part of a test.
                "/p:RunningAsTest=true",

                $"/p:MicrosoftNETCoreAppRuntimeVersion={BuildVariables.MicrosoftNETCoreAppRuntimeVersion}",
                $"/p:MicrosoftNetCompilersToolsetPackageVersion={BuildVariables.MicrosoftNetCompilersToolsetPackageVersion}",
                $"/p:RazorSdkDirectoryRoot={BuildVariables.RazorSdkDirectoryRoot}",
                $"/p:RepoRoot={BuildVariables.RepoRoot}",
            };

            if (!suppressBuildServer)
            {
                buildArgumentList.Add($@"/p:_RazorBuildServerPipeName=""{buildServerPipeName ?? BuildServer.PipeName}""");
            }

            if (!string.IsNullOrEmpty(target))
            {
                buildArgumentList.Add($"/t:{target}");
            }
            else
            {
                buildArgumentList.Add($"/t:Build");
            }

            buildArgumentList.Add($"/p:Configuration={Configuration} {args}");
            var buildArguments = string.Join(" ", buildArgumentList);

            return MSBuildProcessManager.RunProcessAsync(
                Project,
                buildArguments,
                timeout,
                msBuildProcessKind);
        }

        internal void AddProjectFileContent(string content)
            => Project.AddProjectFileContent(content);

        internal void ReplaceContent(string content, params string[] paths)
        {
            if (content == null)
            {
                throw new ArgumentNullException(nameof(content));
            }

            if (paths == null)
            {
                throw new ArgumentNullException(nameof(paths));
            }

            var filePath = Path.Combine(Project.DirectoryPath, Path.Combine(paths));
            if (!File.Exists(filePath))
            {
                throw new InvalidOperationException($"File {filePath} could not be found.");
            }

            File.WriteAllText(filePath, content, Encoding.UTF8);
            // Timestamps on xplat are precise only to a second. Update it's last write time by at least 1 second
            // so we can ensure that MSBuild recognizes the file change. See https://github.com/dotnet/corefx/issues/26024
            File.SetLastWriteTimeUtc(filePath, File.GetLastWriteTimeUtc(filePath).AddSeconds(1));
        }

        /// <summary>
        /// Locks all files, discovered at the time of method invocation, under the
        /// specified <paramref name="directory" /> from reads or writes.
        /// </summary>
        public IDisposable LockDirectory(string directory)
        {
            directory = Path.Combine(Project.DirectoryPath, directory);
            var disposables = new List<IDisposable>();
            foreach (var file in Directory.EnumerateFiles(directory, "*", SearchOption.AllDirectories))
            {
                disposables.Add(LockFile(file));
            }

            var disposable = new Mock<IDisposable>();
            disposable.Setup(d => d.Dispose())
                .Callback(() => disposables.ForEach(d => d.Dispose()));

            return disposable.Object;
        }

        public IDisposable LockFile(string path)
        {
            path = Path.Combine(Project.DirectoryPath, path);
            return File.Open(path, FileMode.Open, FileAccess.Read, FileShare.None);
        }

        public FileThumbPrint GetThumbPrint(string path)
        {
            path = Path.Combine(Project.DirectoryPath, path);
            return FileThumbPrint.Create(path);
        }
    }
}
