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
using Xunit.Abstractions;

namespace Templates.Test.Helpers
{
    public class ProjectFactoryFixture : IDisposable
    {
        private static SemaphoreSlim DotNetNewLock = new SemaphoreSlim(1);
        private static SemaphoreSlim NodeLock = new SemaphoreSlim(1);

        private ConcurrentDictionary<string, Project> _projects = new ConcurrentDictionary<string, Project>();

        public IMessageSink DiagnosticsMessageSink { get; }

        public ProjectFactoryFixture(IMessageSink diagnosticsMessageSink)
        {
            DiagnosticsMessageSink = diagnosticsMessageSink;
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
                        ProjectGuid = Guid.NewGuid().ToString("N").Substring(0, 6)
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
            assembly.GetCustomAttributes<AssemblyMetadataAttribute>()
                .Single(a => a.Key == "TestTemplateCreationFolder")
                .Value;

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
