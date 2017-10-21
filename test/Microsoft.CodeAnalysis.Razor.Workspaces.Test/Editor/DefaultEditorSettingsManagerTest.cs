// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Xunit;

namespace Microsoft.CodeAnalysis.Razor.Editor
{
    public class DefaultEditorSettingsManagerTest
    {
        [Fact]
        public void InitialSettingsAreDefault()
        {
            // Act
            var manager = new DefaultEditorSettingsManager();

            // Assert
            Assert.Equal(EditorSettings.Default, manager.Current);
        }

        [Fact]
        public void Update_TriggersChangedIfEditorSettingsAreDifferent()
        {
            // Arrange
            var manager = new DefaultEditorSettingsManager();
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
            var manager = new DefaultEditorSettingsManager();
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
