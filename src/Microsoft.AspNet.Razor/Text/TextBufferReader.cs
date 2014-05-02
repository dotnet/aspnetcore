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

using System;
using System.Collections.Generic;
using Microsoft.AspNet.Razor.Utils;

namespace Microsoft.AspNet.Razor.Text
{
    public class TextBufferReader : LookaheadTextReader
    {
        private Stack<BacktrackContext> _bookmarks = new Stack<BacktrackContext>();
        private SourceLocationTracker _tracker = new SourceLocationTracker();

        public TextBufferReader(ITextBuffer buffer)
        {
            if (buffer == null)
            {
                throw new ArgumentNullException("buffer");
            }

            InnerBuffer = buffer;
        }

        internal ITextBuffer InnerBuffer { get; private set; }

        public override SourceLocation CurrentLocation
        {
            get { return _tracker.CurrentLocation; }
        }

        public override int Peek()
        {
            return InnerBuffer.Peek();
        }

        public override int Read()
        {
            int read = InnerBuffer.Read();
            if (read != -1)
            {
                char nextChar = '\0';
                int next = Peek();
                if (next != -1)
                {
                    nextChar = (char)next;
                }
                _tracker.UpdateLocation((char)read, nextChar);
            }
            return read;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                IDisposable disposable = InnerBuffer as IDisposable;
                if (disposable != null)
                {
                    disposable.Dispose();
                }
            }
            base.Dispose(disposing);
        }

        public override IDisposable BeginLookahead()
        {
            BacktrackContext context = new BacktrackContext() { Location = CurrentLocation };
            _bookmarks.Push(context);
            return new DisposableAction(() =>
            {
                EndLookahead(context);
            });
        }

        public override void CancelBacktrack()
        {
            if (_bookmarks.Count == 0)
            {
                throw new InvalidOperationException(RazorResources.CancelBacktrack_Must_Be_Called_Within_Lookahead);
            }
            _bookmarks.Pop();
        }

        private void EndLookahead(BacktrackContext context)
        {
            if (_bookmarks.Count > 0 && ReferenceEquals(_bookmarks.Peek(), context))
            {
                // Backtrack wasn't cancelled, so pop it
                _bookmarks.Pop();

                // Set the new current location
                _tracker.CurrentLocation = context.Location;
                InnerBuffer.Position = context.Location.AbsoluteIndex;
            }
        }

        /// <summary>
        /// Need a class for reference equality to support cancelling backtrack.
        /// </summary>
        private class BacktrackContext
        {
            public SourceLocation Location { get; set; }
        }
    }
}
