# Microsoft.CodeAnalysis.Text

``` diff
-namespace Microsoft.CodeAnalysis.Text {
 {
-    public struct LinePosition : IComparable<LinePosition>, IEquatable<LinePosition> {
 {
-        public LinePosition(int line, int character);

-        public int Character { get; }

-        public int Line { get; }

-        public static LinePosition Zero { get; }

-        public int CompareTo(LinePosition other);

-        public bool Equals(LinePosition other);

-        public override bool Equals(object obj);

-        public override int GetHashCode();

-        public static bool operator ==(LinePosition left, LinePosition right);

-        public static bool operator >(LinePosition left, LinePosition right);

-        public static bool operator >=(LinePosition left, LinePosition right);

-        public static bool operator !=(LinePosition left, LinePosition right);

-        public static bool operator <(LinePosition left, LinePosition right);

-        public static bool operator <=(LinePosition left, LinePosition right);

-        public override string ToString();

-    }
-    public struct LinePositionSpan : IEquatable<LinePositionSpan> {
 {
-        public LinePositionSpan(LinePosition start, LinePosition end);

-        public LinePosition End { get; }

-        public LinePosition Start { get; }

-        public bool Equals(LinePositionSpan other);

-        public override bool Equals(object obj);

-        public override int GetHashCode();

-        public static bool operator ==(LinePositionSpan left, LinePositionSpan right);

-        public static bool operator !=(LinePositionSpan left, LinePositionSpan right);

-        public override string ToString();

-    }
-    public enum SourceHashAlgorithm {
 {
-        None = 0,

-        Sha1 = 1,

-        Sha256 = 2,

-    }
-    public abstract class SourceText {
 {
-        protected SourceText(ImmutableArray<byte> checksum = default(ImmutableArray<byte>), SourceHashAlgorithm checksumAlgorithm = SourceHashAlgorithm.Sha1, SourceTextContainer container = null);

-        public bool CanBeEmbedded { get; }

-        public SourceHashAlgorithm ChecksumAlgorithm { get; }

-        public virtual SourceTextContainer Container { get; }

-        public abstract Encoding Encoding { get; }

-        public abstract int Length { get; }

-        public TextLineCollection Lines { get; }

-        public abstract char this[int position] { get; }

-        public bool ContentEquals(SourceText other);

-        protected virtual bool ContentEqualsImpl(SourceText other);

-        public abstract void CopyTo(int sourceIndex, char[] destination, int destinationIndex, int count);

-        public static SourceText From(byte[] buffer, int length, Encoding encoding, SourceHashAlgorithm checksumAlgorithm, bool throwIfBinaryDetected);

-        public static SourceText From(byte[] buffer, int length, Encoding encoding = null, SourceHashAlgorithm checksumAlgorithm = SourceHashAlgorithm.Sha1, bool throwIfBinaryDetected = false, bool canBeEmbedded = false);

-        public static SourceText From(Stream stream, Encoding encoding, SourceHashAlgorithm checksumAlgorithm, bool throwIfBinaryDetected);

-        public static SourceText From(Stream stream, Encoding encoding = null, SourceHashAlgorithm checksumAlgorithm = SourceHashAlgorithm.Sha1, bool throwIfBinaryDetected = false, bool canBeEmbedded = false);

-        public static SourceText From(TextReader reader, int length, Encoding encoding = null, SourceHashAlgorithm checksumAlgorithm = SourceHashAlgorithm.Sha1);

-        public static SourceText From(string text, Encoding encoding = null, SourceHashAlgorithm checksumAlgorithm = SourceHashAlgorithm.Sha1);

-        public virtual IReadOnlyList<TextChangeRange> GetChangeRanges(SourceText oldText);

-        public ImmutableArray<byte> GetChecksum();

-        protected virtual TextLineCollection GetLinesCore();

-        public virtual SourceText GetSubText(TextSpan span);

-        public SourceText GetSubText(int start);

-        public virtual IReadOnlyList<TextChange> GetTextChanges(SourceText oldText);

-        public SourceText Replace(TextSpan span, string newText);

-        public SourceText Replace(int start, int length, string newText);

-        public override string ToString();

-        public virtual string ToString(TextSpan span);

-        public SourceText WithChanges(params TextChange[] changes);

-        public virtual SourceText WithChanges(IEnumerable<TextChange> changes);

-        public virtual void Write(TextWriter writer, TextSpan span, CancellationToken cancellationToken = default(CancellationToken));

-        public void Write(TextWriter textWriter, CancellationToken cancellationToken = default(CancellationToken));

-    }
-    public abstract class SourceTextContainer {
 {
-        protected SourceTextContainer();

-        public abstract SourceText CurrentText { get; }

-        public abstract event EventHandler<TextChangeEventArgs> TextChanged;

-    }
-    public struct TextChange : IEquatable<TextChange> {
 {
-        public TextChange(TextSpan span, string newText);

-        public string NewText { get; }

-        public static IReadOnlyList<TextChange> NoChanges { get; }

-        public TextSpan Span { get; }

-        public bool Equals(TextChange other);

-        public override bool Equals(object obj);

-        public override int GetHashCode();

-        public static bool operator ==(TextChange left, TextChange right);

-        public static implicit operator TextChangeRange (TextChange change);

-        public static bool operator !=(TextChange left, TextChange right);

-        public override string ToString();

-    }
-    public class TextChangeEventArgs : EventArgs {
 {
-        public TextChangeEventArgs(SourceText oldText, SourceText newText, params TextChangeRange[] changes);

-        public TextChangeEventArgs(SourceText oldText, SourceText newText, IEnumerable<TextChangeRange> changes);

-        public IReadOnlyList<TextChangeRange> Changes { get; }

-        public SourceText NewText { get; }

-        public SourceText OldText { get; }

-    }
-    public struct TextChangeRange : IEquatable<TextChangeRange> {
 {
-        public TextChangeRange(TextSpan span, int newLength);

-        public int NewLength { get; }

-        public static IReadOnlyList<TextChangeRange> NoChanges { get; }

-        public TextSpan Span { get; }

-        public static TextChangeRange Collapse(IEnumerable<TextChangeRange> changes);

-        public bool Equals(TextChangeRange other);

-        public override bool Equals(object obj);

-        public override int GetHashCode();

-        public static bool operator ==(TextChangeRange left, TextChangeRange right);

-        public static bool operator !=(TextChangeRange left, TextChangeRange right);

-    }
-    public struct TextLine : IEquatable<TextLine> {
 {
-        public int End { get; }

-        public int EndIncludingLineBreak { get; }

-        public int LineNumber { get; }

-        public TextSpan Span { get; }

-        public TextSpan SpanIncludingLineBreak { get; }

-        public int Start { get; }

-        public SourceText Text { get; }

-        public bool Equals(TextLine other);

-        public override bool Equals(object obj);

-        public static TextLine FromSpan(SourceText text, TextSpan span);

-        public override int GetHashCode();

-        public static bool operator ==(TextLine left, TextLine right);

-        public static bool operator !=(TextLine left, TextLine right);

-        public override string ToString();

-    }
-    public abstract class TextLineCollection : IEnumerable, IEnumerable<TextLine>, IReadOnlyCollection<TextLine>, IReadOnlyList<TextLine> {
 {
-        protected TextLineCollection();

-        public abstract int Count { get; }

-        public abstract TextLine this[int index] { get; }

-        public TextLineCollection.Enumerator GetEnumerator();

-        public virtual TextLine GetLineFromPosition(int position);

-        public virtual LinePosition GetLinePosition(int position);

-        public LinePositionSpan GetLinePositionSpan(TextSpan span);

-        public int GetPosition(LinePosition position);

-        public TextSpan GetTextSpan(LinePositionSpan span);

-        public abstract int IndexOf(int position);

-        IEnumerator<TextLine> System.Collections.Generic.IEnumerable<Microsoft.CodeAnalysis.Text.TextLine>.GetEnumerator();

-        IEnumerator System.Collections.IEnumerable.GetEnumerator();

-        public struct Enumerator : IDisposable, IEnumerator, IEnumerator<TextLine> {
 {
-            public TextLine Current { get; }

-            object System.Collections.IEnumerator.Current { get; }

-            public override bool Equals(object obj);

-            public override int GetHashCode();

-            public bool MoveNext();

-            bool System.Collections.IEnumerator.MoveNext();

-            void System.Collections.IEnumerator.Reset();

-            void System.IDisposable.Dispose();

-        }
-    }
-    public struct TextSpan : IComparable<TextSpan>, IEquatable<TextSpan> {
 {
-        public TextSpan(int start, int length);

-        public int End { get; }

-        public bool IsEmpty { get; }

-        public int Length { get; }

-        public int Start { get; }

-        public int CompareTo(TextSpan other);

-        public bool Contains(TextSpan span);

-        public bool Contains(int position);

-        public bool Equals(TextSpan other);

-        public override bool Equals(object obj);

-        public static TextSpan FromBounds(int start, int end);

-        public override int GetHashCode();

-        public Nullable<TextSpan> Intersection(TextSpan span);

-        public bool IntersectsWith(TextSpan span);

-        public bool IntersectsWith(int position);

-        public static bool operator ==(TextSpan left, TextSpan right);

-        public static bool operator !=(TextSpan left, TextSpan right);

-        public Nullable<TextSpan> Overlap(TextSpan span);

-        public bool OverlapsWith(TextSpan span);

-        public override string ToString();

-    }
-}
```

