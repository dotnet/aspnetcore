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
            HostProject1 = new HostProject("Test1.csproj", FallbackRazorConfiguration.MVC_1_0);
            HostProject2 = new HostProject("Test2.csproj", FallbackRazorConfiguration.MVC_1_0);

            Workspace = TestWorkspace.Create();

            var projectId1 = ProjectId.CreateNewId("Test1");
            var projectId2 = ProjectId.CreateNewId("Test2");

            var solution = Workspace.CurrentSolution
                .AddProject(ProjectInfo.Create(
                    projectId1,
                    VersionStamp.Default,
                    "Test1",
                    "Test1",
                    LanguageNames.CSharp,
                    "Test1.csproj"))
                .AddProject(ProjectInfo.Create(
                    projectId2,
                    VersionStamp.Default,
                    "Test2",
                    "Test2",
                    LanguageNames.CSharp,
                    "Test2.csproj")); ;

            WorkspaceProject1 = solution.GetProject(projectId1);
            WorkspaceProject2 = solution.GetProject(projectId2);
        }

        private HostProject HostProject1 { get; }

        private HostProject HostProject2 { get; }

        private Project WorkspaceProject1 { get; }

        private Project WorkspaceProject2 { get; }

        private Workspace Workspace { get; }

        [ForegroundFact]
        public async Task Queue_ProcessesNotifications_AndGoesBackToSleep()
        {
            // Arrange
            var projectManager = new TestProjectSnapshotManager(Dispatcher, Workspace);
            projectManager.HostProjectAdded(HostProject1);
            projectManager.HostProjectAdded(HostProject2);
            projectManager.WorkspaceProjectAdded(WorkspaceProject1);
            projectManager.WorkspaceProjectAdded(WorkspaceProject2);

            var projectWorker = new TestProjectSnapshotWorker();

            var queue = new ProjectSnapshotWorkerQueue(Dispatcher, projectManager, projectWorker)
            {
                Delay = TimeSpan.FromMilliseconds(1),
                BlockBackgroundWorkStart = new ManualResetEventSlim(initialState: false),
                NotifyBackgroundWorkFinish = new ManualResetEventSlim(initialState: false),
                NotifyForegroundWorkFinish = new ManualResetEventSlim(initialState: false),
            };

            // Act & Assert
            queue.Enqueue(projectManager.GetSnapshot(HostProject1).CreateUpdateContext());

            Assert.True(queue.IsScheduledOrRunning, "Queue should be scheduled during Enqueue");
            Assert.True(queue.HasPendingNotifications, "Queue should have a notification created during Enqueue");

            // Allow the background work to proceed.
            queue.BlockBackgroundWorkStart.Set();

            // Get off the foreground thread and allow the updates to flow through.
            await Task.Run(() => queue.NotifyForegroundWorkFinish.Wait(TimeSpan.FromSeconds(1)));

            Assert.False(queue.IsScheduledOrRunning, "Queue should not have restarted");
            Assert.False(queue.HasPendingNotifications, "Queue should have processed all notifications");
        }

        [ForegroundFact]
        public async Task Queue_ProcessesNotifications_AndRestarts()
        {
            // Arrange
            var projectManager = new TestProjectSnapshotManager(Dispatcher, Workspace);
            projectManager.HostProjectAdded(HostProject1);
            projectManager.HostProjectAdded(HostProject2);
            projectManager.WorkspaceProjectAdded(WorkspaceProject1);
            projectManager.WorkspaceProjectAdded(WorkspaceProject2);

            var projectWorker = new TestProjectSnapshotWorker();

            var queue = new ProjectSnapshotWorkerQueue(Dispatcher, projectManager, projectWorker)
            {
                Delay = TimeSpan.FromMilliseconds(1),
                BlockBackgroundWorkStart = new ManualResetEventSlim(initialState: false),
                NotifyBackgroundWorkFinish = new ManualResetEventSlim(initialState: false),
                NotifyForegroundWorkFinish = new ManualResetEventSlim(initialState: false),
            };

            // Act & Assert
            queue.Enqueue(projectManager.GetSnapshot(HostProject1).CreateUpdateContext());

            Assert.True(queue.IsScheduledOrRunning, "Queue should be scheduled during Enqueue");
            Assert.True(queue.HasPendingNotifications, "Queue should have a notification created during Enqueue");

            // Allow the background work to proceed.
            queue.BlockBackgroundWorkStart.Set();

            queue.NotifyBackgroundWorkFinish.Wait(); // Block the foreground thread so we can queue another notification.

            Assert.True(queue.IsScheduledOrRunning, "Worker should be processing now");
            Assert.False(queue.HasPendingNotifications, "Worker should have taken all notifications");

            queue.Enqueue(projectManager.GetSnapshot(HostProject2).CreateUpdateContext());

            Assert.True(queue.HasPendingNotifications); // Now we should see the worker restart when it finishes.

            // Get off the foreground thread and allow the updates to flow through.
            await Task.Run(() => queue.NotifyForegroundWorkFinish.Wait(TimeSpan.FromSeconds(1)));

            queue.NotifyBackgroundWorkFinish.Reset();
            queue.NotifyForegroundWorkFinish.Reset();

            // It should start running again right away.
            Assert.True(queue.IsScheduledOrRunning, "Queue should be scheduled during Enqueue");
            Assert.True(queue.HasPendingNotifications, "Queue should have a notification created during Enqueue");

            // Allow the background work to proceed.
            queue.BlockBackgroundWorkStart.Set();

            // Get off the foreground thread and allow the updates to flow through.
            await Task.Run(() => queue.NotifyForegroundWorkFinish.Wait(TimeSpan.FromSeconds(1)));

            Assert.False(queue.IsScheduledOrRunning, "Queue should not have restarted");
            Assert.False(queue.HasPendingNotifications, "Queue should have processed all notifications");
        }

        private class TestProjectSnapshotManager : DefaultProjectSnapshotManager
        {
            public TestProjectSnapshotManager(ForegroundDispatcher foregroundDispatcher, Workspace workspace)
                : base(foregroundDispatcher, Mock.Of<ErrorReporter>(), new TestProjectSnapshotWorker(), Enumerable.Empty<ProjectSnapshotChangeTrigger>(), workspace)
            {
            }

            public DefaultProjectSnapshot GetSnapshot(HostProject hostProject)
            {
                return Projects.Cast<DefaultProjectSnapshot>().FirstOrDefault(s => s.FilePath == hostProject.FilePath);
            }

            public DefaultProjectSnapshot GetSnapshot(Project workspaceProject)
            {
                return Projects.Cast<DefaultProjectSnapshot>().FirstOrDefault(s => s.FilePath == workspaceProject.FilePath);
            }

            protected override void NotifyListeners(ProjectChangeEventArgs e)
            {
            }

            protected override void NotifyBackgroundWorker(ProjectSnapshotUpdateContext context)
            {
                Assert.NotNull(context.HostProject);
                Assert.NotNull(context.WorkspaceProject);
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
