// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;

namespace Microsoft.AspNet.Mvc.HeaderValueAbstractions
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
                MediaTypeWithQualityHeaderValue parsedAcceptHeader;
                // If we are unable to parse any of the Accept Headers, we ignore them completely.
                if (!MediaTypeWithQualityHeaderValue.TryParse(item, out parsedAcceptHeader))
                {
                    return null;
                }

                acceptHeaderCollection.Add(parsedAcceptHeader);
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
                StringWithQualityHeaderValue parsedAcceptCharsetHeader;
                // If we are unable to parse any of the Accept-Charset Headers, we ignore them completely.
                if (!StringWithQualityHeaderValue.TryParse(item, out parsedAcceptCharsetHeader))
                {
                    return null;
                }

                acceptCharsetHeaderCollection.Add(parsedAcceptCharsetHeader);
            }

            return acceptCharsetHeaderCollection;
        }
    }
}
