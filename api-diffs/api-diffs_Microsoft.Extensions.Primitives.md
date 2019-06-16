# Microsoft.Extensions.Primitives

``` diff
 namespace Microsoft.Extensions.Primitives {
     public class CancellationChangeToken : IChangeToken {
         public CancellationChangeToken(CancellationToken cancellationToken);
         public bool ActiveChangeCallbacks { get; private set; }
         public bool HasChanged { get; }
         public IDisposable RegisterChangeCallback(Action<object> callback, object state);
     }
     public static class ChangeToken {
         public static IDisposable OnChange(Func<IChangeToken> changeTokenProducer, Action changeTokenConsumer);
         public static IDisposable OnChange<TState>(Func<IChangeToken> changeTokenProducer, Action<TState> changeTokenConsumer, TState state);
     }
     public class CompositeChangeToken : IChangeToken {
         public CompositeChangeToken(IReadOnlyList<IChangeToken> changeTokens);
         public bool ActiveChangeCallbacks { get; }
         public IReadOnlyList<IChangeToken> ChangeTokens { get; }
         public bool HasChanged { get; }
         public IDisposable RegisterChangeCallback(Action<object> callback, object state);
     }
     public static class Extensions {
         public static StringBuilder Append(this StringBuilder builder, StringSegment segment);
     }
     public interface IChangeToken {
         bool ActiveChangeCallbacks { get; }
         bool HasChanged { get; }
         IDisposable RegisterChangeCallback(Action<object> callback, object state);
     }
     public struct InplaceStringBuilder {
         public InplaceStringBuilder(int capacity);
         public int Capacity { get; set; }
         public void Append(StringSegment segment);
         public void Append(char c);
         public void Append(string value);
         public void Append(string value, int offset, int count);
         public override string ToString();
     }
     public readonly struct StringSegment : IEquatable<string>, IEquatable<StringSegment> {
         public static readonly StringSegment Empty;
         public StringSegment(string buffer);
         public StringSegment(string buffer, int offset, int length);
         public string Buffer { get; }
         public bool HasValue { get; }
         public int Length { get; }
         public int Offset { get; }
         public char this[int index] { get; }
         public string Value { get; }
         public ReadOnlyMemory<char> AsMemory();
         public ReadOnlySpan<char> AsSpan();
         public static int Compare(StringSegment a, StringSegment b, StringComparison comparisonType);
         public bool EndsWith(string text, StringComparison comparisonType);
         public bool Equals(StringSegment other);
         public static bool Equals(StringSegment a, StringSegment b, StringComparison comparisonType);
         public bool Equals(StringSegment other, StringComparison comparisonType);
         public override bool Equals(object obj);
         public bool Equals(string text);
         public bool Equals(string text, StringComparison comparisonType);
         public override int GetHashCode();
         public int IndexOf(char c);
         public int IndexOf(char c, int start);
         public int IndexOf(char c, int start, int count);
         public int IndexOfAny(char[] anyOf);
         public int IndexOfAny(char[] anyOf, int startIndex);
         public int IndexOfAny(char[] anyOf, int startIndex, int count);
         public static bool IsNullOrEmpty(StringSegment value);
         public int LastIndexOf(char value);
         public static bool operator ==(StringSegment left, StringSegment right);
         public static implicit operator ReadOnlySpan<char> (StringSegment segment);
         public static implicit operator ReadOnlyMemory<char> (StringSegment segment);
         public static implicit operator StringSegment (string value);
         public static bool operator !=(StringSegment left, StringSegment right);
         public StringTokenizer Split(char[] chars);
         public bool StartsWith(string text, StringComparison comparisonType);
         public StringSegment Subsegment(int offset);
         public StringSegment Subsegment(int offset, int length);
         public string Substring(int offset);
         public string Substring(int offset, int length);
         public override string ToString();
         public StringSegment Trim();
         public StringSegment TrimEnd();
         public StringSegment TrimStart();
     }
     public class StringSegmentComparer : IComparer<StringSegment>, IEqualityComparer<StringSegment> {
         public static StringSegmentComparer Ordinal { get; }
         public static StringSegmentComparer OrdinalIgnoreCase { get; }
         public int Compare(StringSegment x, StringSegment y);
         public bool Equals(StringSegment x, StringSegment y);
         public int GetHashCode(StringSegment obj);
     }
     public readonly struct StringTokenizer : IEnumerable, IEnumerable<StringSegment> {
         public StringTokenizer(StringSegment value, char[] separators);
         public StringTokenizer(string value, char[] separators);
         public StringTokenizer.Enumerator GetEnumerator();
         IEnumerator<StringSegment> System.Collections.Generic.IEnumerable<Microsoft.Extensions.Primitives.StringSegment>.GetEnumerator();
         IEnumerator System.Collections.IEnumerable.GetEnumerator();
         public struct Enumerator : IDisposable, IEnumerator, IEnumerator<StringSegment> {
             public Enumerator(ref StringTokenizer tokenizer);
             public StringSegment Current { get; private set; }
             object System.Collections.IEnumerator.Current { get; }
             public void Dispose();
             public bool MoveNext();
             public void Reset();
         }
     }
     public readonly struct StringValues : ICollection<string>, IEnumerable, IEnumerable<string>, IEquatable<string>, IEquatable<StringValues>, IEquatable<string[]>, IList<string>, IReadOnlyCollection<string>, IReadOnlyList<string> {
         public static readonly StringValues Empty;
         public StringValues(string value);
         public StringValues(string[] values);
         public int Count { get; }
         bool System.Collections.Generic.ICollection<System.String>.IsReadOnly { get; }
         string System.Collections.Generic.IList<System.String>.this[int index] { get; set; }
         public string this[int index] { get; }
         public static StringValues Concat(StringValues values1, StringValues values2);
         public static StringValues Concat(in StringValues values, string value);
         public static StringValues Concat(string value, in StringValues values);
         public bool Equals(StringValues other);
         public static bool Equals(StringValues left, StringValues right);
         public static bool Equals(StringValues left, string right);
         public static bool Equals(StringValues left, string[] right);
         public override bool Equals(object obj);
         public bool Equals(string other);
         public static bool Equals(string left, StringValues right);
         public bool Equals(string[] other);
         public static bool Equals(string[] left, StringValues right);
         public StringValues.Enumerator GetEnumerator();
         public override int GetHashCode();
         public static bool IsNullOrEmpty(StringValues value);
         public static bool operator ==(StringValues left, StringValues right);
         public static bool operator ==(StringValues left, object right);
         public static bool operator ==(StringValues left, string right);
         public static bool operator ==(StringValues left, string[] right);
         public static bool operator ==(object left, StringValues right);
         public static bool operator ==(string left, StringValues right);
         public static bool operator ==(string[] left, StringValues right);
         public static implicit operator string (StringValues values);
         public static implicit operator string[] (StringValues value);
         public static implicit operator StringValues (string value);
         public static implicit operator StringValues (string[] values);
         public static bool operator !=(StringValues left, StringValues right);
         public static bool operator !=(StringValues left, object right);
         public static bool operator !=(StringValues left, string right);
         public static bool operator !=(StringValues left, string[] right);
         public static bool operator !=(object left, StringValues right);
         public static bool operator !=(string left, StringValues right);
         public static bool operator !=(string[] left, StringValues right);
         void System.Collections.Generic.ICollection<System.String>.Add(string item);
         void System.Collections.Generic.ICollection<System.String>.Clear();
         bool System.Collections.Generic.ICollection<System.String>.Contains(string item);
         void System.Collections.Generic.ICollection<System.String>.CopyTo(string[] array, int arrayIndex);
         bool System.Collections.Generic.ICollection<System.String>.Remove(string item);
         IEnumerator<string> System.Collections.Generic.IEnumerable<System.String>.GetEnumerator();
         int System.Collections.Generic.IList<System.String>.IndexOf(string item);
         void System.Collections.Generic.IList<System.String>.Insert(int index, string item);
         void System.Collections.Generic.IList<System.String>.RemoveAt(int index);
         IEnumerator System.Collections.IEnumerable.GetEnumerator();
         public string[] ToArray();
         public override string ToString();
         public struct Enumerator : IDisposable, IEnumerator, IEnumerator<string> {
             public Enumerator(ref StringValues values);
             public string Current { get; }
             object System.Collections.IEnumerator.Current { get; }
             public void Dispose();
             public bool MoveNext();
             void System.Collections.IEnumerator.Reset();
         }
     }
 }
```

