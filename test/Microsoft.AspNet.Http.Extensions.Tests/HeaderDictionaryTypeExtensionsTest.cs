// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNet.PipelineCore.Collections;
using Microsoft.Net.Http.Headers;
using Xunit;

namespace Microsoft.AspNet.Http.Headers
{
    public class HeaderDictionaryTypeExtensionsTest
    {
        [Fact]
        public void GetT_KnownTypeWithValidValue_Success()
        {
            var headers = new HeaderDictionary();
            headers[HeaderNames.ContentType] = "text/plain";

            var result = headers.Get<MediaTypeHeaderValue>(HeaderNames.ContentType);

            var expected = new MediaTypeHeaderValue("text/plain");
            Assert.Equal(expected, result);
        }

        [Fact]
        public void GetT_KnownTypeWithMissingValue_Null()
        {
            var headers = new HeaderDictionary();

            var result = headers.Get<MediaTypeHeaderValue>(HeaderNames.ContentType);

            Assert.Null(result);
        }

        [Fact]
        public void GetT_KnownTypeWithInvalidValue_Null()
        {
            var headers = new HeaderDictionary();
            headers[HeaderNames.ContentType] = "invalid";

            var result = headers.Get<MediaTypeHeaderValue>(HeaderNames.ContentType);

            Assert.Null(result);
        }

        [Fact]
        public void GetT_UnknownTypeWithTryParseAndValidValue_Success()
        {
            var headers = new HeaderDictionary();
            headers["custom"] = "valid";

            var result = headers.Get<TestHeaderValue>("custom");
            Assert.NotNull(result);
        }

        [Fact]
        public void GetT_UnknownTypeWithTryParseAndInvalidValue_Null()
        {
            var headers = new HeaderDictionary();
            headers["custom"] = "invalid";

            var result = headers.Get<TestHeaderValue>("custom");
            Assert.Null(result);
        }

        [Fact]
        public void GetT_UnknownTypeWithTryParseAndMissingValue_Null()
        {
            var headers = new HeaderDictionary();

            var result = headers.Get<TestHeaderValue>("custom");
            Assert.Null(result);
        }

        [Fact]
        public void GetT_UnknownTypeWithoutTryParse_Throws()
        {
            var headers = new HeaderDictionary();
            headers["custom"] = "valid";

            Assert.Throws<NotSupportedException>(() => headers.Get<object>("custom"));
        }

        [Fact]
        public void GetListT_KnownTypeWithValidValue_Success()
        {
            var headers = new HeaderDictionary();
            headers[HeaderNames.Accept] = "text/plain; q=0.9, text/other, */*";

            var result = headers.GetList<MediaTypeHeaderValue>(HeaderNames.Accept);

            var expected = new[] {
                new MediaTypeHeaderValue("text/plain", 0.9),
                new MediaTypeHeaderValue("text/other"),
                new MediaTypeHeaderValue("*/*"),
            }.ToList();
            Assert.Equal(expected, result);
        }

        [Fact]
        public void GetListT_KnownTypeWithMissingValue_Null()
        {
            var headers = new HeaderDictionary();

            var result = headers.GetList<MediaTypeHeaderValue>(HeaderNames.Accept);

            Assert.Null(result);
        }

        [Fact]
        public void GetListT_KnownTypeWithInvalidValue_Null()
        {
            var headers = new HeaderDictionary();
            headers[HeaderNames.Accept] = "invalid";

            var result = headers.GetList<MediaTypeHeaderValue>(HeaderNames.Accept);

            Assert.Null(result);
        }

        [Fact]
        public void GetListT_UnknownTypeWithTryParseListAndValidValue_Success()
        {
            var headers = new HeaderDictionary();
            headers["custom"] = "valid";

            var results = headers.GetList<TestHeaderValue>("custom");
            Assert.NotNull(results);
            Assert.Equal(new[] { new TestHeaderValue() }.ToList(), results);
        }

        [Fact]
        public void GetListT_UnknownTypeWithTryParseListAndInvalidValue_Null()
        {
            var headers = new HeaderDictionary();
            headers["custom"] = "invalid";

            var results = headers.GetList<TestHeaderValue>("custom");
            Assert.Null(results);
        }

        [Fact]
        public void GetListT_UnknownTypeWithTryParseListAndMissingValue_Null()
        {
            var headers = new HeaderDictionary();

            var results = headers.GetList<TestHeaderValue>("custom");
            Assert.Null(results);
        }

        [Fact]
        public void GetListT_UnknownTypeWithoutTryParseList_Throws()
        {
            var headers = new HeaderDictionary();
            headers["custom"] = "valid";

            Assert.Throws<NotSupportedException>(() => headers.GetList<object>("custom"));
        }

        public class TestHeaderValue
        {
            public static bool TryParse(string value, out TestHeaderValue result)
            {
                if (string.Equals("valid", value))
                {
                    result = new TestHeaderValue();
                    return true;
                }
                result = null;
                return false;
            }

            public static bool TryParseList(IList<string> values, out IList<TestHeaderValue> result)
            {
                var results = new List<TestHeaderValue>();
                foreach (var value in values)
                {
                    if (string.Equals("valid", value))
                    {
                        results.Add(new TestHeaderValue());
                    }
                }
                if (results.Count > 0)
                {
                    result = results;
                    return true;
                }
                result = null;
                return false;
            }

            public override bool Equals(object obj)
            {
                var other = obj as TestHeaderValue;
                return other != null;
            }

            public override int GetHashCode()
            {
                return 0;
            }
        }
    }
}