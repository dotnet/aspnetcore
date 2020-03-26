// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.E2ETesting;
using Xunit.Abstractions;

namespace Templates.Test.Helpers
{
    public class ProjectFactoryFixture : IDisposable
    {
        private readonly static SemaphoreSlim DotNetNewLock = new SemaphoreSlim(1);
        private readonly static SemaphoreSlim NodeLock = new SemaphoreSlim(1);

        private readonly ConcurrentDictionary<string, Project> _projects = new ConcurrentDictionary<string, Project>();

        public IMessageSink DiagnosticsMessageSink { get; }

        public ProjectFactoryFixture(IMessageSink diagnosticsMessageSink)
        {
            DiagnosticsMessageSink = diagnosticsMessageSink;
        }

        static ProjectFactoryFixture()
        {
            // There is no good place to put this, so this is the best one.
            // This sets the defualt timeout for all the Selenium test assertions.
            WaitAssert.DefaultTimeout = TimeSpan.FromSeconds(30);
        }

        public async Task<Project> GetOrCreateProject(string projectKey, ITestOutputHelper output)
        {
            await TemplatePackageInstaller.EnsureTemplatingEngineInitializedAsync(output);
            return _projects.GetOrAdd(
                projectKey,
                (key, outputHelper) =>
                {
                    var project = new Project
                    {
                        DotNetNewLock = DotNetNewLock,
                        NodeLock = NodeLock,
                        Output = outputHelper,
                        DiagnosticsMessageSink = DiagnosticsMessageSink,
                        ProjectGuid = Path.GetRandomFileName().Replace(".", string.Empty)
                    };
                    project.ProjectName = $"AspNet.{key}.{project.ProjectGuid}";

                    var assemblyPath = GetType().Assembly;
                    string basePath = GetTemplateFolderBasePath(assemblyPath);
                    project.TemplateOutputDir = Path.Combine(basePath, project.ProjectName);
                    return project;
                },
                output);
        }

        private static string GetTemplateFolderBasePath(Assembly assembly) =>
            (string.IsNullOrEmpty(Environment.GetEnvironmentVariable("HELIX_DIR"))) 
            ? assembly.GetCustomAttributes<AssemblyMetadataAttribute>()
                .Single(a => a.Key == "TestTemplateCreationFolder")
                .Value
            : Path.Combine(Environment.GetEnvironmentVariable("HELIX_DIR"), "Templates", "BaseFolder");

        public void Dispose()
        {
            var list = new List<Exception>();
            foreach (var project in _projects)
            {
                try
                {
                    project.Value.Dispose();
                }
                catch (Exception e)
                {
                    list.Add(e);
                }
            }

            if (list.Count > 0)
            {
                throw new AggregateException(list);
            }
        }
    }
}
