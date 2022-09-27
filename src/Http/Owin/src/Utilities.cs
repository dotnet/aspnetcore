// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Security.Claims;
using System.Security.Principal;
using Microsoft.AspNetCore.Http;

namespace Microsoft.AspNetCore.Owin;

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

    internal static IHeaderDictionary MakeHeaderDictionary(IDictionary<string, string[]> dictionary)
    {
        var wrapper = dictionary as DictionaryStringArrayWrapper;
        if (wrapper != null)
        {
            return wrapper.Inner;
        }
        return new DictionaryStringValuesWrapper(dictionary);
    }

    internal static IDictionary<string, string[]> MakeDictionaryStringArray(IHeaderDictionary dictionary)
    {
        var wrapper = dictionary as DictionaryStringValuesWrapper;
        if (wrapper != null)
        {
            return wrapper.Inner;
        }
        return new DictionaryStringArrayWrapper(dictionary);
    }
}
