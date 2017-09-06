// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Threading;
using System.Threading.Tasks;
using Moq;
using Xunit;

namespace Microsoft.CodeAnalysis.Razor.ProjectSystem
{
    public class DefaultProjectSnapshotWorkerTest : ForegroundDispatcherTestBase
    {
        public DefaultProjectSnapshotWorkerTest()
        {
            Project = new AdhocWorkspace().AddProject("Test1", LanguageNames.CSharp);

            CompletionSource = new TaskCompletionSource<ProjectExtensibilityConfiguration>();
            ConfigurationFactory = Mock.Of<ProjectExtensibilityConfigurationFactory>(f => f.GetConfigurationAsync(It.IsAny<Project>(), default(CancellationToken)) == CompletionSource.Task);
        }

        private Project Project { get; }

        private ProjectExtensibilityConfigurationFactory ConfigurationFactory { get; }

        private TaskCompletionSource<ProjectExtensibilityConfiguration> CompletionSource { get; }

        [ForegroundFact]
        public async Task ProcessUpdateAsync_DoesntBlockForegroundThread()
        {
            // Arrange
            var worker = new DefaultProjectSnapshotWorker(Dispatcher, ConfigurationFactory);

            var context = new ProjectSnapshotUpdateContext(Project);

            var configuration = Mock.Of<ProjectExtensibilityConfiguration>();

            // Act 1 -- We want to verify that this doesn't block the main thread
            var task = worker.ProcessUpdateAsync(context);

            // Assert 1
            //
            // We haven't let the background task proceed yet, so this is still null.
            Assert.Null(context.Configuration);

            // Act 2 - Ok let's go
            CompletionSource.SetResult(configuration);
            await task;

            // Assert 2
            Assert.Same(configuration, context.Configuration);
        }
    }
}
