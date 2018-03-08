// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.CodeAnalysis.Razor;
using Microsoft.CodeAnalysis.Razor.ProjectSystem;
using Moq;
using Xunit;

namespace Microsoft.VisualStudio.Editor.Razor
{
    public class DefaultImportDocumentManagerIntegrationTest : ForegroundDispatcherTestBase
    {
        [ForegroundFact]
        public void Changed_TrackerChanged_ResultsInChangedHavingCorrectArgs()
        {
            // Arrange
            var filePath = "C:\\path\\to\\project\\Views\\Home\\file.cshtml";
            var anotherFilePath = "C:\\path\\to\\project\\anotherFile.cshtml";
            var projectPath = "C:\\path\\to\\project\\project.csproj";
            var testImportsPath = "C:\\path\\to\\project\\_ViewImports.cshtml";
            var tracker = Mock.Of<VisualStudioDocumentTracker>(t => t.FilePath == filePath && t.ProjectPath == projectPath);
            var anotherTracker = Mock.Of<VisualStudioDocumentTracker>(t => t.FilePath == anotherFilePath && t.ProjectPath == projectPath);
            var templateEngineFactoryService = GetProjectEngineFactoryService();
            var fileChangeTracker = new Mock<FileChangeTracker>();
            fileChangeTracker.Setup(f => f.FilePath).Returns(testImportsPath);
            var fileChangeTrackerFactory = new Mock<FileChangeTrackerFactory>();
            fileChangeTrackerFactory
                .Setup(f => f.Create(testImportsPath))
                .Returns(fileChangeTracker.Object);
            fileChangeTrackerFactory
                .Setup(f => f.Create("C:\\path\\to\\project\\Views\\_ViewImports.cshtml"))
                .Returns(Mock.Of<FileChangeTracker>());
            fileChangeTrackerFactory
                .Setup(f => f.Create("C:\\path\\to\\project\\Views\\Home\\_ViewImports.cshtml"))
                .Returns(Mock.Of<FileChangeTracker>());

            var called = false;
            var manager = new DefaultImportDocumentManager(Dispatcher, new DefaultErrorReporter(), fileChangeTrackerFactory.Object, templateEngineFactoryService);
            manager.OnSubscribed(tracker);
            manager.OnSubscribed(anotherTracker);
            manager.Changed += (sender, args) =>
            {
                called = true;
                Assert.Same(sender, manager);
                Assert.Equal(testImportsPath, args.FilePath);
                Assert.Equal(FileChangeKind.Changed, args.Kind);
                Assert.Collection(
                    args.AssociatedDocuments,
                    f => Assert.Equal(filePath, f),
                    f => Assert.Equal(anotherFilePath, f));
            };

            // Act
            fileChangeTracker.Raise(t => t.Changed += null, new FileChangeEventArgs(testImportsPath, FileChangeKind.Changed));

            // Assert
            Assert.True(called);
        }

        private RazorProjectEngineFactoryService GetProjectEngineFactoryService()
        {
            var projectManager = new Mock<ProjectSnapshotManager>();
            projectManager.Setup(p => p.Projects).Returns(Array.Empty<ProjectSnapshot>());

            var service = new DefaultProjectEngineFactoryService(projectManager.Object);
            return service;
        }
    }
}
