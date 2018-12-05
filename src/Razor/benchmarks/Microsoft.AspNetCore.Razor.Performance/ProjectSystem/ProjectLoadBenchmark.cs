// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using BenchmarkDotNet.Attributes;
using Microsoft.CodeAnalysis.Razor.ProjectSystem;

namespace Microsoft.AspNetCore.Razor.Performance
{
    public class ProjectLoadBenchmark : ProjectSnapshotManagerBenchmarkBase
    {
        [IterationSetup]
        public void Setup()
        {
            SnapshotManager = CreateProjectSnapshotManager();
        }

        private DefaultProjectSnapshotManager SnapshotManager { get; set; }

        [Benchmark(Description = "Initializes a project and 100 files", OperationsPerInvoke = 100)]
        public void ProjectLoad_AddProjectAnd100Files()
        {
            SnapshotManager.HostProjectAdded(HostProject);

            for (var i= 0; i < Documents.Length; i++)
            {
                SnapshotManager.DocumentAdded(HostProject, Documents[i], TextLoaders[i % 4]);
            }
        }
    }
}
