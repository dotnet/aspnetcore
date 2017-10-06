// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Utilities;

namespace Microsoft.VisualStudio.Test
{
    public class TestTextBuffer : ITextBuffer
    {
        private ITextSnapshot _currentSnapshot;

        public TestTextBuffer(ITextSnapshot initialSnapshot)
        {
            _currentSnapshot = initialSnapshot;
            ReadOnlyRegionsChanged += (sender, args) => { };
            ChangedLowPriority += (sender, args) => { };
            ChangedHighPriority += (sender, args) => { };
            Changing += (sender, args) => { };
            PostChanged += (sender, args) => { };
            ContentTypeChanged += (sender, args) => { };
            Properties = new PropertyCollection();
        }

        public void ApplyEdit(TestEdit edit)
        {
            ApplyEdits(edit);
        }

        public void ApplyEdits(params TestEdit[] edits)
        {
            var args = new TextContentChangedEventArgs(edits[0].OldSnapshot, edits[edits.Length - 1].NewSnapshot, new EditOptions(), null);
            foreach (var edit in edits)
            {
                args.Changes.Add(new TestTextChange(edit.Change));
            }

            _currentSnapshot = edits[edits.Length - 1].NewSnapshot;

            Changed?.Invoke(this, args);
            PostChanged?.Invoke(null, null);

            ReadOnlyRegionsChanged?.Invoke(null, null);
            ChangedLowPriority?.Invoke(null, null);
            ChangedHighPriority?.Invoke(null, null);
            Changing?.Invoke(null, null);
            ContentTypeChanged?.Invoke(null, null);
        }

        public ITextSnapshot CurrentSnapshot => _currentSnapshot;

        public PropertyCollection Properties { get; }

        public event EventHandler<SnapshotSpanEventArgs> ReadOnlyRegionsChanged;
        public event EventHandler<TextContentChangedEventArgs> Changed;
        public event EventHandler<TextContentChangedEventArgs> ChangedLowPriority;
        public event EventHandler<TextContentChangedEventArgs> ChangedHighPriority;
        public event EventHandler<TextContentChangingEventArgs> Changing;
        public event EventHandler PostChanged;
        public event EventHandler<ContentTypeChangedEventArgs> ContentTypeChanged;

        public bool EditInProgress => throw new NotImplementedException();

        public IContentType ContentType => throw new NotImplementedException();

        public ITextEdit CreateEdit() => new BufferEdit(this);

        public void ChangeContentType(IContentType newContentType, object editTag) => throw new NotImplementedException();

        public bool CheckEditAccess() => throw new NotImplementedException();

        public ITextEdit CreateEdit(EditOptions options, int? reiteratedVersionNumber, object editTag) => throw new NotImplementedException();

        public IReadOnlyRegionEdit CreateReadOnlyRegionEdit() => throw new NotImplementedException();

        public ITextSnapshot Delete(Span deleteSpan) => throw new NotImplementedException();

        public NormalizedSpanCollection GetReadOnlyExtents(Span span) => throw new NotImplementedException();

        public ITextSnapshot Insert(int position, string text) => throw new NotImplementedException();

        public bool IsReadOnly(int position) => throw new NotImplementedException();

        public bool IsReadOnly(int position, bool isEdit) => throw new NotImplementedException();

        public bool IsReadOnly(Span span) => throw new NotImplementedException();

        public bool IsReadOnly(Span span, bool isEdit) => throw new NotImplementedException();

        public ITextSnapshot Replace(Text.Span replaceSpan, string replaceWith) => throw new NotImplementedException();

        public void TakeThreadOwnership() => throw new NotImplementedException();

        private class BufferEdit : ITextEdit
        {
            private readonly TestTextBuffer _textBuffer;
            private readonly List<TestEdit> _edits;

            public BufferEdit(TestTextBuffer textBuffer)
            {
                _textBuffer = textBuffer;
                _edits = new List<TestEdit>();
            }

            public bool HasEffectiveChanges => throw new NotImplementedException();

            public bool HasFailedChanges => throw new NotImplementedException();

            public ITextSnapshot Snapshot => throw new NotImplementedException();

            public bool Canceled => throw new NotImplementedException();

            public ITextSnapshot Apply()
            {
                _textBuffer.ApplyEdits(_edits.ToArray());
                _edits.Clear();

                return _textBuffer.CurrentSnapshot;
            }

            public bool Insert(int position, string text)
            {
                var initialSnapshot = (StringTextSnapshot)_textBuffer.CurrentSnapshot;
                var newText = initialSnapshot.Content.Insert(position, text);
                var changedSnapshot = new StringTextSnapshot(newText);
                var edit = new TestEdit(position, 0, initialSnapshot, text.Length, changedSnapshot, text);
                _edits.Add(edit);

                return true;
            }

            public void Cancel() => throw new NotImplementedException();

            public bool Delete(Span deleteSpan) => throw new NotImplementedException();

            public bool Delete(int startPosition, int charsToDelete) => throw new NotImplementedException();

            public void Dispose() => throw new NotImplementedException();

            public bool Insert(int position, char[] characterBuffer, int startIndex, int length) => throw new NotImplementedException();

            public bool Replace(Span replaceSpan, string replaceWith) => throw new NotImplementedException();

            public bool Replace(int startPosition, int charsToReplace, string replaceWith) => throw new NotImplementedException();
        }
    }
}
