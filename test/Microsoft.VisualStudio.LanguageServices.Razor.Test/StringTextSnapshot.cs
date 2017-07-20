// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Utilities;

namespace Microsoft.AspNetCore.Razor.Language.Legacy
{
    public class StringTextSnapshot : ITextSnapshot
    {
        private readonly string _content;

        public StringTextSnapshot(string content)
        {
            _content = content;
        }

        public char this[int position] => _content[position];

        public VisualStudio.Text.ITextBuffer TextBuffer => throw new NotImplementedException();

        public IContentType ContentType => throw new NotImplementedException();

        public ITextVersion Version => throw new NotImplementedException();

        public int Length => _content.Length;

        public int LineCount => throw new NotImplementedException();

        public IEnumerable<ITextSnapshotLine> Lines => throw new NotImplementedException();

        public void CopyTo(int sourceIndex, char[] destination, int destinationIndex, int count)
        {
            _content.CopyTo(sourceIndex, destination, destinationIndex, count);
        }

        public ITrackingPoint CreateTrackingPoint(int position, PointTrackingMode trackingMode)
        {
            throw new NotImplementedException();
        }

        public ITrackingPoint CreateTrackingPoint(int position, PointTrackingMode trackingMode, TrackingFidelityMode trackingFidelity)
        {
            throw new NotImplementedException();
        }

        public ITrackingSpan CreateTrackingSpan(VisualStudio.Text.Span span, SpanTrackingMode trackingMode)
        {
            throw new NotImplementedException();
        }

        public ITrackingSpan CreateTrackingSpan(VisualStudio.Text.Span span, SpanTrackingMode trackingMode, TrackingFidelityMode trackingFidelity)
        {
            throw new NotImplementedException();
        }

        public ITrackingSpan CreateTrackingSpan(int start, int length, SpanTrackingMode trackingMode)
        {
            throw new NotImplementedException();
        }

        public ITrackingSpan CreateTrackingSpan(int start, int length, SpanTrackingMode trackingMode, TrackingFidelityMode trackingFidelity)
        {
            throw new NotImplementedException();
        }

        public ITextSnapshotLine GetLineFromLineNumber(int lineNumber)
        {
            throw new NotImplementedException();
        }

        public ITextSnapshotLine GetLineFromPosition(int position)
        {
            throw new NotImplementedException();
        }

        public int GetLineNumberFromPosition(int position)
        {
            throw new NotImplementedException();
        }

        public string GetText(VisualStudio.Text.Span span)
        {
            throw new NotImplementedException();
        }

        public string GetText(int startIndex, int length) => _content.Substring(startIndex, length);

        public string GetText() => _content;

        public char[] ToCharArray(int startIndex, int length) => _content.ToCharArray();

        public void Write(TextWriter writer, VisualStudio.Text.Span span)
        {
            throw new NotImplementedException();
        }

        public void Write(TextWriter writer)
        {
            throw new NotImplementedException();
        }
    }
}
