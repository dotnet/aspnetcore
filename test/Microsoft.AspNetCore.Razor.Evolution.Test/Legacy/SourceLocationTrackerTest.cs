// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Xunit;

namespace Microsoft.AspNetCore.Razor.Evolution.Legacy
{
    public class SourceLocationTrackerTest
    {
        private static readonly SourceLocation TestStartLocation = new SourceLocation(10, 42, 45);

        [Fact]
        public void ConstructorSetsCurrentLocationToZero()
        {
            Assert.Equal(SourceLocation.Zero, new SourceLocationTracker().CurrentLocation);
        }

        [Fact]
        public void ConstructorWithSourceLocationSetsCurrentLocationToSpecifiedValue()
        {
            var loc = new SourceLocation(10, 42, 4);
            Assert.Equal(loc, new SourceLocationTracker(loc).CurrentLocation);
        }

        [Fact]
        public void UpdateLocationAdvancesCorrectlyForMultiLineString()
        {
            // Arrange
            var tracker = new SourceLocationTracker(TestStartLocation);

            // Act
            tracker.UpdateLocation("foo\nbar\rbaz\r\nbox");

            // Assert
            Assert.Equal(26, tracker.CurrentLocation.AbsoluteIndex);
            Assert.Equal(45, tracker.CurrentLocation.LineIndex);
            Assert.Equal(3, tracker.CurrentLocation.CharacterIndex);
        }

        [Fact]
        public void UpdateLocationAdvancesAbsoluteIndexOnNonNewlineCharacter()
        {
            // Arrange
            var tracker = new SourceLocationTracker(TestStartLocation);

            // Act
            tracker.UpdateLocation('f', 'o');

            // Assert
            Assert.Equal(11, tracker.CurrentLocation.AbsoluteIndex);
        }

        [Fact]
        public void UpdateLocationAdvancesCharacterIndexOnNonNewlineCharacter()
        {
            // Arrange
            var tracker = new SourceLocationTracker(TestStartLocation);

            // Act
            tracker.UpdateLocation('f', 'o');

            // Assert
            Assert.Equal(46, tracker.CurrentLocation.CharacterIndex);
        }

        [Fact]
        public void UpdateLocationDoesNotAdvanceLineIndexOnNonNewlineCharacter()
        {
            // Arrange
            var tracker = new SourceLocationTracker(TestStartLocation);

            // Act
            tracker.UpdateLocation('f', 'o');

            // Assert
            Assert.Equal(42, tracker.CurrentLocation.LineIndex);
        }

        [Fact]
        public void UpdateLocationAdvancesLineIndexOnSlashN()
        {
            // Arrange
            var tracker = new SourceLocationTracker(TestStartLocation);

            // Act
            tracker.UpdateLocation('\n', 'o');

            // Assert
            Assert.Equal(43, tracker.CurrentLocation.LineIndex);
        }

        [Fact]
        public void UpdateLocationAdvancesAbsoluteIndexOnSlashN()
        {
            // Arrange
            var tracker = new SourceLocationTracker(TestStartLocation);

            // Act
            tracker.UpdateLocation('\n', 'o');

            // Assert
            Assert.Equal(11, tracker.CurrentLocation.AbsoluteIndex);
        }

        [Fact]
        public void UpdateLocationResetsCharacterIndexOnSlashN()
        {
            // Arrange
            var tracker = new SourceLocationTracker(TestStartLocation);

            // Act
            tracker.UpdateLocation('\n', 'o');

            // Assert
            Assert.Equal(0, tracker.CurrentLocation.CharacterIndex);
        }

        [Fact]
        public void UpdateLocationAdvancesLineIndexOnSlashRFollowedByNonNewlineCharacter()
        {
            // Arrange
            var tracker = new SourceLocationTracker(TestStartLocation);

            // Act
            tracker.UpdateLocation('\r', 'o');

            // Assert
            Assert.Equal(43, tracker.CurrentLocation.LineIndex);
        }

        [Fact]
        public void UpdateLocationAdvancesAbsoluteIndexOnSlashRFollowedByNonNewlineCharacter()
        {
            // Arrange
            var tracker = new SourceLocationTracker(TestStartLocation);

            // Act
            tracker.UpdateLocation('\r', 'o');

            // Assert
            Assert.Equal(11, tracker.CurrentLocation.AbsoluteIndex);
        }

        [Fact]
        public void UpdateLocationResetsCharacterIndexOnSlashRFollowedByNonNewlineCharacter()
        {
            // Arrange
            var tracker = new SourceLocationTracker(TestStartLocation);

            // Act
            tracker.UpdateLocation('\r', 'o');

            // Assert
            Assert.Equal(0, tracker.CurrentLocation.CharacterIndex);
        }

        [Fact]
        public void UpdateLocationDoesNotAdvanceLineIndexOnSlashRFollowedBySlashN()
        {
            // Arrange
            var tracker = new SourceLocationTracker(TestStartLocation);

            // Act
            tracker.UpdateLocation('\r', '\n');

            // Assert
            Assert.Equal(42, tracker.CurrentLocation.LineIndex);
        }

        [Fact]
        public void UpdateLocationAdvancesAbsoluteIndexOnSlashRFollowedBySlashN()
        {
            // Arrange
            var tracker = new SourceLocationTracker(TestStartLocation);

            // Act
            tracker.UpdateLocation('\r', '\n');

            // Assert
            Assert.Equal(11, tracker.CurrentLocation.AbsoluteIndex);
        }

        [Fact]
        public void UpdateLocationAdvancesCharacterIndexOnSlashRFollowedBySlashN()
        {
            // Arrange
            var tracker = new SourceLocationTracker(TestStartLocation);

            // Act
            tracker.UpdateLocation('\r', '\n');

            // Assert
            Assert.Equal(46, tracker.CurrentLocation.CharacterIndex);
        }
    }
}
