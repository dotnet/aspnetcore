// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Security.Claims;
using System.Security.Principal;

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
    }
}