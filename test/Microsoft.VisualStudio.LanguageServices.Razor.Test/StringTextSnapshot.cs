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
        public StringTextSnapshot(string content)
        {
            Content = content;
        }

        public string Content { get; }

        public char this[int position] => Content[position];

        public ITextVersion Version { get; } = new TextVersion();

        public int Length => Content.Length;

        public VisualStudio.Text.ITextBuffer TextBuffer => throw new NotImplementedException();

        public IContentType ContentType => throw new NotImplementedException();

        public int LineCount => throw new NotImplementedException();

        public IEnumerable<ITextSnapshotLine> Lines => throw new NotImplementedException();

        public void CopyTo(int sourceIndex, char[] destination, int destinationIndex, int count) => Content.CopyTo(sourceIndex, destination, destinationIndex, count);

        public string GetText(int startIndex, int length) => Content.Substring(startIndex, length);

        public string GetText() => Content;

        public char[] ToCharArray(int startIndex, int length) => Content.ToCharArray();

        public ITrackingPoint CreateTrackingPoint(int position, PointTrackingMode trackingMode) => throw new NotImplementedException();

        public ITrackingPoint CreateTrackingPoint(int position, PointTrackingMode trackingMode, TrackingFidelityMode trackingFidelity) => throw new NotImplementedException();

        public ITrackingSpan CreateTrackingSpan(VisualStudio.Text.Span span, SpanTrackingMode trackingMode) => throw new NotImplementedException();

        public ITrackingSpan CreateTrackingSpan(VisualStudio.Text.Span span, SpanTrackingMode trackingMode, TrackingFidelityMode trackingFidelity) => throw new NotImplementedException();

        public ITrackingSpan CreateTrackingSpan(int start, int length, SpanTrackingMode trackingMode) => throw new NotImplementedException();

        public ITrackingSpan CreateTrackingSpan(int start, int length, SpanTrackingMode trackingMode, TrackingFidelityMode trackingFidelity) => throw new NotImplementedException();

        public ITextSnapshotLine GetLineFromLineNumber(int lineNumber) => throw new NotImplementedException();

        public ITextSnapshotLine GetLineFromPosition(int position) => throw new NotImplementedException();

        public int GetLineNumberFromPosition(int position) => throw new NotImplementedException();

        public string GetText(VisualStudio.Text.Span span) => throw new NotImplementedException();

        public void Write(TextWriter writer, VisualStudio.Text.Span span) => throw new NotImplementedException();

        public void Write(TextWriter writer) => throw new NotImplementedException();

        private class TextVersion : ITextVersion
        {
            public INormalizedTextChangeCollection Changes { get; } = new TextChangeCollection();

            public ITextVersion Next => throw new NotImplementedException();

            public int Length => throw new NotImplementedException();

            public VisualStudio.Text.ITextBuffer TextBuffer => throw new NotImplementedException();

            public int VersionNumber => throw new NotImplementedException();

            public int ReiteratedVersionNumber => throw new NotImplementedException();

            public ITrackingSpan CreateCustomTrackingSpan(VisualStudio.Text.Span span, TrackingFidelityMode trackingFidelity, object customState, CustomTrackToVersion behavior) => throw new NotImplementedException();

            public ITrackingPoint CreateTrackingPoint(int position, PointTrackingMode trackingMode) => throw new NotImplementedException();

            public ITrackingPoint CreateTrackingPoint(int position, PointTrackingMode trackingMode, TrackingFidelityMode trackingFidelity) => throw new NotImplementedException();

            public ITrackingSpan CreateTrackingSpan(VisualStudio.Text.Span span, SpanTrackingMode trackingMode) => throw new NotImplementedException();

            public ITrackingSpan CreateTrackingSpan(VisualStudio.Text.Span span, SpanTrackingMode trackingMode, TrackingFidelityMode trackingFidelity) => throw new NotImplementedException();

            public ITrackingSpan CreateTrackingSpan(int start, int length, SpanTrackingMode trackingMode) => throw new NotImplementedException();

            public ITrackingSpan CreateTrackingSpan(int start, int length, SpanTrackingMode trackingMode, TrackingFidelityMode trackingFidelity) => throw new NotImplementedException();

            private class TextChangeCollection : List<ITextChange>, INormalizedTextChangeCollection
            {
                public bool IncludesLineChanges => false;
            }
        }
    }
}
