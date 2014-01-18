// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

using System;
using System.Net.Http.Headers;

namespace Microsoft.TestCommon
{
    public class MediaTypeAssert
    {
        private static readonly MediaTypeAssert singleton = new MediaTypeAssert();

        public static MediaTypeAssert Singleton { get { return singleton; } }

        public void AreEqual(MediaTypeHeaderValue expected, MediaTypeHeaderValue actual, string errorMessage)
        {
            if (expected != null || actual != null)
            {
                Assert.NotNull(expected);
                Assert.Equal(0, new MediaTypeHeaderValueComparer().Compare(expected, actual));
            }
        }

        public void AreEqual(MediaTypeHeaderValue expected, string actual, string errorMessage)
        {
            if (expected != null || !String.IsNullOrEmpty(actual))
            {
                MediaTypeHeaderValue actualMediaType = new MediaTypeHeaderValue(actual);
                Assert.NotNull(expected);
                Assert.Equal(0, new MediaTypeHeaderValueComparer().Compare(expected, actualMediaType));
            }
        }

        public void AreEqual(string expected, string actual, string errorMessage)
        {
            if (!String.IsNullOrEmpty(expected) || !String.IsNullOrEmpty(actual))
            {
                Assert.NotNull(expected);
                MediaTypeHeaderValue expectedMediaType = new MediaTypeHeaderValue(expected);
                MediaTypeHeaderValue actualMediaType = new MediaTypeHeaderValue(actual);
                Assert.Equal(0, new MediaTypeHeaderValueComparer().Compare(expectedMediaType, actualMediaType));
            }
        }

        public void AreEqual(string expected, MediaTypeHeaderValue actual, string errorMessage)
        {
            if (!String.IsNullOrEmpty(expected) || actual != null)
            {
                Assert.NotNull(expected);
                MediaTypeHeaderValue expectedMediaType = new MediaTypeHeaderValue(expected);
                Assert.Equal(0, new MediaTypeHeaderValueComparer().Compare(expectedMediaType, actual));
            }
        }
    }
}
