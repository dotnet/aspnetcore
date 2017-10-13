// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.Razor;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Test;
using Microsoft.VisualStudio.Text;
using Moq;
using Xunit;

namespace Microsoft.VisualStudio.Editor.Razor
{
    public class DefaultVisualStudioRazorParserTest : ForegroundDispatcherTestBase
    {
        private static VisualStudioDocumentTracker CreateDocumentTracker(bool isSupportedProject = true)
        {
            var documentTracker = Mock.Of<VisualStudioDocumentTracker>(tracker =>
            tracker.TextBuffer == new TestTextBuffer(new StringTextSnapshot(string.Empty)) &&
                tracker.ProjectPath == "SomeProject.csproj" &&
                tracker.FilePath == "SomeFilePath.cshtml" &&
                tracker.IsSupportedProject == isSupportedProject);

            return documentTracker;
        }

        [ForegroundFact]
        public void StartIdleTimer_DoesNotRestartTimerWhenAlreadyRunning()
        {
            // Arrange
            using (var parser = new DefaultVisualStudioRazorParser(
                Dispatcher,
                CreateDocumentTracker(),
                Mock.Of<RazorTemplateEngineFactoryService>(),
                new DefaultErrorReporter(),
                Mock.Of<ICompletionBroker>(),
                Enumerable.Empty<IContextChangedListener>())
            {
                BlockBackgroundIdleWork = new ManualResetEventSlim(),
                IdleDelay = TimeSpan.FromSeconds(5)
            })
            {
                parser.StartIdleTimer();
                using (var currentTimer = parser._idleTimer)
                {

                    // Act
                    parser.StartIdleTimer();
                    var afterTimer = parser._idleTimer;

                    // Assert
                    Assert.NotNull(currentTimer);
                    Assert.Same(currentTimer, afterTimer);
                }
            }
        }

        [ForegroundFact]
        public void StopIdleTimer_StopsTimer()
        {
            // Arrange
            using (var parser = new DefaultVisualStudioRazorParser(
                Dispatcher,
                CreateDocumentTracker(),
                Mock.Of<RazorTemplateEngineFactoryService>(),
                new DefaultErrorReporter(),
                Mock.Of<ICompletionBroker>(),
                Enumerable.Empty<IContextChangedListener>())
            {
                BlockBackgroundIdleWork = new ManualResetEventSlim(),
                IdleDelay = TimeSpan.FromSeconds(5)
            })
            {
                parser.StartIdleTimer();
                var currentTimer = parser._idleTimer;

                // Act
                parser.StopIdleTimer();

                // Assert
                Assert.NotNull(currentTimer);
                Assert.Null(parser._idleTimer);
            }
        }

        [ForegroundFact]
        public void StopParser_DetachesFromTextBufferChangeLoop()
        {
            // Arrange
            var documentTracker = CreateDocumentTracker();
            var textBuffer = (TestTextBuffer)documentTracker.TextBuffer;
            using (var parser = new DefaultVisualStudioRazorParser(
                Dispatcher,
                CreateDocumentTracker(),
                Mock.Of<RazorTemplateEngineFactoryService>(),
                new DefaultErrorReporter(),
                Mock.Of<ICompletionBroker>(),
                Enumerable.Empty<IContextChangedListener>()))
            {
                parser.StartParser();

                // Act
                parser.StopParser();

                // Assert
                Assert.Empty(textBuffer.AttachedChangedEvents);
                Assert.Null(parser._parser);
            }
        }

        [ForegroundFact]
        public void StartParser_AttachesToTextBufferChangeLoop()
        {
            // Arrange
            var documentTracker = CreateDocumentTracker();
            var textBuffer = (TestTextBuffer)documentTracker.TextBuffer;
            using (var parser = new DefaultVisualStudioRazorParser(
                Dispatcher,
                documentTracker,
                Mock.Of<RazorTemplateEngineFactoryService>(),
                new DefaultErrorReporter(),
                Mock.Of<ICompletionBroker>(),
                Enumerable.Empty<IContextChangedListener>()))
            {
                // Act
                parser.StartParser();

                // Assert
                Assert.Equal(1, textBuffer.AttachedChangedEvents.Count);
                Assert.NotNull(parser._parser);
            }
        }

        [ForegroundFact]
        public void NotifyParserContextChanged_NotifiesListeners()
        {
            // Arrange
            var listener1 = new Mock<IContextChangedListener>();
            listener1.Setup(l => l.OnContextChanged(It.IsAny<VisualStudioRazorParser>()));
            var listener2 = new Mock<IContextChangedListener>();
            listener2.Setup(l => l.OnContextChanged(It.IsAny<VisualStudioRazorParser>()));
            using (var parser = new DefaultVisualStudioRazorParser(
                Dispatcher,
                CreateDocumentTracker(),
                Mock.Of<RazorTemplateEngineFactoryService>(),
                new DefaultErrorReporter(),
                Mock.Of<ICompletionBroker>(),
                new[] { listener1.Object, listener2.Object }))
            {
                // Act
                parser.NotifyParserContextChanged();

                // Assert
                listener1.Verify();
                listener2.Verify();
            }
        }

        [ForegroundFact]
        public void TryReinitializeParser_ReturnsTrue_IfProjectIsSupported()
        {
            // Arrange
            using (var parser = new DefaultVisualStudioRazorParser(
                Dispatcher,
                CreateDocumentTracker(isSupportedProject: true),
                Mock.Of<RazorTemplateEngineFactoryService>(),
                new DefaultErrorReporter(),
                Mock.Of<ICompletionBroker>(),
                Enumerable.Empty<IContextChangedListener>()))
            {
                // Act
                var result = parser.TryReinitializeParser();

                // Assert
                Assert.True(result);
            }
        }

        [ForegroundFact]
        public void TryReinitializeParser_ReturnsFalse_IfProjectIsNotSupported()
        {
            // Arrange
            using (var parser = new DefaultVisualStudioRazorParser(
                Dispatcher,
                CreateDocumentTracker(isSupportedProject: false),
                Mock.Of<RazorTemplateEngineFactoryService>(),
                new DefaultErrorReporter(),
                Mock.Of<ICompletionBroker>(),
                Enumerable.Empty<IContextChangedListener>()))
            {
                // Act
                var result = parser.TryReinitializeParser();

                // Assert
                Assert.False(result);
            }
        }
    }
}
