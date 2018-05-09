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
    public class BackgroundDocumentGeneratorTest : ForegroundDispatcherTestBase
    {
        public BackgroundDocumentGeneratorTest()
        {
            Documents = new HostDocument[]
            {
                new HostDocument("c:\\Test1\\Index.cshtml", "Index.cshtml"),
                new HostDocument("c:\\Test1\\Components\\Counter.cshtml", "Components\\Counter.cshtml"),
            };

            HostProject1 = new HostProject("c:\\Test1\\Test1.csproj", FallbackRazorConfiguration.MVC_1_0);
            HostProject2 = new HostProject("c:\\Test2\\Test2.csproj", FallbackRazorConfiguration.MVC_1_0);

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
                    "c:\\Test1\\Test1.csproj"))
                .AddProject(ProjectInfo.Create(
                    projectId2,
                    VersionStamp.Default,
                    "Test2",
                    "Test2",
                    LanguageNames.CSharp,
                    "c:\\Test2\\Test2.csproj")); ;

            WorkspaceProject1 = solution.GetProject(projectId1);
            WorkspaceProject2 = solution.GetProject(projectId2);
        }

        private HostDocument[] Documents { get; }

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
            projectManager.DocumentAdded(HostProject1, Documents[0], null);
            projectManager.DocumentAdded(HostProject1, Documents[1], null);

            var project = projectManager.GetLoadedProject(HostProject1.FilePath);

            var queue = new BackgroundDocumentGenerator(Dispatcher)
            {
                Delay = TimeSpan.FromMilliseconds(1),
                BlockBackgroundWorkStart = new ManualResetEventSlim(initialState: false),
                NotifyBackgroundWorkStarting = new ManualResetEventSlim(initialState: false),
                BlockBackgroundWorkCompleting = new ManualResetEventSlim(initialState: false),
                NotifyBackgroundWorkCompleted = new ManualResetEventSlim(initialState: false),
            };

            // Act & Assert
            queue.Enqueue(project, project.GetDocument(Documents[0].FilePath));

            Assert.True(queue.IsScheduledOrRunning, "Queue should be scheduled during Enqueue");
            Assert.True(queue.HasPendingNotifications, "Queue should have a notification created during Enqueue");

            // Allow the background work to proceed.
            queue.BlockBackgroundWorkStart.Set();
            queue.BlockBackgroundWorkCompleting.Set();

            await Task.Run(() => queue.NotifyBackgroundWorkCompleted.Wait(TimeSpan.FromSeconds(1)));

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
            projectManager.DocumentAdded(HostProject1, Documents[0], null);
            projectManager.DocumentAdded(HostProject1, Documents[1], null);

            var project = projectManager.GetLoadedProject(HostProject1.FilePath);

            var queue = new BackgroundDocumentGenerator(Dispatcher)
            {
                Delay = TimeSpan.FromMilliseconds(1),
                BlockBackgroundWorkStart = new ManualResetEventSlim(initialState: false),
                NotifyBackgroundWorkStarting = new ManualResetEventSlim(initialState: false),
                BlockBackgroundWorkCompleting = new ManualResetEventSlim(initialState: false),
                NotifyBackgroundWorkCompleted = new ManualResetEventSlim(initialState: false),
            };

            // Act & Assert
            queue.Enqueue(project, project.GetDocument(Documents[0].FilePath));

            Assert.True(queue.IsScheduledOrRunning, "Queue should be scheduled during Enqueue");
            Assert.True(queue.HasPendingNotifications, "Queue should have a notification created during Enqueue");

            // Allow the background work to start.
            queue.BlockBackgroundWorkStart.Set();

            await Task.Run(() => queue.NotifyBackgroundWorkStarting.Wait(TimeSpan.FromSeconds(1)));

            Assert.True(queue.IsScheduledOrRunning, "Worker should be processing now");
            Assert.False(queue.HasPendingNotifications, "Worker should have taken all notifications");

            queue.Enqueue(project, project.GetDocument(Documents[1].FilePath));
            Assert.True(queue.HasPendingNotifications); // Now we should see the worker restart when it finishes.

            // Allow work to complete, which should restart the timer.
            queue.BlockBackgroundWorkCompleting.Set();

            await Task.Run(() => queue.NotifyBackgroundWorkCompleted.Wait(TimeSpan.FromSeconds(1)));
            queue.NotifyBackgroundWorkCompleted.Reset();

            // It should start running again right away.
            Assert.True(queue.IsScheduledOrRunning, "Queue should be scheduled during Enqueue");
            Assert.True(queue.HasPendingNotifications, "Queue should have a notification created during Enqueue");

            // Allow the background work to proceed.
            queue.BlockBackgroundWorkStart.Set();

            queue.BlockBackgroundWorkCompleting.Set();
            await Task.Run(() => queue.NotifyBackgroundWorkCompleted.Wait(TimeSpan.FromSeconds(1)));

            Assert.False(queue.IsScheduledOrRunning, "Queue should not have restarted");
            Assert.False(queue.HasPendingNotifications, "Queue should have processed all notifications");
        }
    }
}
