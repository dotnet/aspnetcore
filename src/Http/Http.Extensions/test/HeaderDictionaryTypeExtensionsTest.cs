// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Net.Http.Headers;

namespace Microsoft.AspNetCore.Http.Headers;

public class HeaderDictionaryTypeExtensionsTest
{
    [Fact]
    public void GetT_KnownTypeWithValidValue_Success()
    {
        var context = new DefaultHttpContext();
        context.Request.Headers.ContentType = "text/plain";

        var result = context.Request.GetTypedHeaders().Get<MediaTypeHeaderValue>(HeaderNames.ContentType);

        var expected = new MediaTypeHeaderValue("text/plain");
        Assert.Equal(expected, result);
    }

    [Fact]
    public void GetT_KnownTypeWithMissingValue_Null()
    {
        var context = new DefaultHttpContext();

        var result = context.Request.GetTypedHeaders().Get<MediaTypeHeaderValue>(HeaderNames.ContentType);

        Assert.Null(result);
    }

    [Fact]
    public void GetT_KnownTypeWithInvalidValue_Null()
    {
        var context = new DefaultHttpContext();
        context.Request.Headers.ContentType = "invalid";

        var result = context.Request.GetTypedHeaders().Get<MediaTypeHeaderValue>(HeaderNames.ContentType);

        Assert.Null(result);
    }

    [Fact]
    public void GetT_UnknownTypeWithTryParseAndValidValue_Success()
    {
        var context = new DefaultHttpContext();
        context.Request.Headers["custom"] = "valid";

        var result = context.Request.GetTypedHeaders().Get<TestHeaderValue>("custom");
        Assert.NotNull(result);
    }

    [Fact]
    public void GetT_UnknownTypeWithTryParseAndInvalidValue_Null()
    {
        var context = new DefaultHttpContext();
        context.Request.Headers["custom"] = "invalid";

        var result = context.Request.GetTypedHeaders().Get<TestHeaderValue>("custom");
        Assert.Null(result);
    }

    [Fact]
    public void GetT_UnknownTypeWithTryParseAndMissingValue_Null()
    {
        var context = new DefaultHttpContext();

        var result = context.Request.GetTypedHeaders().Get<TestHeaderValue>("custom");
        Assert.Null(result);
    }

    [Fact]
    public void GetT_UnknownTypeWithoutTryParse_Throws()
    {
        var context = new DefaultHttpContext();
        context.Request.Headers["custom"] = "valid";

        Assert.Throws<NotSupportedException>(() => context.Request.GetTypedHeaders().Get<object>("custom"));
    }

    [Fact]
    public void GetListT_KnownTypeWithValidValue_Success()
    {
        var context = new DefaultHttpContext();
        context.Request.Headers.Accept = "text/plain; q=0.9, text/other, */*";

        var result = context.Request.GetTypedHeaders().GetList<MediaTypeHeaderValue>(HeaderNames.Accept);

        var expected = new[] {
                new MediaTypeHeaderValue("text/plain", 0.9),
                new MediaTypeHeaderValue("text/other"),
                new MediaTypeHeaderValue("*/*"),
            }.ToList();
        Assert.Equal(expected, result);
    }

    [Fact]
    public void GetListT_KnownTypeWithMissingValue_EmptyList()
    {
        var context = new DefaultHttpContext();

        var result = context.Request.GetTypedHeaders().GetList<MediaTypeHeaderValue>(HeaderNames.Accept);

        Assert.Empty(result);
    }

    [Fact]
    public void GetListT_KnownTypeWithInvalidValue_EmptyList()
    {
        var context = new DefaultHttpContext();
        context.Request.Headers.Accept = "invalid";

        var result = context.Request.GetTypedHeaders().GetList<MediaTypeHeaderValue>(HeaderNames.Accept);

        Assert.Empty(result);
    }

    [Fact]
    public void GetListT_UnknownTypeWithTryParseListAndValidValue_Success()
    {
        var context = new DefaultHttpContext();
        context.Request.Headers["custom"] = "valid";

        var results = context.Request.GetTypedHeaders().GetList<TestHeaderValue>("custom");
        Assert.NotNull(results);
        Assert.Equal(new[] { new TestHeaderValue() }.ToList(), results);
    }

    [Fact]
    public void GetListT_UnknownTypeWithTryParseListAndInvalidValue_EmptyList()
    {
        var context = new DefaultHttpContext();
        context.Request.Headers["custom"] = "invalid";

        var results = context.Request.GetTypedHeaders().GetList<TestHeaderValue>("custom");
        Assert.Empty(results);
    }

    [Fact]
    public void GetListT_UnknownTypeWithTryParseListAndMissingValue_EmptyList()
    {
        var context = new DefaultHttpContext();

        var results = context.Request.GetTypedHeaders().GetList<TestHeaderValue>("custom");
        Assert.Empty(results);
    }

    [Fact]
    public void GetListT_UnknownTypeWithoutTryParseList_Throws()
    {
        var context = new DefaultHttpContext();
        context.Request.Headers["custom"] = "valid";

        Assert.Throws<NotSupportedException>(() => context.Request.GetTypedHeaders().GetList<object>("custom"));
    }

    public class TestHeaderValue
    {
        public static bool TryParse(string value, out TestHeaderValue result)
        {
            if (string.Equals("valid", value, StringComparison.Ordinal))
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
                if (string.Equals("valid", value, StringComparison.Ordinal))
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
