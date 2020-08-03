// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.DotNet.Watcher.Internal;
using Moq;
using Xunit;

namespace Microsoft.DotNet.Watcher.Tools
{
    public class MSBuildEvaluationFilterTest
    {
        private readonly IFileSetFactory _fileSetFactory = Mock.Of<IFileSetFactory>(
            f => f.CreateAsync(It.IsAny<CancellationToken>()) == Task.FromResult<IFileSet>(FileSet.Empty));

        [Fact]
        public async Task ProcessAsync_EvaluatesFileSetIfProjFileChanges()
        {
            // Arrange
            var filter = new MSBuildEvaluationFilter(_fileSetFactory);
            var context = new DotNetWatchContext
            {
                Iteration = 0,
            };

            await filter.ProcessAsync(context, default);

            context.Iteration++;
            context.ChangedFile = "Test.csproj";
            context.RequiresMSBuildRevaluation = false;

            // Act
            await filter.ProcessAsync(context, default);

            // Assert
            Assert.True(context.RequiresMSBuildRevaluation);
        }

        [Fact]
        public async Task ProcessAsync_DoesNotEvaluateFileSetIfNonProjFileChanges()
        {
            // Arrange
            var filter = new MSBuildEvaluationFilter(_fileSetFactory);
            var context = new DotNetWatchContext
            {
                Iteration = 0,
            };

            await filter.ProcessAsync(context, default);

            context.Iteration++;
            context.ChangedFile = "Controller.cs";
            context.RequiresMSBuildRevaluation = false;

            // Act
            await filter.ProcessAsync(context, default);

            // Assert
            Assert.False(context.RequiresMSBuildRevaluation);
            Mock.Get(_fileSetFactory).Verify(v => v.CreateAsync(It.IsAny<CancellationToken>()), Times.Once());
        }

        [Fact]
        public async Task ProcessAsync_EvaluateFileSetOnEveryChangeIfOptimizationIsSuppressed()
        {
            // Arrange
            var filter = new MSBuildEvaluationFilter(_fileSetFactory);
            var context = new DotNetWatchContext
            {
                Iteration = 0,
                SuppressMSBuildIncrementalism = true,
            };

            await filter.ProcessAsync(context, default);

            context.Iteration++;
            context.ChangedFile = "Controller.cs";
            context.RequiresMSBuildRevaluation = false;

            // Act
            await filter.ProcessAsync(context, default);

            // Assert
            Assert.True(context.RequiresMSBuildRevaluation);
            Mock.Get(_fileSetFactory).Verify(v => v.CreateAsync(It.IsAny<CancellationToken>()), Times.Exactly(2));

        }

        [Fact]
        public async Task ProcessAsync_SetsEvaluationRequired_IfMSBuildFileChanges_ButIsNotChangedFile()
        {
            // There's a chance that the watcher does not correctly report edits to msbuild files on
            // concurrent edits. MSBuildEvaluationFilter uses timestamps to additionally track changes to these files.

            // Arrange
            var fileSet = new FileSet(false, new[] { "Controlller.cs", "Proj.csproj" });
            var fileSetFactory = Mock.Of<IFileSetFactory>(f => f.CreateAsync(It.IsAny<CancellationToken>()) == Task.FromResult<IFileSet>(fileSet));

            var filter = new TestableMSBuildEvaluationFilter(fileSetFactory)
            {
                Timestamps =
                {
                    ["Controller.cs"] = new DateTime(1000),
                    ["Proj.csproj"] = new DateTime(1000),
                }
            };
            var context = new DotNetWatchContext
            {
                Iteration = 0,
            };

            await filter.ProcessAsync(context, default);
            context.RequiresMSBuildRevaluation = false;
            context.ChangedFile = "Controller.cs";
            context.Iteration++;
            filter.Timestamps["Proj.csproj"] = new DateTime(1007);

            // Act
            await filter.ProcessAsync(context, default);

            // Assert
            Assert.True(context.RequiresMSBuildRevaluation);
        }

        public class TestableMSBuildEvaluationFilter : MSBuildEvaluationFilter
        {
            public TestableMSBuildEvaluationFilter(IFileSetFactory factory)
                : base(factory)
            {
            }

            public Dictionary<string, DateTime> Timestamps { get; } = new Dictionary<string, DateTime>();

            protected override DateTime GetLastWriteTimeUtcSafely(string file) => Timestamps[file];
        }
    }
}
