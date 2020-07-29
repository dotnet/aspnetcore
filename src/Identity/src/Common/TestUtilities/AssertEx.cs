// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using Xunit;

namespace TestUtilities
{
    /// <summary>
    /// Assert style type to deal with the lack of features in xUnit's Assert type
    /// </summary>
    public static class AssertEx
    {
        #region AssertEqualityComparer<T>

        private class AssertEqualityComparer<T> : IEqualityComparer<T>
        {
            private static readonly IEqualityComparer<T> s_instance = new AssertEqualityComparer<T>();

            private static bool CanBeNull()
            {
                var type = typeof(T);
                return !type.GetTypeInfo().IsValueType ||
                    (type.GetTypeInfo().IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>));
            }

            public static bool IsNull(T @object)
            {
                if (!CanBeNull())
                {
                    return false;
                }

                return object.Equals(@object, default(T));
            }

            public static bool Equals(T left, T right)
            {
                return s_instance.Equals(left, right);
            }

            bool IEqualityComparer<T>.Equals(T x, T y)
            {
                if (CanBeNull())
                {
                    if (object.Equals(x, default(T)))
                    {
                        return object.Equals(y, default(T));
                    }

                    if (object.Equals(y, default(T)))
                    {
                        return false;
                    }
                }

                if (x.GetType() != y.GetType())
                {
                    return false;
                }

                var equatable = x as IEquatable<T>;
                if (equatable != null)
                {
                    return equatable.Equals(y);
                }

                var comparableT = x as IComparable<T>;
                if (comparableT != null)
                {
                    return comparableT.CompareTo(y) == 0;
                }

                var comparable = x as IComparable;
                if (comparable != null)
                {
                    return comparable.CompareTo(y) == 0;
                }

                var enumerableX = x as IEnumerable;
                var enumerableY = y as IEnumerable;

                if (enumerableX != null && enumerableY != null)
                {
                    var enumeratorX = enumerableX.GetEnumerator();
                    var enumeratorY = enumerableY.GetEnumerator();

                    while (true)
                    {
                        bool hasNextX = enumeratorX.MoveNext();
                        bool hasNextY = enumeratorY.MoveNext();

                        if (!hasNextX || !hasNextY)
                        {
                            return hasNextX == hasNextY;
                        }

                        if (!Equals(enumeratorX.Current, enumeratorY.Current))
                        {
                            return false;
                        }
                    }
                }

                return object.Equals(x, y);
            }

            int IEqualityComparer<T>.GetHashCode(T obj)
            {
                throw new NotImplementedException();
            }
        }

        #endregion

        public static void AreEqual<T>(T expected, T actual, string message = null, IEqualityComparer<T> comparer = null)
        {
            if (ReferenceEquals(expected, actual))
            {
                return;
            }

            if (expected == null)
            {
                Fail("expected was null, but actual wasn't\r\n" + message);
            }
            else if (actual == null)
            {
                Fail("actual was null, but expected wasn't\r\n" + message);
            }
            else
            {
                if (!(comparer != null ?
                    comparer.Equals(expected, actual) :
                    AssertEqualityComparer<T>.Equals(expected, actual)))
                {
                    Fail("Expected and actual were different.\r\n" +
                         "Expected: " + expected + "\r\n" +
                         "Actual:   " + actual + "\r\n" +
                         message);
                }
            }
        }

        public static void Equal<T>(ImmutableArray<T> expected, IEnumerable<T> actual, Func<T, T, bool> comparer = null, string message = null)
        {
            if (actual == null || expected.IsDefault)
            {
                Assert.True((actual == null) == expected.IsDefault, message);
            }
            else
            {
                Equal((IEnumerable<T>)expected, actual, comparer, message);
            }
        }

        public static void Equal<T>(IEnumerable<T> expected, ImmutableArray<T> actual, Func<T, T, bool> comparer = null, string message = null, string itemSeparator = null)
        {
            if (expected == null || actual.IsDefault)
            {
                Assert.True((expected == null) == actual.IsDefault, message);
            }
            else
            {
                Equal(expected, (IEnumerable<T>)actual, comparer, message, itemSeparator);
            }
        }

        public static void Equal<T>(ImmutableArray<T> expected, ImmutableArray<T> actual, Func<T, T, bool> comparer = null, string message = null, string itemSeparator = null)
        {
            Equal(expected, (IEnumerable<T>)actual, comparer, message, itemSeparator);
        }

        public static void Equal<T>(IEnumerable<T> expected, IEnumerable<T> actual, Func<T, T, bool> comparer = null, string message = null,
            string itemSeparator = null, Func<T, string> itemInspector = null)
        {
            if (ReferenceEquals(expected, actual))
            {
                return;
            }

            if (expected == null)
            {
                Fail("expected was null, but actual wasn't\r\n" + message);
            }
            else if (actual == null)
            {
                Fail("actual was null, but expected wasn't\r\n" + message);
            }
            else if (!SequenceEqual(expected, actual, comparer))
            {
                string assertMessage = GetAssertMessage(expected, actual, comparer, itemInspector, itemSeparator);

                if (message != null)
                {
                    assertMessage = message + "\r\n" + assertMessage;
                }

                Assert.True(false, assertMessage);
            }
        }

        private static bool SequenceEqual<T>(IEnumerable<T> expected, IEnumerable<T> actual, Func<T, T, bool> comparer = null)
        {
            var enumerator1 = expected.GetEnumerator();
            var enumerator2 = actual.GetEnumerator();

            while (true)
            {
                var hasNext1 = enumerator1.MoveNext();
                var hasNext2 = enumerator2.MoveNext();

                if (hasNext1 != hasNext2)
                {
                    return false;
                }

                if (!hasNext1)
                {
                    break;
                }

                var value1 = enumerator1.Current;
                var value2 = enumerator2.Current;

                if (!(comparer != null ? comparer(value1, value2) : AssertEqualityComparer<T>.Equals(value1, value2)))
                {
                    return false;
                }
            }

            return true;
        }

        public static void SetEqual<T>(IEnumerable<T> expected, IEnumerable<T> actual, IEqualityComparer<T> comparer = null, string message = null, string itemSeparator = "\r\n")
        {
            var expectedSet = new HashSet<T>(expected, comparer);
            var result = expected.Count() == actual.Count() && expectedSet.SetEquals(actual);
            if (!result)
            {
                if (string.IsNullOrEmpty(message))
                {
                    message = GetAssertMessage(expected, actual);
                }

                Assert.True(result, message);
            }
        }

        public static void SetEqual<T>(IEnumerable<T> actual, params T[] expected)
        {
            var expectedSet = new HashSet<T>(expected);
            Assert.True(expectedSet.SetEquals(actual), string.Format("Expected: {0}\nActual: {1}", ToString(expected), ToString(actual)));
        }

        public static void None<T>(IEnumerable<T> actual, Func<T, bool> predicate)
        {
            var none = !actual.Any(predicate);
            if (!none)
            {
                Assert.True(none, string.Format(
                    "Unexpected item found among existing items: {0}\nExisting items: {1}",
                    ToString(actual.First(predicate)),
                    ToString(actual)));
            }
        }

        public static void Any<T>(IEnumerable<T> actual, Func<T, bool> predicate)
        {
            var any = actual.Any(predicate);
            Assert.True(any, string.Format("No expected item was found.\nExisting items: {0}", ToString(actual)));
        }

        public static void All<T>(IEnumerable<T> actual, Func<T, bool> predicate)
        {
            var all = actual.All(predicate);
            if (!all)
            {
                Assert.True(all, string.Format(
                    "Not all items satisfy condition:\n{0}",
                    ToString(actual.Where(i => !predicate(i)))));
            }
        }

        public static string ToString(object o)
        {
            return Convert.ToString(o);
        }

        public static string ToString<T>(IEnumerable<T> list, string separator = ", ", Func<T, string> itemInspector = null)
        {
            if (itemInspector == null)
            {
                itemInspector = i => Convert.ToString(i);
            }

            return string.Join(separator, list.Select(itemInspector));
        }

        public static void Fail(string message)
        {
            Assert.False(true, message);
        }

        public static void Fail(string format, params object[] args)
        {
            Assert.False(true, string.Format(format, args));
        }

        public static void Null<T>(T @object, string message = null)
        {
            Assert.True(AssertEqualityComparer<T>.IsNull(@object), message);
        }

        public static void NotNull<T>(T @object, string message = null)
        {
            Assert.False(AssertEqualityComparer<T>.IsNull(@object), message);
        }

        // compares against a baseline
        public static void AssertEqualToleratingWhitespaceDifferences(
            string expected,
            string actual,
            bool escapeQuotes = true,
            [CallerFilePath]string expectedValueSourcePath = null,
            [CallerLineNumber]int expectedValueSourceLine = 0)
        {
            if (!EqualIgnoringWhitespace(expected, actual))
            {
                Assert.True(false, GetAssertMessage(expected, actual, escapeQuotes, expectedValueSourcePath, expectedValueSourceLine));
            }
        }

        public static bool EqualIgnoringWhitespace(string left, string right)
            => NormalizeWhitespace(left) == NormalizeWhitespace(right);

        public static void ThrowsArgumentNull(string parameterName, Action del)
        {
            try
            {
                del();
            }
            catch (ArgumentNullException e)
            {
                Assert.Equal(parameterName, e.ParamName);
            }
        }

        public static void ThrowsArgumentException(string parameterName, Action del)
        {
            try
            {
                del();
            }
            catch (ArgumentException e)
            {
                Assert.Equal(parameterName, e.ParamName);
            }
        }

        public static T Throws<T>(Action del, bool allowDerived = false) where T : Exception
        {
            try
            {
                del();
            }
            catch (Exception ex)
            {
                var type = ex.GetType();
                if (type.Equals(typeof(T)))
                {
                    // We got exactly the type we wanted
                    return (T)ex;
                }

                if (allowDerived && typeof(T).GetTypeInfo().IsAssignableFrom(type.GetTypeInfo()))
                {
                    // We got a derived type
                    return (T)ex;
                }

                // We got some other type. We know that type != typeof(T), and so we'll use Assert.Equal since Xunit
                // will give a nice Expected/Actual output for this
                Assert.Equal(typeof(T), type);
            }

            throw new Exception("No exception was thrown.");
        }

        internal static string NormalizeWhitespace(string input)
        {
            var output = new StringBuilder();
            var inputLines = input.Split('\n', '\r');
            foreach (var line in inputLines)
            {
                var trimmedLine = line.Trim();
                if (trimmedLine.Length > 0)
                {
                    if (!(trimmedLine[0] == '{' || trimmedLine[0] == '}'))
                    {
                        output.Append("  ");
                    }

                    output.AppendLine(trimmedLine);
                }
            }

            return output.ToString();
        }

        public static string GetAssertMessage(string expected, string actual, bool escapeQuotes = false, string expectedValueSourcePath = null, int expectedValueSourceLine = 0)
        {
            return GetAssertMessage(DiffUtil.Lines(expected), DiffUtil.Lines(actual), escapeQuotes, expectedValueSourcePath, expectedValueSourceLine);
        }

        public static string GetAssertMessage<T>(IEnumerable<T> expected, IEnumerable<T> actual, bool escapeQuotes, string expectedValueSourcePath = null, int expectedValueSourceLine = 0)
        {
            Func<T, string> itemInspector = escapeQuotes ? new Func<T, string>(t => t.ToString().Replace("\"", "\"\"")) : null;
            return GetAssertMessage(expected, actual, itemInspector: itemInspector, itemSeparator: "\r\n", expectedValueSourcePath: expectedValueSourcePath, expectedValueSourceLine: expectedValueSourceLine);
        }

        public static string GetAssertMessage<T>(
            IEnumerable<T> expected,
            IEnumerable<T> actual,
            Func<T, T, bool> comparer = null,
            Func<T, string> itemInspector = null,
            string itemSeparator = null,
            string expectedValueSourcePath = null,
            int expectedValueSourceLine = 0)
        {
            if (itemInspector == null)
            {
                if (expected is IEnumerable<byte>)
                {
                    itemInspector = b => $"0x{b:X2}";
                }
                else
                {
                    itemInspector = new Func<T, string>(obj => (obj != null) ? obj.ToString() : "<null>");
                }
            }

            if (itemSeparator == null)
            {
                if (expected is IEnumerable<byte>)
                {
                    itemSeparator = ", ";
                }
                else
                {
                    itemSeparator = ",\r\n";
                }
            }

            var message = new StringBuilder();
            message.AppendLine();
            message.AppendLine("Actual:");
            message.AppendLine(string.Join(itemSeparator, actual.Select(itemInspector)));

            message.AppendLine();
            message.AppendLine("Expected:");
            message.AppendLine(string.Join(itemSeparator, expected.Select(itemInspector)));

            message.AppendLine();
            message.AppendLine("Diff:");
            message.Append(DiffUtil.DiffReport(expected, actual, comparer, itemInspector, itemSeparator));

            return message.ToString();
        }

        public static void AssertLinesEqual(string expected, string actual, string message = null, Func<string, string, bool> comparer = null)
        {
            if (expected == actual)
            {
                return;
            }

            Assert.NotNull(expected);
            Assert.NotNull(actual);

            IEnumerable<string> GetLines(string str) => 
                str.Trim().Replace("\r\n", "\n").Split(new[] { '\r', '\n' }, StringSplitOptions.None);

            Equal(
                GetLines(expected), 
                GetLines(actual),
                message: message,
                comparer: comparer ?? new Func<string, string, bool>((left, right) => left.Trim() == right.Trim()),
                itemInspector: line => line.Replace("\"", "\"\""),
                itemSeparator: Environment.NewLine);
        }

    }
}
