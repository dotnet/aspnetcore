// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.ObjectModel;
using Microsoft.CodeAnalysis;
using Microsoft.VisualStudio.Editor.Razor;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Moq;
using Xunit;

namespace Microsoft.VisualStudio.LanguageServices.Razor.Editor
{
    public class RazorTextViewConnectionListenerTest : ForegroundDispatcherTestBase
    {
        [ForegroundFact]
        public void SubjectBuffersConnected_CallsRazorDocumentManager_OnTextViewOpened()
        {
            // Arrange
            var textView = Mock.Of<IWpfTextView>();
            var buffers = new Collection<ITextBuffer>();
            var workspace = new AdhocWorkspace();
            var documentManager = new Mock<RazorDocumentManager>(MockBehavior.Strict);
            documentManager.Setup(d => d.OnTextViewOpened(textView, buffers)).Verifiable();

            var listener = new RazorTextViewConnectionListener(Dispatcher, workspace, documentManager.Object);

            // Act
            listener.SubjectBuffersConnected(textView, ConnectionReason.BufferGraphChange, buffers);

            // Assert
            documentManager.Verify();
        }

        [ForegroundFact]
        public void SubjectBuffersDisonnected_CallsRazorDocumentManager_OnTextViewClosed()
        {
            // Arrange
            var textView = Mock.Of<IWpfTextView>();
            var buffers = new Collection<ITextBuffer>();
            var workspace = new AdhocWorkspace();
            var documentManager = new Mock<RazorDocumentManager>(MockBehavior.Strict);
            documentManager.Setup(d => d.OnTextViewClosed(textView, buffers)).Verifiable();

            var listener = new RazorTextViewConnectionListener(Dispatcher, workspace, documentManager.Object);

            // Act
            listener.SubjectBuffersDisconnected(textView, ConnectionReason.BufferGraphChange, buffers);

            // Assert
            documentManager.Verify();
        }
    }
}
