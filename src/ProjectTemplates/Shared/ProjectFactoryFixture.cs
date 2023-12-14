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

namespace Templates.Test.Helpers;

public class ProjectFactoryFixture : IDisposable
{
    private const string LetterChars = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ";
    private readonly ConcurrentDictionary<string, Project> _projects = new ConcurrentDictionary<string, Project>();

    public IMessageSink DiagnosticsMessageSink { get; }

    public ProjectFactoryFixture(IMessageSink diagnosticsMessageSink)
    {
        DiagnosticsMessageSink = diagnosticsMessageSink;
    }

    public async Task<Project> CreateProject(ITestOutputHelper output)
    {
        await TemplatePackageInstaller.EnsureTemplatingEngineInitializedAsync(output);

        var project = CreateProjectImpl(output);

        var projectKey = Guid.NewGuid().ToString().Substring(0, 10).ToLowerInvariant();
        if (!_projects.TryAdd(projectKey, project))
        {
            throw new InvalidOperationException($"Project key collision in {nameof(ProjectFactoryFixture)}.{nameof(CreateProject)}!");
        }

        return project;
    }

    private Project CreateProjectImpl(ITestOutputHelper output)
    {
        var project = new Project
        {
            Output = output,
            DiagnosticsMessageSink = DiagnosticsMessageSink,
            // Ensure first character is a letter to avoid random insertions of '_' into template namespace
            // declarations (i.e. make it more stable for testing)
            ProjectGuid = GetRandomLetter() + Path.GetRandomFileName().Replace(".", string.Empty)
        };
        project.ProjectName = $"AspNet.{project.ProjectGuid}";

        var assemblyPath = GetType().Assembly;
        var basePath = GetTemplateFolderBasePath(assemblyPath);
        project.TemplateOutputDir = Path.Combine(basePath, project.ProjectName);

        return project;
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
