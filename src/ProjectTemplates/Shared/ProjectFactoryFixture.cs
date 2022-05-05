// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Xunit.Abstractions;

namespace Templates.Test.Helpers
{
    public class ProjectFactoryFixture : IDisposable
    {
        private const string LetterChars = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ";
        private readonly ConcurrentDictionary<string, Project> _projects = new ConcurrentDictionary<string, Project>();

        public IMessageSink DiagnosticsMessageSink { get; }

        public ProjectFactoryFixture(IMessageSink diagnosticsMessageSink)
        {
            DiagnosticsMessageSink = diagnosticsMessageSink;
        }

        public async Task<Project> GetOrCreateProject(string projectKey, ITestOutputHelper output)
        {
            await TemplatePackageInstaller.EnsureTemplatingEngineInitializedAsync(output);
            // Different tests may have different output helpers, so need to fix up the output to write to the correct log
            if (_projects.TryGetValue(projectKey, out var project))
            {
                project.Output = output;
                return project;
            }
            return _projects.GetOrAdd(
                projectKey,
                (key, outputHelper) =>
                {
                    var project = new Project
                    {
                        Output = outputHelper,
                        DiagnosticsMessageSink = DiagnosticsMessageSink,
                        ProjectGuid = Path.GetRandomFileName().Replace(".", string.Empty)
                    };
                    // Replace first character with a random letter if it's a digit to avoid random insertions of '_'
                    // into template namespace declarations (i.e. make it more stable for testing)
                    var projectNameSuffix = !char.IsLetter(project.ProjectGuid[0])
                        ? string.Create(project.ProjectGuid.Length, project.ProjectGuid, (suffix, guid) =>
                        {
                            guid.AsSpan(1).CopyTo(suffix[1..]);
                            suffix[0] = GetRandomLetter();
                        })
                        : project.ProjectGuid;
                    project.ProjectName = $"AspNet.{projectNameSuffix}";

                    var assemblyPath = GetType().Assembly;
                    var basePath = GetTemplateFolderBasePath(assemblyPath);
                    project.TemplateOutputDir = Path.Combine(basePath, project.ProjectName);
                    return project;
                },
                output);
        }

        private static char GetRandomLetter() => LetterChars[Random.Shared.Next(LetterChars.Length - 1)];

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
