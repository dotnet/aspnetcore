// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Microsoft.AspNetCore.Mvc;

public static class UrlHelperExtensions
{
    public static string GetLocalUrl(this IUrlHelper urlHelper, string localUrl)
    {
        if (!urlHelper.IsLocalUrl(localUrl))
        {
            return urlHelper.Page("/Index");
        }

        return localUrl;
    }

    public static string EmailConfirmationLink(this IUrlHelper urlHelper, string userId, string code, string scheme)
    {
        return urlHelper.Page(
            "/Account/ConfirmEmail",
            pageHandler: null,
            values: new { userId, code },
            protocol: scheme);
    }

    public static string ResetPasswordCallbackLink(this IUrlHelper urlHelper, string userId, string code, string scheme)
    {
        return urlHelper.Page(
            "/Account/ResetPassword",
            pageHandler: null,
            values: new { userId, code },
            protocol: scheme);
    }
}
