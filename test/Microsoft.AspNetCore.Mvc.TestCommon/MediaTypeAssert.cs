using System;
using Microsoft.Extensions.Primitives;
using Microsoft.Net.Http.Headers;
using Xunit.Sdk;

namespace Microsoft.AspNetCore.Mvc.TestCommon
{
    public class MediaTypeAssert
    {
        public static void Equal(string left, string right)
        {
            Equal(new StringSegment(left), new StringSegment(right));
        }

        public static void Equal(string left, StringSegment right)
        {
            Equal(new StringSegment(left), right);
        }

        public static void Equal(StringSegment left, string right)
        {
            Equal(left, new StringSegment(right));
        }

        public static void Equal(StringSegment left, StringSegment right)
        {
            if (!left.HasValue && !right.HasValue)
            {
                return;
            }
            else if (!left.HasValue || !right.HasValue)
            {
                throw new EqualException(left.ToString(), right.ToString());
            }

            MediaTypeHeaderValue leftMediaType = null;
            MediaTypeHeaderValue rightMediaType = null;

            if (!MediaTypeHeaderValue.TryParse(left.Value, out leftMediaType) ||
                !MediaTypeHeaderValue.TryParse(right.Value, out rightMediaType) ||
                !leftMediaType.Equals(rightMediaType))
            {
                throw new EqualException(left.ToString(), right.ToString());
            }
        }
    }
}
