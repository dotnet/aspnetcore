// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.CodeAnalysis.Razor;
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
        public void ReparseOnForeground_NoopsIfDisposed()
        {
            // Arrange
            var parser = new DefaultVisualStudioRazorParser(
                Dispatcher,
                CreateDocumentTracker(),
                Mock.Of<RazorTemplateEngineFactoryService>(),
                new DefaultErrorReporter(),
                Mock.Of<VisualStudioCompletionBroker>());
            parser.Dispose();

            // Act & Assert
            parser.ReparseOnForeground(null);
        }

        [ForegroundFact]
        public void OnIdle_NoopsIfDisposed()
        {
            // Arrange
            var parser = new DefaultVisualStudioRazorParser(
                Dispatcher,
                CreateDocumentTracker(),
                Mock.Of<RazorTemplateEngineFactoryService>(),
                new DefaultErrorReporter(),
                Mock.Of<VisualStudioCompletionBroker>());
            parser.Dispose();

            // Act & Assert
            parser.OnIdle(null);
        }

        [ForegroundFact]
        public void OnDocumentStructureChanged_NoopsIfDisposed()
        {
            // Arrange
            var parser = new DefaultVisualStudioRazorParser(
                Dispatcher,
                CreateDocumentTracker(),
                Mock.Of<RazorTemplateEngineFactoryService>(),
                new DefaultErrorReporter(),
                Mock.Of<VisualStudioCompletionBroker>());
            parser.Dispose();

            // Act & Assert
            parser.OnDocumentStructureChanged(new object());
        }

        [ForegroundFact]
        public void OnDocumentStructureChanged_IgnoresEditsThatAreOld()
        {
            // Arrange
            using (var parser = new DefaultVisualStudioRazorParser(
                Dispatcher,
                CreateDocumentTracker(),
                Mock.Of<RazorTemplateEngineFactoryService>(),
                new DefaultErrorReporter(),
                Mock.Of<VisualStudioCompletionBroker>()))
            {
                var called = false;
                parser.DocumentStructureChanged += (sender, e) => called = true;
                parser._latestChangeReference = new DefaultVisualStudioRazorParser.ChangeReference(null, new StringTextSnapshot(string.Empty));
                var args = new DocumentStructureChangedEventArgs(
                    new SourceChange(0, 0, string.Empty),
                    new StringTextSnapshot(string.Empty),
                    TestRazorCodeDocument.CreateEmpty());

                // Act
                parser.OnDocumentStructureChanged(args);

                // Assert
                Assert.False(called);
            }
        }

        [ForegroundFact]
        public void OnDocumentStructureChanged_FiresForLatestTextBufferEdit()
        {
            // Arrange
            var documentTracker = CreateDocumentTracker();
            using (var parser = new DefaultVisualStudioRazorParser(
                Dispatcher,
                documentTracker,
                Mock.Of<RazorTemplateEngineFactoryService>(),
                new DefaultErrorReporter(),
                Mock.Of<VisualStudioCompletionBroker>()))
            {
                var called = false;
                parser.DocumentStructureChanged += (sender, e) => called = true;
                var latestChange = new SourceChange(0, 0, string.Empty);
                var latestSnapshot = documentTracker.TextBuffer.CurrentSnapshot;
                parser._latestChangeReference = new DefaultVisualStudioRazorParser.ChangeReference(latestChange, latestSnapshot);
                var codeDocument = TestRazorCodeDocument.CreateEmpty();
                codeDocument.SetSyntaxTree(RazorSyntaxTree.Parse(TestRazorSourceDocument.Create()));
                var args = new DocumentStructureChangedEventArgs(
                    latestChange,
                    latestSnapshot,
                    codeDocument);

                // Act
                parser.OnDocumentStructureChanged(args);

                // Assert
                Assert.True(called);
            }
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
                Mock.Of<VisualStudioCompletionBroker>())
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
                Mock.Of<VisualStudioCompletionBroker>())
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
                Mock.Of<VisualStudioCompletionBroker>()))
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
                Mock.Of<VisualStudioCompletionBroker>()))
            {
                // Act
                parser.StartParser();

                // Assert
                Assert.Equal(1, textBuffer.AttachedChangedEvents.Count);
                Assert.NotNull(parser._parser);
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
                Mock.Of<VisualStudioCompletionBroker>()))
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
                Mock.Of<VisualStudioCompletionBroker>()))
            {
                // Act
                var result = parser.TryReinitializeParser();

                // Assert
                Assert.False(result);
            }
        }
    }
}
