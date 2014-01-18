// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using Xunit.Sdk;

namespace Microsoft.TestCommon
{
    // An early copy of the new string assert exception from xUnit.net 2.0 (temporarily, until it RTMs)

    [Serializable]
    internal class StringEqualException : AssertException
    {
        private static Dictionary<char, string> _encodings = new Dictionary<char, string> { { '\r', "\\r" }, { '\n', "\\n" }, { '\t', "\\t" }, { '\0', "\\0" } };
        private string _message;

        public StringEqualException(string expected, string actual, int expectedIndex, int actualIndex)
            : base("Assert.Equal() failure")
        {
            Actual = actual;
            ActualIndex = actualIndex;
            Expected = expected;
            ExpectedIndex = expectedIndex;
        }

        public string Actual { get; private set; }

        public int ActualIndex { get; private set; }

        public string Expected { get; private set; }

        public int ExpectedIndex { get; private set; }

        public override string Message
        {
            get
            {
                if (_message == null)
                    _message = CreateMessage();

                return _message;
            }
        }

        private string CreateMessage()
        {
            Tuple<string, string> printedExpected = ShortenAndEncode(Expected, ExpectedIndex, '↓');
            Tuple<string, string> printedActual = ShortenAndEncode(Actual, ActualIndex, '↑');

            return String.Format(
                CultureInfo.CurrentCulture,
                "{1}{0}          {2}{0}Expected: {3}{0}Actual:   {4}{0}          {5}",
                Environment.NewLine,
                base.Message,
                printedExpected.Item2,
                printedExpected.Item1,
                printedActual.Item1,
                printedActual.Item2
            );
        }

        private Tuple<string, string> ShortenAndEncode(string value, int position, char pointer)
        {
            int start = Math.Max(position - 20, 0);
            int end = Math.Min(position + 41, value.Length);
            StringBuilder printedValue = new StringBuilder(100);
            StringBuilder printedPointer = new StringBuilder(100);

            if (start > 0)
            {
                printedValue.Append("···");
                printedPointer.Append("   ");
            }

            for (int idx = start; idx < end; ++idx)
            {
                char c = value[idx];
                string encoding;
                int paddingLength = 1;

                if (_encodings.TryGetValue(c, out encoding))
                {
                    printedValue.Append(encoding);
                    paddingLength = encoding.Length;
                }
                else
                {
                    printedValue.Append(c);
                }

                if (idx < position)
                {
                    printedPointer.Append(' ', paddingLength);
                }
                else if (idx == position)
                {
                    printedPointer.AppendFormat("{0} (pos {1})", pointer, position);
                }
            }

            if (end < value.Length)
            {
                printedValue.Append("···");
            }

            return new Tuple<string, string>(printedValue.ToString(), printedPointer.ToString());
        }

        protected override bool ExcludeStackFrame(string stackFrame)
        {
            return base.ExcludeStackFrame(stackFrame)
                || stackFrame.StartsWith("at Microsoft.TestCommon.Assert.", StringComparison.OrdinalIgnoreCase);
        }
    }
}
