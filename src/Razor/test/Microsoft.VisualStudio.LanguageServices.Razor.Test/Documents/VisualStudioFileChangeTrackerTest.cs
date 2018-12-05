// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.CodeAnalysis.Razor;
using Microsoft.VisualStudio.Shell.Interop;
using Moq;
using Xunit;

namespace Microsoft.VisualStudio.Editor.Razor.Documents
{
    public class VisualStudioFileChangeTrackerTest : ForegroundDispatcherTestBase
    {
        private ErrorReporter ErrorReporter { get; } = new DefaultErrorReporter();

        [ForegroundFact]
        public void StartListening_AdvisesForFileChange()
        {
            // Arrange
            uint cookie;
            var fileChangeService = new Mock<IVsFileChangeEx>();
            fileChangeService
                .Setup(f => f.AdviseFileChange(It.IsAny<string>(), It.IsAny<uint>(), It.IsAny<IVsFileChangeEvents>(), out cookie))
                .Returns(VSConstants.S_OK)
                .Verifiable();
            var tracker = new VisualStudioFileChangeTracker(TestProjectData.SomeProjectImportFile.FilePath, Dispatcher, ErrorReporter, fileChangeService.Object);

            // Act
            tracker.StartListening();

            // Assert
            fileChangeService.Verify();
        }

        [ForegroundFact]
        public void StartListening_AlreadyListening_DoesNothing()
        {
            // Arrange
            uint cookie = 100;
            var callCount = 0;
            var fileChangeService = new Mock<IVsFileChangeEx>();
            fileChangeService
                .Setup(f => f.AdviseFileChange(It.IsAny<string>(), It.IsAny<uint>(), It.IsAny<IVsFileChangeEvents>(), out cookie))
                .Returns(VSConstants.S_OK)
                .Callback(() => callCount++);
            var tracker = new VisualStudioFileChangeTracker(TestProjectData.SomeProjectImportFile.FilePath, Dispatcher, ErrorReporter, fileChangeService.Object);
            tracker.StartListening();

            // Act
            tracker.StartListening();

            // Assert
            Assert.Equal(1, callCount);
        }

        [ForegroundFact]
        public void StopListening_UnadvisesForFileChange()
        {
            // Arrange
            uint cookie = 100;
            var fileChangeService = new Mock<IVsFileChangeEx>(MockBehavior.Strict);
            fileChangeService
                .Setup(f => f.AdviseFileChange(It.IsAny<string>(), It.IsAny<uint>(), It.IsAny<IVsFileChangeEvents>(), out cookie))
                .Returns(VSConstants.S_OK)
                .Verifiable();
            fileChangeService
                .Setup(f => f.UnadviseFileChange(cookie))
                .Returns(VSConstants.S_OK)
                .Verifiable();
            var tracker = new VisualStudioFileChangeTracker(TestProjectData.SomeProjectImportFile.FilePath, Dispatcher, ErrorReporter, fileChangeService.Object);
            tracker.StartListening(); // Start listening for changes.

            // Act
            tracker.StopListening();

            // Assert
            fileChangeService.Verify();
        }

        [ForegroundFact]
        public void StopListening_NotListening_DoesNothing()
        {
            // Arrange
            uint cookie = VSConstants.VSCOOKIE_NIL;
            var fileChangeService = new Mock<IVsFileChangeEx>(MockBehavior.Strict);
            fileChangeService
                .Setup(f => f.UnadviseFileChange(cookie))
                .Throws(new InvalidOperationException());
            var tracker = new VisualStudioFileChangeTracker(TestProjectData.SomeProjectImportFile.FilePath, Dispatcher, ErrorReporter, fileChangeService.Object);

            // Act & Assert
            tracker.StopListening();
        }

        [ForegroundTheory]
        [InlineData((uint)_VSFILECHANGEFLAGS.VSFILECHG_Size, (int)FileChangeKind.Changed)]
        [InlineData((uint)_VSFILECHANGEFLAGS.VSFILECHG_Time, (int)FileChangeKind.Changed)]
        [InlineData((uint)_VSFILECHANGEFLAGS.VSFILECHG_Add, (int)FileChangeKind.Added)]
        [InlineData((uint)_VSFILECHANGEFLAGS.VSFILECHG_Del, (int)FileChangeKind.Removed)]
        public void FilesChanged_WithSpecificFlags_InvokesChangedHandler_WithExpectedArguments(uint fileChangeFlag, int expectedKind)
        {
            // Arrange
            var filePath = TestProjectData.SomeProjectImportFile.FilePath;
            uint cookie;
            var fileChangeService = new Mock<IVsFileChangeEx>();
            fileChangeService
                .Setup(f => f.AdviseFileChange(It.IsAny<string>(), It.IsAny<uint>(), It.IsAny<IVsFileChangeEvents>(), out cookie))
                .Returns(VSConstants.S_OK);
            var tracker = new VisualStudioFileChangeTracker(filePath, Dispatcher, ErrorReporter, fileChangeService.Object);

            var called = false;
            tracker.Changed += (sender, args) =>
            {
                called = true;
                Assert.Same(sender, tracker);
                Assert.Equal(filePath, args.FilePath);
                Assert.Equal((FileChangeKind)expectedKind, args.Kind);
            };

            // Act
            tracker.FilesChanged(fileCount: 1, filePaths: new[] { filePath }, fileChangeFlags: new[] { fileChangeFlag });

            // Assert
            Assert.True(called);
        }
    }
}
