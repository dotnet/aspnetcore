// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.CodeAnalysis.Razor;
using Microsoft.CodeAnalysis.Razor.ProjectSystem;
using Microsoft.VisualStudio.Editor.Razor.Documents;
using Moq;
using Xunit;

namespace Microsoft.VisualStudio.Editor.Razor
{
    public class DefaultImportDocumentManagerTest : ForegroundDispatcherTestBase
    {
        public DefaultImportDocumentManagerTest()
        {
            ProjectPath = "C:\\path\\to\\project\\project.csproj";

            FileSystem = RazorProjectFileSystem.Create(Path.GetDirectoryName(ProjectPath));
            ProjectEngine = RazorProjectEngine.Create(FallbackRazorConfiguration.MVC_2_1, FileSystem, b =>
            {
                // These tests rely on MVC's import behavior.
                Microsoft.AspNetCore.Mvc.Razor.Extensions.RazorExtensions.Register(b);
            });
        }

        private string FilePath { get; }

        private string ProjectPath { get; }

        private RazorProjectFileSystem FileSystem { get; }

        private RazorProjectEngine ProjectEngine { get; }

        [ForegroundFact]
        public void OnSubscribed_StartsFileChangeTrackers()
        {
            // Arrange
            var tracker = Mock.Of<VisualStudioDocumentTracker>(
                t => t.FilePath == "C:\\path\\to\\project\\Views\\Home\\file.cshtml" && 
                t.ProjectPath == ProjectPath &&
                t.ProjectSnapshot == Mock.Of<ProjectSnapshot>(p => p.GetProjectEngine() == ProjectEngine));

            var fileChangeTrackerFactory = new Mock<FileChangeTrackerFactory>();
            var fileChangeTracker1 = new Mock<FileChangeTracker>();
            fileChangeTracker1
                .Setup(f => f.StartListening())
                .Verifiable();
            fileChangeTrackerFactory
                .Setup(f => f.Create("C:\\path\\to\\project\\Views\\Home\\_ViewImports.cshtml"))
                .Returns(fileChangeTracker1.Object)
                .Verifiable();
            var fileChangeTracker2 = new Mock<FileChangeTracker>();
            fileChangeTracker2
                .Setup(f => f.StartListening())
                .Verifiable();
            fileChangeTrackerFactory
                .Setup(f => f.Create("C:\\path\\to\\project\\Views\\_ViewImports.cshtml"))
                .Returns(fileChangeTracker2.Object)
                .Verifiable();
            var fileChangeTracker3 = new Mock<FileChangeTracker>();
            fileChangeTracker3.Setup(f => f.StartListening()).Verifiable();
            fileChangeTrackerFactory
                .Setup(f => f.Create("C:\\path\\to\\project\\_ViewImports.cshtml"))
                .Returns(fileChangeTracker3.Object)
                .Verifiable();

            var manager = new DefaultImportDocumentManager(Dispatcher, new DefaultErrorReporter(), fileChangeTrackerFactory.Object);

            // Act
            manager.OnSubscribed(tracker);

            // Assert
            fileChangeTrackerFactory.Verify();
            fileChangeTracker1.Verify();
            fileChangeTracker2.Verify();
            fileChangeTracker3.Verify();
        }

        [ForegroundFact]
        public void OnSubscribed_AlreadySubscribed_DoesNothing()
        {
            // Arrange
            var tracker = Mock.Of<VisualStudioDocumentTracker>(
                t => t.FilePath == "C:\\path\\to\\project\\file.cshtml" &&
                t.ProjectPath == ProjectPath &&
                t.ProjectSnapshot == Mock.Of<ProjectSnapshot>(p => p.GetProjectEngine() == ProjectEngine));

            var anotherTracker = Mock.Of<VisualStudioDocumentTracker>(
                t => t.FilePath == "C:\\path\\to\\project\\anotherFile.cshtml" && 
                t.ProjectPath == ProjectPath &&
                t.ProjectSnapshot == Mock.Of<ProjectSnapshot>(p => p.GetProjectEngine() == ProjectEngine));

            var callCount = 0;
            var fileChangeTrackerFactory = new Mock<FileChangeTrackerFactory>();
            fileChangeTrackerFactory
                .Setup(f => f.Create(It.IsAny<string>()))
                .Returns(Mock.Of<FileChangeTracker>())
                .Callback(() => callCount++);

            var manager = new DefaultImportDocumentManager(Dispatcher, new DefaultErrorReporter(), fileChangeTrackerFactory.Object);
            manager.OnSubscribed(tracker); // Start tracking the import.

            // Act
            manager.OnSubscribed(anotherTracker);

            // Assert
            Assert.Equal(1, callCount);
        }

        [ForegroundFact]
        public void OnUnsubscribed_StopsFileChangeTracker()
        {
            // Arrange
            var tracker = Mock.Of<VisualStudioDocumentTracker>(
                t => t.FilePath == "C:\\path\\to\\project\\file.cshtml" &&
                t.ProjectPath == ProjectPath &&
                t.ProjectSnapshot == Mock.Of<ProjectSnapshot>(p => p.GetProjectEngine() == ProjectEngine));

            var fileChangeTrackerFactory = new Mock<FileChangeTrackerFactory>(MockBehavior.Strict);
            var fileChangeTracker = new Mock<FileChangeTracker>();
            fileChangeTracker
                .Setup(f => f.StopListening())
                .Verifiable();
            fileChangeTrackerFactory
                .Setup(f => f.Create("C:\\path\\to\\project\\_ViewImports.cshtml"))
                .Returns(fileChangeTracker.Object)
                .Verifiable();

            var manager = new DefaultImportDocumentManager(Dispatcher, new DefaultErrorReporter(), fileChangeTrackerFactory.Object);
            manager.OnSubscribed(tracker); // Start tracking the import.

            // Act
            manager.OnUnsubscribed(tracker);

            // Assert
            fileChangeTrackerFactory.Verify();
            fileChangeTracker.Verify();
        }

        [ForegroundFact]
        public void OnUnsubscribed_AnotherDocumentTrackingImport_DoesNotStopFileChangeTracker()
        {
            // Arrange
            var tracker = Mock.Of<VisualStudioDocumentTracker>(
                t => t.FilePath == "C:\\path\\to\\project\\file.cshtml" && 
                t.ProjectPath == ProjectPath &&
                t.ProjectSnapshot == Mock.Of<ProjectSnapshot>(p => p.GetProjectEngine() == ProjectEngine));

            var anotherTracker = Mock.Of<VisualStudioDocumentTracker>(
                t => t.FilePath == "C:\\path\\to\\project\\anotherFile.cshtml" &&
                t.ProjectPath == ProjectPath &&
                t.ProjectSnapshot == Mock.Of<ProjectSnapshot>(p => p.GetProjectEngine() == ProjectEngine));

            var fileChangeTrackerFactory = new Mock<FileChangeTrackerFactory>();
            var fileChangeTracker = new Mock<FileChangeTracker>();
            fileChangeTracker
                .Setup(f => f.StopListening())
                .Throws(new InvalidOperationException());
            fileChangeTrackerFactory
                .Setup(f => f.Create(It.IsAny<string>()))
                .Returns(fileChangeTracker.Object);

            var manager = new DefaultImportDocumentManager(Dispatcher, new DefaultErrorReporter(), fileChangeTrackerFactory.Object);
            manager.OnSubscribed(tracker); // Starts tracking import for the first document.

            manager.OnSubscribed(anotherTracker); // Starts tracking import for the second document.

            // Act & Assert (Does not throw)
            manager.OnUnsubscribed(tracker);
            manager.OnUnsubscribed(tracker);
        }
    }
}
