// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.Extensions.Primitives;
using Microsoft.Net.Http.Headers;
using Xunit.Sdk;

namespace Microsoft.AspNetCore.Mvc
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

            if (!MediaTypeHeaderValue.TryParse(left.Value, out var leftMediaType) ||
                !MediaTypeHeaderValue.TryParse(right.Value, out var rightMediaType) ||
                !leftMediaType.Equals(rightMediaType))
            {
                throw new EqualException(left.ToString(), right.ToString());
            }
        }
    }
}
