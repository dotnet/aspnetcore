// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
using System.Xml.Linq;

namespace Microsoft.AspNetCore.DataProtection;

/// <summary>
/// Helpful XML-based assertions.
/// </summary>
public static class XmlAssert
{
    public static readonly IEqualityComparer<XNode> EqualityComparer = new CallbackBasedEqualityComparer<XNode>(Core.AreEqual);

    /// <summary>
    /// Asserts that a <see cref="string"/> and an <see cref="XElement"/> are semantically equivalent.
    /// </summary>
    public static void Equal(string expected, XElement actual)
    {
        Assert.NotNull(expected);
        Assert.NotNull(actual);
        Equal(XElement.Parse(expected), actual);
    }

    /// <summary>
    /// Asserts that two <see cref="XElement"/> instances are semantically equivalent.
    /// </summary>
    public static void Equal(XElement expected, XElement actual)
    {
        Assert.NotNull(expected);
        Assert.NotNull(actual);

        if (!Core.AreEqual(expected, actual))
        {
            Assert.True(false,
                   "Expected element:" + Environment.NewLine
                   + expected.ToString() + Environment.NewLine
                   + "Actual element:" + Environment.NewLine
                   + actual.ToString());
        }
    }

    private static class Core
    {
        private static readonly IEqualityComparer<XAttribute> AttributeEqualityComparer = new CallbackBasedEqualityComparer<XAttribute>(AreEqual);

        private static bool AreEqual(XElement expected, XElement actual)
        {
            return expected.Name == actual.Name
                && AreEqual(expected.Attributes(), actual.Attributes())
                && AreEqual(expected.Nodes(), actual.Nodes());
        }

        private static bool AreEqual(IEnumerable<XNode> expected, IEnumerable<XNode> actual)
        {
            List<XNode> filteredExpected = expected.Where(ShouldIncludeNodeDuringComparison).ToList();
            List<XNode> filteredActual = actual.Where(ShouldIncludeNodeDuringComparison).ToList();
            return filteredExpected.SequenceEqual(filteredActual, EqualityComparer);
        }

        internal static bool AreEqual(XNode expected, XNode actual)
        {
            if (expected is XText && actual is XText)
            {
                return AreEqual((XText)expected, (XText)actual);
            }
            else if (expected is XElement && actual is XElement)
            {
                return AreEqual((XElement)expected, (XElement)actual);
            }
            else
            {
                return false;
            }
        }

        private static bool AreEqual(XText expected, XText actual)
        {
            return expected.Value == actual.Value;
        }

        private static bool AreEqual(IEnumerable<XAttribute> expected, IEnumerable<XAttribute> actual)
        {
            List<XAttribute> orderedExpected = expected
                .Where(ShouldIncludeAttributeDuringComparison)
                .OrderBy(attr => attr.Name.ToString())
                .ToList();

            List<XAttribute> orderedActual = actual
                .Where(ShouldIncludeAttributeDuringComparison)
                .OrderBy(attr => attr.Name.ToString())
                .ToList();

            return orderedExpected.SequenceEqual(orderedActual, AttributeEqualityComparer);
        }

        private static bool AreEqual(XAttribute expected, XAttribute actual)
        {
            return expected.Name == actual.Name
                && expected.Value == actual.Value;
        }

        private static bool ShouldIncludeAttributeDuringComparison(XAttribute attribute)
        {
            // exclude 'xmlns' attributes since they're already considered in the
            // actual element and attribute names
            return attribute.Name != (XName)"xmlns"
                && attribute.Name.Namespace != XNamespace.Xmlns;
        }

        private static bool ShouldIncludeNodeDuringComparison(XNode node)
        {
            if (node is XComment)
            {
                return false; // not contextually relevant
            }

            if (node is XText /* includes XCData */ || node is XElement)
            {
                return true; // relevant
            }

            throw new NotSupportedException(string.Format(CultureInfo.InvariantCulture, "Node of type '{0}' is not supported.", node.GetType().Name));
        }
    }

    private sealed class CallbackBasedEqualityComparer<T> : IEqualityComparer<T>
    {
        private readonly Func<T, T, bool> _equalityCheck;

        public CallbackBasedEqualityComparer(Func<T, T, bool> equalityCheck)
        {
            _equalityCheck = equalityCheck;
        }

        public bool Equals(T x, T y)
        {
            return _equalityCheck(x, y);
        }

        public int GetHashCode(T obj)
        {
            return obj.ToString().GetHashCode();
        }
    }
}
