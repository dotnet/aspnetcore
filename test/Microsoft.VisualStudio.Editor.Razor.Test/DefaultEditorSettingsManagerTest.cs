// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.Razor.Editor;
using Xunit;

namespace Microsoft.VisualStudio.Editor.Razor
{
    public class DefaultEditorSettingsManagerTest : ForegroundDispatcherTestBase
    {
        [Fact]
        public void InitialSettingsAreDefault()
        {
            // Act
            var manager = new DefaultEditorSettingsManager(Dispatcher);

            // Assert
            Assert.Equal(EditorSettings.Default, manager.Current);
        }

        [Fact]
        public void Update_TriggersChangedIfEditorSettingsAreDifferent()
        {
            // Arrange
            var manager = new DefaultEditorSettingsManager(Dispatcher);
            var called = false;
            manager.Changed += (caller, args) =>
            {
                called = true;
            };
            var settings = new EditorSettings(indentWithTabs: true, indentSize: 7);

            // Act
            manager.Update(settings);

            // Assert
            Assert.True(called);
            Assert.Equal(settings, manager.Current);
        }

        [Fact]
        public void Update_DoesNotTriggerChangedIfEditorSettingsAreSame()
        {
            // Arrange
            var manager = new DefaultEditorSettingsManager(Dispatcher);
            var called = false;
            manager.Changed += (caller, args) =>
            {
                called = true;
            };
            var originalSettings = manager.Current;

            // Act
            manager.Update(EditorSettings.Default);

            // Assert
            Assert.False(called);
            Assert.Same(originalSettings, manager.Current);
        }
    }
}
