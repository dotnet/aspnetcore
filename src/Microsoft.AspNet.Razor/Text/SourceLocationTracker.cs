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

using Microsoft.AspNet.Razor.Parser;

namespace Microsoft.AspNet.Razor.Text
{
    public class SourceLocationTracker
    {
        private int _absoluteIndex = 0;
        private int _characterIndex = 0;
        private int _lineIndex = 0;
        private SourceLocation _currentLocation;

        public SourceLocationTracker()
            : this(SourceLocation.Zero)
        {
        }

        public SourceLocationTracker(SourceLocation currentLocation)
        {
            CurrentLocation = currentLocation;

            UpdateInternalState();
        }

        public SourceLocation CurrentLocation
        {
            get
            {
                return _currentLocation;
            }
            set
            {
                if (_currentLocation != value)
                {
                    _currentLocation = value;
                    UpdateInternalState();
                }
            }
        }

        public void UpdateLocation(char characterRead, char nextCharacter)
        {
            UpdateCharacterCore(characterRead, nextCharacter);
            RecalculateSourceLocation();
        }

        public SourceLocationTracker UpdateLocation(string content)
        {
            for (int i = 0; i < content.Length; i++)
            {
                char nextCharacter = '\0';
                if (i < content.Length - 1)
                {
                    nextCharacter = content[i + 1];
                }
                UpdateCharacterCore(content[i], nextCharacter);
            }
            RecalculateSourceLocation();
            return this;
        }

        private void UpdateCharacterCore(char characterRead, char nextCharacter)
        {
            _absoluteIndex++;

            if (ParserHelpers.IsNewLine(characterRead) && (characterRead != '\r' || nextCharacter != '\n'))
            {
                _lineIndex++;
                _characterIndex = 0;
            }
            else
            {
                _characterIndex++;
            }
        }

        private void UpdateInternalState()
        {
            _absoluteIndex = CurrentLocation.AbsoluteIndex;
            _characterIndex = CurrentLocation.CharacterIndex;
            _lineIndex = CurrentLocation.LineIndex;
        }

        private void RecalculateSourceLocation()
        {
            _currentLocation = new SourceLocation(_absoluteIndex, _lineIndex, _characterIndex);
        }

        public static SourceLocation CalculateNewLocation(SourceLocation lastPosition, string newContent)
        {
            return new SourceLocationTracker(lastPosition).UpdateLocation(newContent).CurrentLocation;
        }
    }
}
