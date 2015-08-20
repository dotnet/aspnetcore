// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Security.Principal;
using Microsoft.AspNet.Http;
using Microsoft.Framework.Primitives;

namespace Microsoft.AspNet.Owin
{
    internal static class Utilities
    {
        internal static string RemoveQuestionMark(string queryString)
        {
            if (!string.IsNullOrEmpty(queryString))
            {
                if (queryString[0] == '?')
                {
                    return queryString.Substring(1);
                }
            }
            return queryString;
        }

        internal static string AddQuestionMark(string queryString)
        {
            if (!string.IsNullOrEmpty(queryString))
            {
                return '?' + queryString;
            }
            return queryString;
        }

        internal static ClaimsPrincipal MakeClaimsPrincipal(IPrincipal principal)
        {
            if (principal == null)
            {
                return null;
            }
            if (principal is ClaimsPrincipal)
            {
                return principal as ClaimsPrincipal;
            }
            return new ClaimsPrincipal(principal);
        }

        internal static IDictionary<string, StringValues> MakeDictionaryStringValues(IDictionary<string, string[]> dictionary)
        {
            var wrapper = dictionary as DictionaryStringArrayWrapper;
            if (wrapper != null)
            {
                return wrapper.Inner;
            }
            return new DictionaryStringValuesWrapper(dictionary);
        }

        internal static IDictionary<string, string[]> MakeDictionaryStringArray(IDictionary<string, StringValues> dictionary)
        {
            var wrapper = dictionary as DictionaryStringValuesWrapper;
            if (wrapper != null)
            {
                return wrapper.Inner;
            }
            return new DictionaryStringArrayWrapper(dictionary);
        }
    }
}
