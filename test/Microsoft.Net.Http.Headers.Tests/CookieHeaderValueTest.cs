// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace Microsoft.Net.Http.Headers
{
    public class CookieHeaderValueTest
    {
        public static TheoryData<CookieHeaderValue, string> CookieHeaderDataSet
        {
            get
            {
                var dataset = new TheoryData<CookieHeaderValue, string>();
                var header1 = new CookieHeaderValue("name1", "n1=v1&n2=v2&n3=v3");
                dataset.Add(header1, "name1=n1=v1&n2=v2&n3=v3");

                var header2 = new CookieHeaderValue("name2", "");
                dataset.Add(header2, "name2=");

                var header3 = new CookieHeaderValue("name3", "value3");
                dataset.Add(header3, "name3=value3");

                var header4 = new CookieHeaderValue("name4", "\"value4\"");
                dataset.Add(header4, "name4=\"value4\"");

                return dataset;
            }
        }

        public static TheoryData<string> InvalidCookieHeaderDataSet
        {
            get
            {
                return new TheoryData<string>
                {
                    "=value",
                    "name=value;",
                    "name=value,",
                };
            }
        }

        public static TheoryData<string> InvalidCookieNames
        {
            get
            {
                return new TheoryData<string>
                {
                    "<acb>",
                    "{acb}",
                    "[acb]",
                    "\"acb\"",
                    "a,b",
                    "a;b",
                    "a\\b",
                    "a b",
                };
            }
        }

        public static TheoryData<string> InvalidCookieValues
        {
            get
            {
                return new TheoryData<string>
                {
                    { "\"" },
                    { "a,b" },
                    { "a;b" },
                    { "a\\b" },
                    { "\"abc" },
                    { "a\"bc" },
                    { "abc\"" },
                    { "a b" },
                };
            }
        }
        public static TheoryData<IList<CookieHeaderValue>, string[]> ListOfCookieHeaderDataSet
        {
            get
            {
                var dataset = new TheoryData<IList<CookieHeaderValue>, string[]>();
                var header1 = new CookieHeaderValue("name1", "n1=v1&n2=v2&n3=v3");
                var string1 = "name1=n1=v1&n2=v2&n3=v3";

                var header2 = new CookieHeaderValue("name2", "value2");
                var string2 = "name2=value2";

                var header3 = new CookieHeaderValue("name3", "value3");
                var string3 = "name3=value3";

                var header4 = new CookieHeaderValue("name4", "\"value4\"");
                var string4 = "name4=\"value4\"";

                dataset.Add(new[] { header1 }.ToList(), new[] { string1 });
                dataset.Add(new[] { header1, header1 }.ToList(), new[] { string1, string1 });
                dataset.Add(new[] { header1, header1 }.ToList(), new[] { string1, null, "", " ", ";", " , ", string1 });
                dataset.Add(new[] { header2 }.ToList(), new[] { string2 });
                dataset.Add(new[] { header1, header2 }.ToList(), new[] { string1, string2 });
                dataset.Add(new[] { header1, header2 }.ToList(), new[] { string1 + ", " + string2 });
                dataset.Add(new[] { header2, header1 }.ToList(), new[] { string2 + "; " + string1 });
                dataset.Add(new[] { header1, header2, header3, header4 }.ToList(), new[] { string1, string2, string3, string4 });
                dataset.Add(new[] { header1, header2, header3, header4 }.ToList(), new[] { string.Join(",", string1, string2, string3, string4) });
                dataset.Add(new[] { header1, header2, header3, header4 }.ToList(), new[] { string.Join(";", string1, string2, string3, string4) });

                return dataset;
            }
        }

        // TODO: [Fact]
        public void CookieHeaderValue_CtorThrowsOnNullName()
        {
            Assert.Throws<ArgumentNullException>(() => new CookieHeaderValue(null, "value"));
        }

        [Theory]
        [MemberData(nameof(InvalidCookieNames))]
        public void CookieHeaderValue_CtorThrowsOnInvalidName(string name)
        {
            Assert.Throws<ArgumentException>(() => new CookieHeaderValue(name, "value"));
        }

        [Theory]
        [MemberData(nameof(InvalidCookieValues))]
        public void CookieHeaderValue_CtorThrowsOnInvalidValue(string value)
        {
            Assert.Throws<ArgumentException>(() => new CookieHeaderValue("name", value));
        }

        [Fact]
        public void CookieHeaderValue_Ctor1_InitializesCorrectly()
        {
            var header = new CookieHeaderValue("cookie");
            Assert.Equal("cookie", header.Name);
            Assert.Equal(string.Empty, header.Value);
        }

        [Theory]
        [InlineData("name", "")]
        [InlineData("name", "value")]
        [InlineData("name", "\"acb\"")]
        public void CookieHeaderValue_Ctor2InitializesCorrectly(string name, string value)
        {
            var header = new CookieHeaderValue(name, value);
            Assert.Equal(name, header.Name);
            Assert.Equal(value, header.Value);
        }

        [Fact]
        public void CookieHeaderValue_Value()
        {
            var cookie = new CookieHeaderValue("name");
            Assert.Equal(string.Empty, cookie.Value);

            cookie.Value = "value1";
            Assert.Equal("value1", cookie.Value);
        }

        [Theory]
        [MemberData(nameof(CookieHeaderDataSet))]
        public void CookieHeaderValue_ToString(CookieHeaderValue input, string expectedValue)
        {
            Assert.Equal(expectedValue, input.ToString());
        }

        [Theory]
        [MemberData(nameof(CookieHeaderDataSet))]
        public void CookieHeaderValue_Parse_AcceptsValidValues(CookieHeaderValue cookie, string expectedValue)
        {
            var header = CookieHeaderValue.Parse(expectedValue);

            Assert.Equal(cookie, header);
            Assert.Equal(expectedValue, header.ToString());
        }

        [Theory]
        [MemberData(nameof(CookieHeaderDataSet))]
        public void CookieHeaderValue_TryParse_AcceptsValidValues(CookieHeaderValue cookie, string expectedValue)
        {
            CookieHeaderValue header;
            bool result = CookieHeaderValue.TryParse(expectedValue, out header);
            Assert.True(result);

            Assert.Equal(cookie, header);
            Assert.Equal(expectedValue, header.ToString());
        }

        [Theory]
        [MemberData(nameof(InvalidCookieHeaderDataSet))]
        public void CookieHeaderValue_Parse_RejectsInvalidValues(string value)
        {
            Assert.Throws<FormatException>(() => CookieHeaderValue.Parse(value));
        }

        [Theory]
        [MemberData(nameof(InvalidCookieHeaderDataSet))]
        public void CookieHeaderValue_TryParse_RejectsInvalidValues(string value)
        {
            CookieHeaderValue header;
            bool result = CookieHeaderValue.TryParse(value, out header);

            Assert.False(result);
        }

        [Theory]
        [MemberData(nameof(ListOfCookieHeaderDataSet))]
        public void CookieHeaderValue_ParseList_AcceptsValidValues(IList<CookieHeaderValue> cookies, string[] input)
        {
            var results = CookieHeaderValue.ParseList(input);

            Assert.Equal(cookies, results);
        }

        [Theory]
        [MemberData(nameof(ListOfCookieHeaderDataSet))]
        public void CookieHeaderValue_TryParseList_AcceptsValidValues(IList<CookieHeaderValue> cookies, string[] input)
        {
            IList<CookieHeaderValue> results;
            bool result = CookieHeaderValue.TryParseList(input, out results);
            Assert.True(result);

            Assert.Equal(cookies, results);
        }
    }
}
