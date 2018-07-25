// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Build.Construction;
using Microsoft.Build.Evaluation;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using RepoTasks.Utilities;

namespace RepoTasks.ProjectModel
{
    internal class SolutionInfoFactory
    {
        private readonly TaskLoggingHelper _logger;
        private readonly IBuildEngine4 _buildEngine;

        public SolutionInfoFactory(TaskLoggingHelper logger, IBuildEngine4 buildEngine)
        {
            _logger = logger;
            _buildEngine = buildEngine;
        }

        public IReadOnlyList<SolutionInfo> Create(IEnumerable<ITaskItem> solutionItems, IDictionary<string, string> properties, string defaultConfig, CancellationToken ct)
        {
            var timer = Stopwatch.StartNew();

            var solutions = new ConcurrentBag<SolutionInfo>();

            Parallel.ForEach(solutionItems, solution =>
            {
                if (ct.IsCancellationRequested)
                {
                    return;
                }

                var solutionFile = solution.ItemSpec.Replace('\\', '/');
                var solutionProps = new Dictionary<string, string>(properties, StringComparer.OrdinalIgnoreCase);
                foreach (var prop in MSBuildListSplitter.GetNamedProperties(solution.GetMetadata("AdditionalProperties")))
                {
                    solutionProps[prop.Key] = prop.Value;
                }

                if (!solutionProps.TryGetValue("Configuration", out var configName))
                {
                    solutionProps["Configuration"] = configName = defaultConfig;
                }

                var key = $"SlnInfo:{solutionFile}:{configName}";
                var obj = _buildEngine.GetRegisteredTaskObject(key, RegisteredTaskObjectLifetime.Build);

                if (obj is SolutionInfo cachedSlnInfo)
                {
                    solutions.Add(cachedSlnInfo);
                    return;
                }

                _logger.LogMessage($"Analyzing {solutionFile} ({configName})");
                var projects = new ConcurrentBag<ProjectInfo>();
                var projectFiles = GetProjectsForSolutionConfig(solutionFile, configName);
                using (var projCollection = new ProjectCollection(solutionProps) { IsBuildEnabled = false })
                {
                    Parallel.ForEach(projectFiles, projectFile =>
                    {
                        if (ct.IsCancellationRequested)
                        {
                            return;
                        }

                        try
                        {
                            projects.Add(new ProjectInfoFactory(_logger).Create(projectFile, projCollection));
                        }
                        catch (Exception ex)
                        {
                            _logger.LogErrorFromException(ex);
                        }
                    });
                }

                bool.TryParse(solution.GetMetadata("Build"), out var shouldBuild);
                bool.TryParse(solution.GetMetadata("IsPatching"), out var isPatching);

                var solutionInfo = new SolutionInfo(
                    solutionFile,
                    configName,
                    projects.ToArray(),
                    shouldBuild,
                    isPatching);

                _buildEngine.RegisterTaskObject(key, solutionInfo, RegisteredTaskObjectLifetime.Build, allowEarlyCollection: true);

                solutions.Add(solutionInfo);
            });

            timer.Stop();
            _logger.LogMessage(MessageImportance.High, $"Finished design-time build in {timer.ElapsedMilliseconds}ms");
            return solutions.ToArray();
        }

        private IList<string> GetProjectsForSolutionConfig(string filePath, string configName)
        {
            var sln = SolutionFile.Parse(filePath);

            if (string.IsNullOrEmpty(configName))
            {
                configName = sln.GetDefaultConfigurationName();
            }

            var projects = new List<string>();

            var config = sln.SolutionConfigurations.FirstOrDefault(c => c.ConfigurationName == configName);
            if (config == null)
            {
                throw new InvalidOperationException($"A solution configuration by the name of '{configName}' was not found in '{filePath}'");
            }

            foreach (var project in sln.ProjectsInOrder
                .Where(p =>
                    p.ProjectType == SolutionProjectType.KnownToBeMSBuildFormat // skips solution folders
                    && p.ProjectConfigurations.TryGetValue(config.FullName, out var projectConfig)
                    && projectConfig.IncludeInBuild))
            {
                projects.Add(project.AbsolutePath.Replace('\\', '/'));
            }

            return projects;
        }
    }
}
