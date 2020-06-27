// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Threading;
using System.Threading.Tasks;
using Moq;
using Xunit;

namespace Microsoft.DotNet.Watcher.Tools
{
    public class MSBuildEvaluationFilterTest
    {
        private readonly IFileSetFactory _fileSetFactory = Mock.Of<IFileSetFactory>(f => f.CreateAsync(It.IsAny<CancellationToken>()) == Task.FromResult(Mock.Of<IFileSet>()));

        [Fact]
        public async Task ProcessAsync_EvaluatesFileSetIfProjFileChanges()
        {
            // Arrange
            var filter = new MSBuildEvaluationFilter(_fileSetFactory);
            var fileSet = Mock.Of<IFileSet>();
            var context = new DotNetWatchContext
            {
                Iteration = 4,
                ChangedFile = "Test.csproj",
                FileSet = fileSet,
            };

            // Act
            await filter.ProcessAsync(context, default);

            // Assert
            Assert.True(context.RequiresMSBuildRevaluation);
            Assert.NotSame(fileSet, context.FileSet);
        }

        [Fact]
        public async Task ProcessAsync_DoesNotEvaluateFileSetIfNonProjFileChanges()
        {
            // Arrange
            var filter = new MSBuildEvaluationFilter(_fileSetFactory);
            var fileSet = Mock.Of<IFileSet>();
            var context = new DotNetWatchContext
            {
                Iteration = 5,
                ChangedFile = "Controller.cs",
                FileSet = fileSet,
            };

            // Act
            await filter.ProcessAsync(context, default);

            // Assert
            Assert.False(context.RequiresMSBuildRevaluation);
            Assert.Same(fileSet, context.FileSet);
        }
    }
}
