// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Http;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;

namespace Microsoft.AspNetCore.Identity.Service
{
    public class FragmentResponseGenerator
    {
        private readonly UrlEncoder _urlEncoder;

        public FragmentResponseGenerator(UrlEncoder urlEncoder)
        {
            _urlEncoder = urlEncoder;
        }

        public void GenerateResponse(
            HttpContext context,
            string redirectUri,
            IEnumerable<KeyValuePair<string, string>> parameters)
        {
            var builder = new StringBuilder();
            builder.Append(redirectUri);
            builder.Append('#');

            var enumerator = parameters.GetEnumerator();
            while (enumerator.MoveNext())
            {
                if (!ShouldSkipKey(enumerator.Current.Key))
                {
                    builder.Append(_urlEncoder.Encode(enumerator.Current.Key));
                    builder.Append('=');
                    builder.Append(_urlEncoder.Encode(enumerator.Current.Value));
                    break;
                }
            }

            while (enumerator.MoveNext())
            {
                if (!ShouldSkipKey(enumerator.Current.Key))
                {
                    builder.Append('&');
                    builder.Append(_urlEncoder.Encode(enumerator.Current.Key));
                    builder.Append('=');
                    builder.Append(_urlEncoder.Encode(enumerator.Current.Value));
                }
            }

            context.Response.Redirect(builder.ToString());
        }

        private bool ShouldSkipKey(string key)
        {
            return string.Equals(key, OpenIdConnectParameterNames.RedirectUri, StringComparison.OrdinalIgnoreCase);
        }
    }
}
