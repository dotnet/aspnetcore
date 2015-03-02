// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.


using System;
using Microsoft.AspNet.Http;
using Newtonsoft.Json;

namespace Microsoft.AspNet.Authentication.Cookies
{
    internal static class DefaultBehavior
    {
        internal static readonly Action<CookieApplyRedirectContext> ApplyRedirect = context =>
        {
            if (IsAjaxRequest(context.Request))
            {
                string jsonResponse = JsonConvert.SerializeObject(new
                {
                    status = context.Response.StatusCode,
                    headers = new
                    {
                        location = context.RedirectUri
                    }
                }, Formatting.None);

                context.Response.StatusCode = 200;
                context.Response.Headers.Append("X-Responded-JSON", jsonResponse);
            }
            else
            {
                context.Response.Redirect(context.RedirectUri);
            }
        };

        private static bool IsAjaxRequest(HttpRequest request)
        {
            IReadableStringCollection query = request.Query;
            if (query != null)
            {
                if (query["X-Requested-With"] == "XMLHttpRequest")
                {
                    return true;
                }
            }

            IHeaderDictionary headers = request.Headers;
            if (headers != null)
            {
                if (headers["X-Requested-With"] == "XMLHttpRequest")
                {
                    return true;
                }
            }
            return false;
        }
    }
}
