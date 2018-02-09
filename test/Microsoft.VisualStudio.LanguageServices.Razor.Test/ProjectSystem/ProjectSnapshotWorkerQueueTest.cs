// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Moq;
using Xunit;

namespace Microsoft.CodeAnalysis.Razor.ProjectSystem
{
    // These tests are really integration tests. There isn't a good way to unit test this functionality since
    // the only thing in here is threading.
    public class ProjectSnapshotWorkerQueueTest : ForegroundDispatcherTestBase
    {
        public ProjectSnapshotWorkerQueueTest()
        {
            Project project1 = null;
            Project project2 = null;

            Workspace = TestWorkspace.Create(workspace =>
            {
                project1 = workspace.CurrentSolution.AddProject("Test1", "Test1", LanguageNames.CSharp);
                project2 = workspace.CurrentSolution.AddProject("Test2", "Test2", LanguageNames.CSharp);
            });

            Project1 = project1;
            Project2 = project2;
        }

        public Project Project1 { get; }

        public Project Project2 { get; }

        public Workspace Workspace { get; }

        [ForegroundFact]
        public async Task Queue_ProcessesNotifications_AndGoesBackToSleep()
        {
            // Arrange
            var projectManager = new TestProjectSnapshotManager(Dispatcher, Workspace);
            var projectWorker = new TestProjectSnapshotWorker();

            var queue = new ProjectSnapshotWorkerQueue(Dispatcher, projectManager, projectWorker)
            {
                Delay = TimeSpan.FromMilliseconds(1),
                BlockBackgroundWorkStart = new ManualResetEventSlim(initialState: false),
                NotifyBackgroundWorkFinish = new ManualResetEventSlim(initialState: false),
                NotifyForegroundWorkFinish = new ManualResetEventSlim(initialState: false),
            };

            // Act & Assert
            queue.Enqueue(Project1);

            Assert.True(queue.IsScheduledOrRunning);
            Assert.True(queue.HasPendingNotifications);

            // Allow the background work to proceed.
            queue.BlockBackgroundWorkStart.Set();

            // Get off the foreground thread and allow the updates to flow through.
            await Task.Run(() => queue.NotifyForegroundWorkFinish.Wait(TimeSpan.FromSeconds(1)));

            Assert.False(queue.IsScheduledOrRunning);
            Assert.False(queue.HasPendingNotifications);
        }

        [ForegroundFact]
        public async Task Queue_ProcessesNotifications_AndRestarts()
        {
            // Arrange
            var projectManager = new TestProjectSnapshotManager(Dispatcher, Workspace);
            var projectWorker = new TestProjectSnapshotWorker();

            var queue = new ProjectSnapshotWorkerQueue(Dispatcher, projectManager, projectWorker)
            {
                Delay = TimeSpan.FromMilliseconds(1),
                BlockBackgroundWorkStart = new ManualResetEventSlim(initialState: false),
                NotifyBackgroundWorkFinish = new ManualResetEventSlim(initialState: false),
                NotifyForegroundWorkFinish = new ManualResetEventSlim(initialState: false),
            };

            // Act & Assert
            queue.Enqueue(Project1);

            Assert.True(queue.IsScheduledOrRunning);
            Assert.True(queue.HasPendingNotifications);

            // Allow the background work to proceed.
            queue.BlockBackgroundWorkStart.Set();

            queue.NotifyBackgroundWorkFinish.Wait(); // Block the foreground thread so we can queue another notification.

            Assert.True(queue.IsScheduledOrRunning);
            Assert.False(queue.HasPendingNotifications);

            queue.Enqueue(Project2);

            Assert.True(queue.HasPendingNotifications); // Now we should see the worker restart when it finishes.

            // Get off the foreground thread and allow the updates to flow through.
            await Task.Run(() => queue.NotifyForegroundWorkFinish.Wait(TimeSpan.FromSeconds(1)));

            queue.NotifyBackgroundWorkFinish.Reset();
            queue.NotifyForegroundWorkFinish.Reset();

            // It should start running again right away.
            Assert.True(queue.IsScheduledOrRunning);
            Assert.True(queue.HasPendingNotifications);

            // Allow the background work to proceed.
            queue.BlockBackgroundWorkStart.Set();

            // Get off the foreground thread and allow the updates to flow through.
            await Task.Run(() => queue.NotifyForegroundWorkFinish.Wait(TimeSpan.FromSeconds(1)));
        
            Assert.False(queue.IsScheduledOrRunning);
            Assert.False(queue.HasPendingNotifications);
        }

        private class TestProjectSnapshotManager : DefaultProjectSnapshotManager
        {
            public TestProjectSnapshotManager(ForegroundDispatcher foregroundDispatcher, Workspace workspace)
                : base(foregroundDispatcher, Mock.Of<ErrorReporter>(), new TestProjectSnapshotWorker(), Enumerable.Empty<ProjectSnapshotChangeTrigger>(), workspace)
            {
            }

            public DefaultProjectSnapshot GetSnapshot(ProjectId id)
            {
                return Projects.Cast<DefaultProjectSnapshot>().FirstOrDefault(s => s.UnderlyingProject.Id == id);
            }

            protected override void NotifyListeners(ProjectChangeEventArgs e)
            {
            }

            protected override void NotifyBackgroundWorker(Project project)
            {
            }
        }

        private class TestProjectSnapshotWorker : ProjectSnapshotWorker
        {
            public TestProjectSnapshotWorker()
            {
            }

            public override Task ProcessUpdateAsync(ProjectSnapshotUpdateContext update, CancellationToken cancellationToken = default(CancellationToken))
            {
                return Task.CompletedTask;
            }
        }
    }
}
