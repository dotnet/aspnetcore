// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Microsoft.AspNet.Razor.Utils;

namespace Microsoft.AspNet.Razor.Text
{
    public class BufferingTextReader : LookaheadTextReader
    {
        private Stack<BacktrackContext> _backtrackStack = new Stack<BacktrackContext>();
        private int _currentBufferPosition;

        private int _currentCharacter;
        private SourceLocationTracker _locationTracker;

        public BufferingTextReader(TextReader source)
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            InnerReader = source;
            _locationTracker = new SourceLocationTracker();

            UpdateCurrentCharacter();
        }

        internal StringBuilder Buffer { get; set; }
        internal bool Buffering { get; set; }
        internal TextReader InnerReader { get; private set; }

        public override SourceLocation CurrentLocation
        {
            get { return _locationTracker.CurrentLocation; }
        }

        protected virtual int CurrentCharacter
        {
            get { return _currentCharacter; }
        }

        public override int Read()
        {
            var ch = CurrentCharacter;
            NextCharacter();
            return ch;
        }

        // TODO: Optimize Read(char[],int,int) to copy direct from the buffer where possible

        public override int Peek()
        {
            return CurrentCharacter;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                InnerReader.Dispose();
            }
            base.Dispose(disposing);
        }

        public override IDisposable BeginLookahead()
        {
            // Is this our first lookahead?
            if (Buffer == null)
            {
                // Yes, setup the backtrack buffer
                Buffer = new StringBuilder();
            }

            if (!Buffering)
            {
                // We're not already buffering, so we need to expand the buffer to hold the first character
                ExpandBuffer();
                Buffering = true;
            }

            // Mark the position to return to when we backtrack
            // Use the closures and the "using" statement rather than an explicit stack
            var context = new BacktrackContext()
            {
                BufferIndex = _currentBufferPosition,
                Location = CurrentLocation
            };
            _backtrackStack.Push(context);
            return new DisposableAction(() =>
            {
                EndLookahead(context);
            });
        }

        // REVIEW: This really doesn't sound like the best name for this...
        public override void CancelBacktrack()
        {
            if (_backtrackStack.Count == 0)
            {
                throw new InvalidOperationException(RazorResources.CancelBacktrack_Must_Be_Called_Within_Lookahead);
            }
            // Just pop the current backtrack context so that when the lookahead ends, it won't be backtracked
            _backtrackStack.Pop();
        }

        private void EndLookahead(BacktrackContext context)
        {
            // If the specified context is not the one on the stack, it was popped by a call to DoNotBacktrack
            if (_backtrackStack.Count > 0 && ReferenceEquals(_backtrackStack.Peek(), context))
            {
                _backtrackStack.Pop();
                _currentBufferPosition = context.BufferIndex;
                _locationTracker.CurrentLocation = context.Location;

                UpdateCurrentCharacter();
            }
        }

        protected virtual void NextCharacter()
        {
            var prevChar = CurrentCharacter;
            if (prevChar == -1)
            {
                return; // We're at the end of the source
            }

            if (Buffering)
            {
                if (_currentBufferPosition >= Buffer.Length - 1)
                {
                    // If there are no more lookaheads (thus no need to continue with the buffer) we can just clean up the buffer
                    if (_backtrackStack.Count == 0)
                    {
                        // Reset the buffer
                        Buffer.Length = 0;
                        _currentBufferPosition = 0;
                        Buffering = false;
                    }
                    else if (!ExpandBuffer())
                    {
                        // Failed to expand the buffer, because we're at the end of the source
                        _currentBufferPosition = Buffer.Length; // Force the position past the end of the buffer
                    }
                }
                else
                {
                    // Not at the end yet, just advance the buffer pointer
                    _currentBufferPosition++;
                }
            }
            else
            {
                // Just act like normal
                InnerReader.Read(); // Don't care about the return value, Peek() is used to get characters from the source
            }

            UpdateCurrentCharacter();
            _locationTracker.UpdateLocation((char)prevChar, (char)CurrentCharacter);
        }

        protected bool ExpandBuffer()
        {
            // Pull another character into the buffer and update the position
            var ch = InnerReader.Read();

            // Only append the character to the buffer if there actually is one
            if (ch != -1)
            {
                Buffer.Append((char)ch);
                _currentBufferPosition = Buffer.Length - 1;
                return true;
            }
            return false;
        }

        private void UpdateCurrentCharacter()
        {
            if (Buffering && _currentBufferPosition < Buffer.Length)
            {
                // Read from the buffer
                _currentCharacter = (int)Buffer[_currentBufferPosition];
            }
            else
            {
                // No buffer? Peek from the source
                _currentCharacter = InnerReader.Peek();
            }
        }

        private class BacktrackContext
        {
            public int BufferIndex { get; set; }
            public SourceLocation Location { get; set; }
        }
    }
}
