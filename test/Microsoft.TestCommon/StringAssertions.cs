// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

using System;

namespace Microsoft.TestCommon
{
    // An early copy of the new string assert from xUnit.net 2.0 (temporarily, until it RTMs)

    public partial class Assert
    {
        private const string NullDisplayValue = "(null)";

        /// <summary>
        /// Verifies that two strings are equivalent.
        /// </summary>
        /// <param name="expected">The expected string value.</param>
        /// <param name="actual">The actual string value.</param>
        /// <param name="ignoreCase">If set to <c>true</c>, ignores cases differences. The invariant culture is used.</param>
        /// <param name="ignoreLineEndingDifferences">If set to <c>true</c>, treats \r\n, \r, and \n as equivalent.</param>
        /// <param name="ignoreWhiteSpaceDifferences">If set to <c>true</c>, treats spaces and tabs (in any non-zero quantity) as equivalent.</param>
        /// <exception cref="Microsoft.TestCommon.Assert.StringEqualException">Thrown when the strings are not equivalent.</exception>
        public static void Equal(string expected, string actual, bool ignoreCase = false, bool ignoreLineEndingDifferences = false, bool ignoreWhiteSpaceDifferences = false)
        {
            // Start out assuming the one of the values is null
            int expectedIndex = -1;
            int actualIndex = -1;
            int expectedLength = 0;
            int actualLength = 0;

            if (expected == null)
            {
                if (actual == null)
                {
                    return;
                }

                expected = NullDisplayValue;
            }
            else if (actual == null)
            {
                actual = NullDisplayValue;
            }
            else
            {
                // Walk the string, keeping separate indices since we can skip variable amounts of
                // data based on ignoreLineEndingDifferences and ignoreWhiteSpaceDifferences.
                expectedIndex = 0;
                actualIndex = 0;
                expectedLength = expected.Length;
                actualLength = actual.Length;

                while (expectedIndex < expectedLength && actualIndex < actualLength)
                {
                    char expectedChar = expected[expectedIndex];
                    char actualChar = actual[actualIndex];

                    if (ignoreLineEndingDifferences && IsLineEnding(expectedChar) && IsLineEnding(actualChar))
                    {
                        expectedIndex = SkipLineEnding(expected, expectedIndex);
                        actualIndex = SkipLineEnding(actual, actualIndex);
                    }
                    else if (ignoreWhiteSpaceDifferences && IsWhiteSpace(expectedChar) && IsWhiteSpace(actualChar))
                    {
                        expectedIndex = SkipWhitespace(expected, expectedIndex);
                        actualIndex = SkipWhitespace(actual, actualIndex);
                    }
                    else
                    {
                        if (ignoreCase)
                        {
                            expectedChar = Char.ToUpperInvariant(expectedChar);
                            actualChar = Char.ToUpperInvariant(actualChar);
                        }

                        if (expectedChar != actualChar)
                        {
                            break;
                        }

                        expectedIndex++;
                        actualIndex++;
                    }
                }
            }

            if (expectedIndex < expectedLength || actualIndex < actualLength)
            {
                throw new StringEqualException(expected, actual, expectedIndex, actualIndex);
            }
        }

        private static bool IsLineEnding(char c)
        {
            return c == '\r' || c == '\n';
        }

        private static bool IsWhiteSpace(char c)
        {
            return c == ' ' || c == '\t';
        }

        private static int SkipLineEnding(string value, int index)
        {
            if (value[index] == '\r')
            {
                ++index;
            }
            if (index < value.Length && value[index] == '\n')
            {
                ++index;
            }

            return index;
        }

        private static int SkipWhitespace(string value, int index)
        {
            while (index < value.Length)
            {
                switch (value[index])
                {
                    case ' ':
                    case '\t':
                        index++;
                        break;

                    default:
                        return index;
                }
            }

            return index;
        }
    }
}
