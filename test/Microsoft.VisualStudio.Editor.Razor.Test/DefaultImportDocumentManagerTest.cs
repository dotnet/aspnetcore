// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.CodeAnalysis.Razor;
using Microsoft.CodeAnalysis.Razor.ProjectSystem;
using Moq;
using Xunit;

namespace Microsoft.VisualStudio.Editor.Razor
{
    public class DefaultImportDocumentManagerTest : ForegroundDispatcherTestBase
    {
        [ForegroundFact]
        public void OnSubscribed_StartsFileChangeTrackers()
        {
            // Arrange
            var filePath = "C:\\path\\to\\project\\Views\\Home\\file.cshtml";
            var projectPath = "C:\\path\\to\\project\\project.csproj";
            var tracker = Mock.Of<VisualStudioDocumentTracker>(t => t.FilePath == filePath && t.ProjectPath == projectPath);
            var templateEngineFactoryService = GetTemplateEngineFactoryService();
            var fileChangeTracker1 = new Mock<FileChangeTracker>();
            fileChangeTracker1.Setup(f => f.StartListening()).Verifiable();
            var fileChangeTrackerFactory = new Mock<FileChangeTrackerFactory>();
            fileChangeTrackerFactory
                .Setup(f => f.Create("C:\\path\\to\\project\\Views\\Home\\_ViewImports.cshtml"))
                .Returns(fileChangeTracker1.Object)
                .Verifiable();
            var fileChangeTracker2 = new Mock<FileChangeTracker>();
            fileChangeTracker2.Setup(f => f.StartListening()).Verifiable();
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

            var manager = new DefaultImportDocumentManager(Dispatcher, new DefaultErrorReporter(), fileChangeTrackerFactory.Object, templateEngineFactoryService);

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
            var filePath = "C:\\path\\to\\project\\file.cshtml";
            var projectPath = "C:\\path\\to\\project\\project.csproj";
            var tracker = Mock.Of<VisualStudioDocumentTracker>(t => t.FilePath == filePath && t.ProjectPath == projectPath);
            var templateEngineFactoryService = GetTemplateEngineFactoryService();

            var callCount = 0;
            var fileChangeTrackerFactory = new Mock<FileChangeTrackerFactory>();
            fileChangeTrackerFactory
                .Setup(f => f.Create(It.IsAny<string>()))
                .Returns(Mock.Of<FileChangeTracker>())
                .Callback(() => callCount++);

            var manager = new DefaultImportDocumentManager(Dispatcher, new DefaultErrorReporter(), fileChangeTrackerFactory.Object, templateEngineFactoryService);
            manager.OnSubscribed(tracker); // Start tracking the import.

            var anotherFilePath = "C:\\path\\to\\project\\anotherFile.cshtml";
            var anotherTracker = Mock.Of<VisualStudioDocumentTracker>(t => t.FilePath == anotherFilePath && t.ProjectPath == projectPath);

            // Act
            manager.OnSubscribed(anotherTracker);

            // Assert
            Assert.Equal(1, callCount);
        }

        [ForegroundFact]
        public void OnUnsubscribed_StopsFileChangeTracker()
        {
            // Arrange
            var filePath = "C:\\path\\to\\project\\file.cshtml";
            var projectPath = "C:\\path\\to\\project\\project.csproj";
            var tracker = Mock.Of<VisualStudioDocumentTracker>(t => t.FilePath == filePath && t.ProjectPath == projectPath);
            var templateEngineFactoryService = GetTemplateEngineFactoryService();

            var fileChangeTracker = new Mock<FileChangeTracker>();
            fileChangeTracker.Setup(f => f.StopListening()).Verifiable();
            var fileChangeTrackerFactory = new Mock<FileChangeTrackerFactory>(MockBehavior.Strict);
            fileChangeTrackerFactory
                .Setup(f => f.Create("C:\\path\\to\\project\\_ViewImports.cshtml"))
                .Returns(fileChangeTracker.Object)
                .Verifiable();

            var manager = new DefaultImportDocumentManager(Dispatcher, new DefaultErrorReporter(), fileChangeTrackerFactory.Object, templateEngineFactoryService);
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
            var filePath = "C:\\path\\to\\project\\file.cshtml";
            var projectPath = "C:\\path\\to\\project\\project.csproj";
            var tracker = Mock.Of<VisualStudioDocumentTracker>(t => t.FilePath == filePath && t.ProjectPath == projectPath);
            var templateEngineFactoryService = GetTemplateEngineFactoryService();

            var fileChangeTracker = new Mock<FileChangeTracker>();
            fileChangeTracker
                .Setup(f => f.StopListening())
                .Throws(new InvalidOperationException());
            var fileChangeTrackerFactory = new Mock<FileChangeTrackerFactory>();
            fileChangeTrackerFactory
                .Setup(f => f.Create(It.IsAny<string>()))
                .Returns(fileChangeTracker.Object);

            var manager = new DefaultImportDocumentManager(Dispatcher, new DefaultErrorReporter(), fileChangeTrackerFactory.Object, templateEngineFactoryService);
            manager.OnSubscribed(tracker); // Starts tracking import for the first document.

            var anotherFilePath = "C:\\path\\to\\project\\anotherFile.cshtml";
            var anotherTracker = Mock.Of<VisualStudioDocumentTracker>(t => t.FilePath == anotherFilePath && t.ProjectPath == projectPath);
            manager.OnSubscribed(anotherTracker); // Starts tracking import for the second document.

            // Act & Assert (Does not throw)
            manager.OnUnsubscribed(tracker);
        }

        private RazorTemplateEngineFactoryService GetTemplateEngineFactoryService()
        {
            var projectManager = new Mock<ProjectSnapshotManager>();
            projectManager.Setup(p => p.Projects).Returns(Array.Empty<ProjectSnapshot>());

            var service = new DefaultTemplateEngineFactoryService(projectManager.Object);
            return service;
        }
    }
}
