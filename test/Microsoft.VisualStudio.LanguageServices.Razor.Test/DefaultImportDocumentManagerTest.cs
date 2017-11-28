// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.CodeAnalysis.Razor;
using Microsoft.CodeAnalysis.Razor.ProjectSystem;
using Microsoft.VisualStudio.Editor.Razor;
using Microsoft.VisualStudio.Shell.Interop;
using Moq;
using Xunit;

namespace Microsoft.VisualStudio.LanguageServices.Razor
{
    public class DefaultImportDocumentManagerTest : ForegroundDispatcherTestBase
    {
        [ForegroundFact]
        public void OnSubscribed_StartTrackingImport()
        {
            // Arrange
            var filePath = "C:\\path\\to\\project\\Views\\Home\\file.cshtml";
            var projectPath = "C:\\path\\to\\project\\project.csproj";
            var tracker = Mock.Of<VisualStudioDocumentTracker>(t => t.FilePath == filePath && t.ProjectPath == projectPath);
            var templateEngineFactoryService = GetTemplateEngineFactoryService();

            uint cookie;
            var fileChangeService = new Mock<IVsFileChangeEx>(MockBehavior.Strict);
            fileChangeService
                .Setup(f => f.AdviseFileChange("C:\\path\\to\\project\\Views\\Home\\_ViewImports.cshtml", It.IsAny<uint>(), It.IsAny<IVsFileChangeEvents>(), out cookie))
                .Returns(VSConstants.S_OK)
                .Verifiable();
            fileChangeService
                .Setup(f => f.AdviseFileChange("C:\\path\\to\\project\\Views\\_ViewImports.cshtml", It.IsAny<uint>(), It.IsAny<IVsFileChangeEvents>(), out cookie))
                .Returns(VSConstants.S_OK)
                .Verifiable();
            fileChangeService
                .Setup(f => f.AdviseFileChange("C:\\path\\to\\project\\_ViewImports.cshtml", It.IsAny<uint>(), It.IsAny<IVsFileChangeEvents>(), out cookie))
                .Returns(VSConstants.S_OK)
                .Verifiable();

            var manager = new DefaultImportDocumentManager(fileChangeService.Object, templateEngineFactoryService, Dispatcher, new DefaultErrorReporter());

            // Act
            manager.OnSubscribed(tracker);

            // Assert
            fileChangeService.Verify();
        }

        [ForegroundFact]
        public void OnSubscribed_AlreadyTrackingImport_DoesNothing()
        {
            // Arrange
            var filePath = "C:\\path\\to\\project\\file.cshtml";
            var projectPath = "C:\\path\\to\\project\\project.csproj";
            var tracker = Mock.Of<VisualStudioDocumentTracker>(t => t.FilePath == filePath && t.ProjectPath == projectPath);
            var templateEngineFactoryService = GetTemplateEngineFactoryService();

            uint cookie;
            var callCount = 0;
            var fileChangeService = new Mock<IVsFileChangeEx>();
            fileChangeService
                .Setup(f => f.AdviseFileChange(It.IsAny<string>(), It.IsAny<uint>(), It.IsAny<IVsFileChangeEvents>(), out cookie))
                .Returns(VSConstants.S_OK)
                .Callback(() => callCount++);

            var manager = new DefaultImportDocumentManager(fileChangeService.Object, templateEngineFactoryService, Dispatcher, new DefaultErrorReporter());
            manager.OnSubscribed(tracker); // Start tracking the import.

            var anotherFilePath = "C:\\path\\to\\project\\anotherFile.cshtml";
            var anotherTracker = Mock.Of<VisualStudioDocumentTracker>(t => t.FilePath == anotherFilePath && t.ProjectPath == projectPath);

            // Act
            manager.OnSubscribed(anotherTracker);

            // Assert
            Assert.Equal(1, callCount);
        }

        [ForegroundFact]
        public void OnUnsubscribed_StopsTrackingImport()
        {
            // Arrange
            var filePath = "C:\\path\\to\\project\\file.cshtml";
            var projectPath = "C:\\path\\to\\project\\project.csproj";
            var tracker = Mock.Of<VisualStudioDocumentTracker>(t => t.FilePath == filePath && t.ProjectPath == projectPath);
            var templateEngineFactoryService = GetTemplateEngineFactoryService();

            uint cookie = 100;
            var fileChangeService = new Mock<IVsFileChangeEx>(MockBehavior.Strict);
            fileChangeService
                .Setup(f => f.AdviseFileChange("C:\\path\\to\\project\\_ViewImports.cshtml", It.IsAny<uint>(), It.IsAny<IVsFileChangeEvents>(), out cookie))
                .Returns(VSConstants.S_OK)
                .Verifiable();
            fileChangeService
                .Setup(f => f.UnadviseFileChange(cookie))
                .Returns(VSConstants.S_OK)
                .Verifiable();

            var manager = new DefaultImportDocumentManager(fileChangeService.Object, templateEngineFactoryService, Dispatcher, new DefaultErrorReporter());
            manager.OnSubscribed(tracker); // Start tracking the import.

            // Act
            manager.OnUnsubscribed(tracker);

            // Assert
            fileChangeService.Verify();
        }

        [ForegroundFact]
        public void OnUnsubscribed_AnotherDocumentTrackingImport_DoesNotStopTrackingImport()
        {
            // Arrange
            var filePath = "C:\\path\\to\\project\\file.cshtml";
            var projectPath = "C:\\path\\to\\project\\project.csproj";
            var tracker = Mock.Of<VisualStudioDocumentTracker>(t => t.FilePath == filePath && t.ProjectPath == projectPath);
            var templateEngineFactoryService = GetTemplateEngineFactoryService();

            uint cookie;
            var fileChangeService = new Mock<IVsFileChangeEx>();
            fileChangeService
                .Setup(f => f.AdviseFileChange(It.IsAny<string>(), It.IsAny<uint>(), It.IsAny<IVsFileChangeEvents>(), out cookie))
                .Returns(VSConstants.S_OK);
            fileChangeService
                .Setup(f => f.UnadviseFileChange(It.IsAny<uint>()))
                .Returns(VSConstants.S_OK)
                .Callback(() => throw new InvalidOperationException());

            var manager = new DefaultImportDocumentManager(fileChangeService.Object, templateEngineFactoryService, Dispatcher, new DefaultErrorReporter());
            manager.OnSubscribed(tracker); // Starts tracking import for the first document.

            var anotherFilePath = "C:\\path\\to\\project\\anotherFile.cshtml";
            var anotherTracker = Mock.Of<VisualStudioDocumentTracker>(t => t.FilePath == anotherFilePath && t.ProjectPath == projectPath);
            manager.OnSubscribed(anotherTracker); // Starts tracking import for the second document.

            // Act & Assert (Does not throw)
            manager.OnUnsubscribed(tracker);
        }

        [ForegroundTheory]
        [InlineData((uint)_VSFILECHANGEFLAGS.VSFILECHG_Size, (int)ImportChangeKind.Changed)]
        [InlineData((uint)_VSFILECHANGEFLAGS.VSFILECHG_Time, (int)ImportChangeKind.Changed)]
        [InlineData((uint)_VSFILECHANGEFLAGS.VSFILECHG_Add, (int)ImportChangeKind.Added)]
        [InlineData((uint)_VSFILECHANGEFLAGS.VSFILECHG_Del, (int)ImportChangeKind.Removed)]
        public void OnFilesChanged_WithSpecificFlags_InvokesChangedHandler_WithExpectedArguments(uint fileChangeFlag, int expectedKind)
        {
            // Arrange
            var filePath = "C:\\path\\to\\project\\file.cshtml";
            var projectPath = "C:\\path\\to\\project\\project.csproj";
            var tracker = Mock.Of<VisualStudioDocumentTracker>(t => t.FilePath == filePath && t.ProjectPath == projectPath);
            var templateEngineFactoryService = GetTemplateEngineFactoryService();

            var anotherFilePath = "C:\\path\\to\\project\\anotherFile.cshtml";
            var anotherTracker = Mock.Of<VisualStudioDocumentTracker>(t => t.FilePath == anotherFilePath && t.ProjectPath == projectPath);

            uint cookie;
            var fileChangeService = new Mock<IVsFileChangeEx>();
            fileChangeService
                .Setup(f => f.AdviseFileChange(It.IsAny<string>(), It.IsAny<uint>(), It.IsAny<IVsFileChangeEvents>(), out cookie))
                .Returns(VSConstants.S_OK);
            var manager = new DefaultImportDocumentManager(fileChangeService.Object, templateEngineFactoryService, Dispatcher, new DefaultErrorReporter());
            manager.OnSubscribed(tracker);
            manager.OnSubscribed(anotherTracker);

            var called = false;
            manager.Changed += (sender, args) =>
            {
                called = true;
                Assert.Same(sender, manager);
                Assert.Equal("C:\\path\\to\\project\\_ViewImports.cshtml", args.FilePath);
                Assert.Equal((ImportChangeKind)expectedKind, args.Kind);
                Assert.Collection(
                    args.AssociatedDocuments,
                    f => Assert.Equal(filePath, f),
                    f => Assert.Equal(anotherFilePath, f));
            };

            // Act
            manager.OnFilesChanged(fileCount: 1, filePaths: new[] { "C:\\path\\to\\project\\_ViewImports.cshtml" }, fileChangeFlags: new[] { fileChangeFlag });

            // Assert
            Assert.True(called);
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
