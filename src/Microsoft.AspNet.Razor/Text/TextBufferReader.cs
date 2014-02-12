// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

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
