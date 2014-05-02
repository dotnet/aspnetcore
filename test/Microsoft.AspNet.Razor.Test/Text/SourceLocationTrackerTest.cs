// Copyright (c) Microsoft Open Technologies, Inc.
// All Rights Reserved
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// http://www.apache.org/licenses/LICENSE-2.0
//
// THIS CODE IS PROVIDED *AS IS* BASIS, WITHOUT WARRANTIES OR
// CONDITIONS OF ANY KIND, EITHER EXPRESS OR IMPLIED, INCLUDING
// WITHOUT LIMITATION ANY IMPLIED WARRANTIES OR CONDITIONS OF
// TITLE, FITNESS FOR A PARTICULAR PURPOSE, MERCHANTABLITY OR
// NON-INFRINGEMENT.
// See the Apache 2 License for the specific language governing
// permissions and limitations under the License.

using Microsoft.AspNet.Razor.Text;
using Microsoft.TestCommon;

namespace Microsoft.AspNet.Razor.Test.Text
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
            SourceLocation loc = new SourceLocation(10, 42, 4);
            Assert.Equal(loc, new SourceLocationTracker(loc).CurrentLocation);
        }

        [Fact]
        public void UpdateLocationAdvancesCorrectlyForMultiLineString()
        {
            // Arrange
            SourceLocationTracker tracker = new SourceLocationTracker(TestStartLocation);

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
            SourceLocationTracker tracker = new SourceLocationTracker(TestStartLocation);

            // Act
            tracker.UpdateLocation('f', 'o');

            // Assert
            Assert.Equal(11, tracker.CurrentLocation.AbsoluteIndex);
        }

        [Fact]
        public void UpdateLocationAdvancesCharacterIndexOnNonNewlineCharacter()
        {
            // Arrange
            SourceLocationTracker tracker = new SourceLocationTracker(TestStartLocation);

            // Act
            tracker.UpdateLocation('f', 'o');

            // Assert
            Assert.Equal(46, tracker.CurrentLocation.CharacterIndex);
        }

        [Fact]
        public void UpdateLocationDoesNotAdvanceLineIndexOnNonNewlineCharacter()
        {
            // Arrange
            SourceLocationTracker tracker = new SourceLocationTracker(TestStartLocation);

            // Act
            tracker.UpdateLocation('f', 'o');

            // Assert
            Assert.Equal(42, tracker.CurrentLocation.LineIndex);
        }

        [Fact]
        public void UpdateLocationAdvancesLineIndexOnSlashN()
        {
            // Arrange
            SourceLocationTracker tracker = new SourceLocationTracker(TestStartLocation);

            // Act
            tracker.UpdateLocation('\n', 'o');

            // Assert
            Assert.Equal(43, tracker.CurrentLocation.LineIndex);
        }

        [Fact]
        public void UpdateLocationAdvancesAbsoluteIndexOnSlashN()
        {
            // Arrange
            SourceLocationTracker tracker = new SourceLocationTracker(TestStartLocation);

            // Act
            tracker.UpdateLocation('\n', 'o');

            // Assert
            Assert.Equal(11, tracker.CurrentLocation.AbsoluteIndex);
        }

        [Fact]
        public void UpdateLocationResetsCharacterIndexOnSlashN()
        {
            // Arrange
            SourceLocationTracker tracker = new SourceLocationTracker(TestStartLocation);

            // Act
            tracker.UpdateLocation('\n', 'o');

            // Assert
            Assert.Equal(0, tracker.CurrentLocation.CharacterIndex);
        }

        [Fact]
        public void UpdateLocationAdvancesLineIndexOnSlashRFollowedByNonNewlineCharacter()
        {
            // Arrange
            SourceLocationTracker tracker = new SourceLocationTracker(TestStartLocation);

            // Act
            tracker.UpdateLocation('\r', 'o');

            // Assert
            Assert.Equal(43, tracker.CurrentLocation.LineIndex);
        }

        [Fact]
        public void UpdateLocationAdvancesAbsoluteIndexOnSlashRFollowedByNonNewlineCharacter()
        {
            // Arrange
            SourceLocationTracker tracker = new SourceLocationTracker(TestStartLocation);

            // Act
            tracker.UpdateLocation('\r', 'o');

            // Assert
            Assert.Equal(11, tracker.CurrentLocation.AbsoluteIndex);
        }

        [Fact]
        public void UpdateLocationResetsCharacterIndexOnSlashRFollowedByNonNewlineCharacter()
        {
            // Arrange
            SourceLocationTracker tracker = new SourceLocationTracker(TestStartLocation);

            // Act
            tracker.UpdateLocation('\r', 'o');

            // Assert
            Assert.Equal(0, tracker.CurrentLocation.CharacterIndex);
        }

        [Fact]
        public void UpdateLocationDoesNotAdvanceLineIndexOnSlashRFollowedBySlashN()
        {
            // Arrange
            SourceLocationTracker tracker = new SourceLocationTracker(TestStartLocation);

            // Act
            tracker.UpdateLocation('\r', '\n');

            // Assert
            Assert.Equal(42, tracker.CurrentLocation.LineIndex);
        }

        [Fact]
        public void UpdateLocationAdvancesAbsoluteIndexOnSlashRFollowedBySlashN()
        {
            // Arrange
            SourceLocationTracker tracker = new SourceLocationTracker(TestStartLocation);

            // Act
            tracker.UpdateLocation('\r', '\n');

            // Assert
            Assert.Equal(11, tracker.CurrentLocation.AbsoluteIndex);
        }

        [Fact]
        public void UpdateLocationAdvancesCharacterIndexOnSlashRFollowedBySlashN()
        {
            // Arrange
            SourceLocationTracker tracker = new SourceLocationTracker(TestStartLocation);

            // Act
            tracker.UpdateLocation('\r', '\n');

            // Assert
            Assert.Equal(46, tracker.CurrentLocation.CharacterIndex);
        }
    }
}
