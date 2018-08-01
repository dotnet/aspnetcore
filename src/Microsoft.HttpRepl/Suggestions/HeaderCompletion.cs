// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;

namespace Microsoft.HttpRepl.Suggestions
{
    public class HeaderCompletion
    {
        private static readonly IEnumerable<string> CommonHeaders = new[]
       {
            "A-IM",
            "Accept",
            "Accept-Charset",
            "Accept-Encoding",
            "Accept-Language",
            "Accept-Datetime",
            "Access-Control-Request-Method",
            "Access-Control-Request-Headers",
            "Authorization",
            "Cache-Control",
            "Connection",
            "Content-Length",
            "Content-MD5",
            "Content-Type",
            "Cookie",
            "Date",
            "Expect",
            "Forwarded",
            "From",
            "Host",
            "If-Match",
            "If-Modified-Since",
            "If-None-Match",
            "If-Range",
            "If-Unmodified-Since",
            "Max-Forwards",
            "Origin",
            "Pragma",
            "Proxy-Authentication",
            "Range",
            "Referer",
            "TE",
            "User-Agent",
            "Upgrade",
            "Via",
            "Warning",
            //Non-standard
            "Upgrade-Insecure-Requests",
            "X-Requested-With",
            "DNT",
            "X-Forwarded-For",
            "X-Forwarded-Host",
            "X-Forwarded-Proto",
            "Front-End-Https",
            "X-Http-Method-Override",
            "X-ATT-DeviceId",
            "X-Wap-Profile",
            "Proxy-Connection",
            "X-UIDH",
            "X-Csrf-Token",
            "X-Request-ID",
            "X-Correlation-ID"
        };

        private static readonly IReadOnlyList<string> DefaultContentTypesList = null;

        public static IEnumerable<string> GetCompletions(IReadOnlyCollection<string> existingHeaders, string prefix)
        {
            List<string> result = CommonHeaders.Where(x => x.StartsWith(prefix, StringComparison.OrdinalIgnoreCase) && !(existingHeaders?.Contains(x) ?? false)).ToList();

            if (result.Count > 0)
            {
                return result;
            }

            return null;
        }

        public static IEnumerable<string> GetValueCompletions(string method, string path, string header, string prefix, HttpState programState)
        {
            switch (header.ToUpperInvariant())
            {
                case "CONTENT-TYPE":
                    IEnumerable<string> results = programState.GetApplicableContentTypes(method, path) ?? DefaultContentTypesList;

                    if (results is null)
                    {
                        return null;
                    }

                    return results.Where(x => !string.IsNullOrEmpty(x) && x.StartsWith(prefix, StringComparison.OrdinalIgnoreCase));
                default:
                    return null;
            }
        }
    }
}
