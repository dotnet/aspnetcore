// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Moq;

namespace Microsoft.AspNetCore.Razor.Design.IntegrationTests
{
    public abstract class MSBuildIntegrationTestBase
    {
        private static readonly AsyncLocal<ProjectDirectory> _project = new AsyncLocal<ProjectDirectory>();

        protected MSBuildIntegrationTestBase()
        {
        }

#if DEBUG
        protected string Configuration => "Debug";
#elif RELEASE
        protected string Configuration => "Release";
#else
#error Configuration not supported
#endif

        protected string IntermediateOutputPath => Path.Combine("obj", Configuration, TargetFramework);

        protected string OutputPath => Path.Combine("bin", Configuration, TargetFramework);

        protected string PublishOutputPath => Path.Combine(OutputPath, "publish");

        // Used by the test framework to set the project that we're working with
        internal static ProjectDirectory Project
        {
            get { return _project.Value; }
            set { _project.Value = value; }
        }

        protected string RazorIntermediateOutputPath => Path.Combine(IntermediateOutputPath, "Razor");

        protected string TargetFramework { get; set; } = "netcoreapp2.0";

        internal Task<MSBuildResult> DotnetMSBuild(
            string target,
            string args = null,
            bool suppressRestore = false,
            bool suppressTimeout = false,
            MSBuildProcessKind msBuildProcessKind = MSBuildProcessKind.Dotnet)
        {
            var timeout = suppressTimeout ? (TimeSpan?)Timeout.InfiniteTimeSpan : null;
            var restoreArgument = suppressRestore ? "" : "/restore";

            return MSBuildProcessManager.RunProcessAsync(
                Project,
                $"{restoreArgument} /t:{target} /p:Configuration={Configuration} {args}",
                timeout,
                msBuildProcessKind);
        }

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
