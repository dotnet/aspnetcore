// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net.Http;
using System.Xml.Linq;

namespace Microsoft.AspNetCore.Authentication.Test.OpenIdConnect;

internal class TestTransaction
{
    public HttpRequestMessage Request { get; set; }

    public HttpResponseMessage Response { get; set; }

    public IList<string> SetCookie { get; set; }

    public string ResponseText { get; set; }

    public XElement ResponseElement { get; set; }

    public string AuthenticationCookieValue
    {
        get
        {
            if (SetCookie != null && SetCookie.Count > 0)
            {
                var authCookie = SetCookie.SingleOrDefault(c => c.Contains(".AspNetCore.Cookies="));
                if (authCookie != null)
                {
                    return authCookie.Substring(0, authCookie.IndexOf(';'));
                }
            }

            return null;
        }
    }
}
