// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;

namespace Microsoft.AspNetCore.Identity.Service
{
    public class QueryResponseGenerator
    {
        public void GenerateResponse(
            HttpContext context,
            string redirect,
            IEnumerable<KeyValuePair<string,string>> parameters)
        {
            var uri = new Uri(redirect);

            var queryCollection = QueryHelpers.ParseQuery(uri.Query);
            var queryBuilder = new QueryBuilder();
            foreach (var kvp in parameters)
            {
                if (!ShouldSkipKey(kvp.Key))
                {
                    queryBuilder.Add(kvp.Key, kvp.Value);
                }
            }

            var queryString = queryBuilder.ToQueryString().ToUriComponent();
            var redirectUri = $"{uri.GetComponents(UriComponents.SchemeAndServer | UriComponents.Path, UriFormat.Unescaped)}{queryString}";
            context.Response.Redirect(redirectUri);
        }

        private bool ShouldSkipKey(string key)
        {
            return string.Equals(key, OpenIdConnectParameterNames.RedirectUri, StringComparison.OrdinalIgnoreCase);
        }
    }
}
