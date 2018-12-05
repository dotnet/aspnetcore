// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.CodeAnalysis.Razor;
using Microsoft.CodeAnalysis.Razor.Editor;
using Moq;
using Xunit;

namespace Microsoft.VisualStudio.Editor.Razor
{
    public class DefaultWorkspaceEditorSettingsTest : ForegroundDispatcherTestBase
    {
        [Fact]
        public void InitialSettingsAreEditorSettingsManagerDefault()
        {
            // Arrange
            var editorSettings = new EditorSettings(true, 123);
            var editorSettingsManager = Mock.Of<EditorSettingsManager>(m => m.Current == editorSettings);

            // Act
            var manager = new DefaultWorkspaceEditorSettings(Dispatcher, editorSettingsManager);

            // Assert
            Assert.Equal(editorSettings, manager.Current);
        }

        [Fact]
        public void OnChanged_TriggersChanged()
        {
            // Arrange
            var manager = new DefaultWorkspaceEditorSettings(Dispatcher, Mock.Of<EditorSettingsManager>());
            var called = false;
            manager.Changed += (caller, args) =>
            {
                called = true;
            };

            // Act
            manager.OnChanged(null, null);

            // Assert
            Assert.True(called);
        }

        [Fact]
        public void Attach_CalledOnceForMultipleListeners()
        {
            // Arrange
            var manager = new TestEditorSettingsManagerInternal(Dispatcher);

            // Act
            manager.Changed += (caller, args) => { };
            manager.Changed += (caller, args) => { };

            // Assert
            Assert.Equal(1, manager.AttachCount);
        }

        [Fact]
        public void Detach_CalledOnceWhenNoMoreListeners()
        {
            // Arrange
            var manager = new TestEditorSettingsManagerInternal(Dispatcher);
            EventHandler<EditorSettingsChangedEventArgs> listener1 = (caller, args) => { };
            EventHandler<EditorSettingsChangedEventArgs> listener2 = (caller, args) => { };
            manager.Changed += listener1;
            manager.Changed += listener2;

            // Act
            manager.Changed -= listener1;
            manager.Changed -= listener2;

            // Assert
            Assert.Equal(1, manager.DetachCount);
        }

        private class TestEditorSettingsManagerInternal : DefaultWorkspaceEditorSettings
        {
            public TestEditorSettingsManagerInternal(ForegroundDispatcher foregroundDispatcher) : base(foregroundDispatcher, Mock.Of<EditorSettingsManager>())
            {
            }

            public int AttachCount { get; private set; }

            public int DetachCount { get; private set; }

            internal override void AttachToEditorSettingsManager()
            {
                AttachCount++;
            }

            internal override void DetachFromEditorSettingsManager()
            {
                DetachCount++;
            }
        }
    }
}
