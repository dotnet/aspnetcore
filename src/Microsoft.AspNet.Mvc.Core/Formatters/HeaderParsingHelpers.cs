// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using Microsoft.AspNet.Mvc.HeaderValueAbstractions;

namespace Microsoft.AspNet.Mvc
{
    public static class HeaderParsingHelpers
    {
        public static IList<MediaTypeWithQualityHeaderValue> GetAcceptHeaders(string acceptHeader)
        {
            if (string.IsNullOrEmpty(acceptHeader))
            {
                return null;
            }

            var acceptHeaderCollection = new List<MediaTypeWithQualityHeaderValue>();
            foreach (var item in acceptHeader.Split(','))
            {
                acceptHeaderCollection.Add(MediaTypeWithQualityHeaderValue.Parse(item));
            }

            return acceptHeaderCollection;
        }

        public static IList<StringWithQualityHeaderValue> GetAcceptCharsetHeaders(string acceptCharsetHeader)
        {
            if (string.IsNullOrEmpty(acceptCharsetHeader))
            {
                return null;
            }

            var acceptCharsetHeaderCollection = new List<StringWithQualityHeaderValue>();
            foreach (var item in acceptCharsetHeader.Split(','))
            {
                acceptCharsetHeaderCollection.Add(StringWithQualityHeaderValue.Parse(item));
            }

            return acceptCharsetHeaderCollection;
        }
    }
}
