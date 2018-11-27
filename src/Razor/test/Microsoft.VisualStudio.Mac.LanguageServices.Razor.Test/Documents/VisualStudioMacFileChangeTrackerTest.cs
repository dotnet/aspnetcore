// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.Razor;
using Xunit;

namespace Microsoft.VisualStudio.Editor.Razor.Documents
{
    public class VisualStudioMacFileChangeTrackerTest : ForegroundDispatcherTestBase
    {
        [ForegroundFact]
        public void StartListening_AdvisesForFileChange()
        {
            // Arrange
            var tracker = new TestFileChangeTracker("C:/_ViewImports.cshtml", Dispatcher);

            // Act
            tracker.StartListening();

            // Assert
            Assert.Equal(1, tracker.AttachToFileServiceEventsCount);
        }

        [ForegroundFact]
        public void StartListening_AlreadyListening_DoesNothing()
        {
            // Arrange
            var tracker = new TestFileChangeTracker("C:/_ViewImports.cshtml", Dispatcher);
            tracker.StartListening();

            // Act
            tracker.StartListening();

            // Assert
            Assert.Equal(1, tracker.AttachToFileServiceEventsCount);
        }

        [ForegroundFact]
        public void StopListening_UnadvisesForFileChange()
        {
            // Arrange
            var tracker = new TestFileChangeTracker("C:/_ViewImports.cshtml", Dispatcher);
            tracker.StartListening(); // Start listening for changes.

            // Act
            tracker.StopListening();

            // Assert
            Assert.Equal(1, tracker.AttachToFileServiceEventsCount);
            Assert.Equal(1, tracker.DetachFromFileServiceEventsCount);
        }

        [ForegroundFact]
        public void StopListening_NotListening_DoesNothing()
        {
            // Arrange
            var tracker = new TestFileChangeTracker("C:/_ViewImports.cshtml", Dispatcher);

            // Act
            tracker.StopListening();

            // Assert

            Assert.Equal(0, tracker.AttachToFileServiceEventsCount);
            Assert.Equal(0, tracker.DetachFromFileServiceEventsCount);
        }

        private class TestFileChangeTracker : VisualStudioMacFileChangeTracker
        {
            public TestFileChangeTracker(
                string filePath, 
                ForegroundDispatcher foregroundDispatcher) : base(filePath, foregroundDispatcher)
            {
            }

            public int AttachToFileServiceEventsCount { get; private set; }

            public int DetachFromFileServiceEventsCount { get; private set; }

            protected override void AttachToFileServiceEvents() => AttachToFileServiceEventsCount++;

            protected override void DetachFromFileServiceEvents() => DetachFromFileServiceEventsCount++;
        }
    }
}
