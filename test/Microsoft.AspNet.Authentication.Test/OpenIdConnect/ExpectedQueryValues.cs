// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using Microsoft.Extensions.WebEncoders;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Xunit;

namespace Microsoft.AspNet.Authentication.Tests.OpenIdConnect
{
    /// <summary>
    /// This helper class is used to check that query string parameters are as expected.
    /// </summary>
    public class ExpectedQueryValues
    {
        public ExpectedQueryValues(string authority, OpenIdConnectConfiguration configuration = null)
        {
            Authority = authority;
            Configuration = configuration ?? TestUtilities.DefaultOpenIdConnectConfiguration;
        }

        public static ExpectedQueryValues Defaults(string authority)
        {
            var result = new ExpectedQueryValues(authority);
            result.Scope = OpenIdConnectScopes.OpenIdProfile;
            result.ResponseType = OpenIdConnectResponseTypes.CodeIdToken;
            return result;
        }

        public void CheckValues(string query, IEnumerable<string> parameters)
        {
            var errors = new List<string>();
            if (!query.StartsWith(ExpectedAuthority))
            {
                errors.Add("ExpectedAuthority: " + ExpectedAuthority);
            }

            foreach(var str in parameters)
            {
                if (str == OpenIdConnectParameterNames.ClientId)
                {
                    if (!query.Contains(ExpectedClientId))
                        errors.Add("ExpectedClientId: " + ExpectedClientId);

                    continue;
                }

                if (str == OpenIdConnectParameterNames.RedirectUri)
                {
                     if(!query.Contains(ExpectedRedirectUri))
                        errors.Add("ExpectedRedirectUri: " + ExpectedRedirectUri);

                    continue;
                }

                if (str == OpenIdConnectParameterNames.Resource)
                {
                    if(!query.Contains(ExpectedResource))
                        errors.Add("ExpectedResource: " + ExpectedResource);

                    continue;
                }

                if (str == OpenIdConnectParameterNames.ResponseMode)
                {
                    if(!query.Contains(ExpectedResponseMode))
                        errors.Add("ExpectedResponseMode: " + ExpectedResponseMode);

                    continue;
                }

                if (str == OpenIdConnectParameterNames.Scope)
                {
                    if (!query.Contains(ExpectedScope))
                        errors.Add("ExpectedScope: " + ExpectedScope);

                    continue;
                }

                if (str == OpenIdConnectParameterNames.State)
                {
                    if (!query.Contains(ExpectedState))
                        errors.Add("ExpectedState: " + ExpectedState);

                    continue;
                }
            }

            if (errors.Count > 0)
            {
                var sb = new StringBuilder();
                sb.AppendLine("query string not as expected: " + Environment.NewLine + query + Environment.NewLine);
                foreach (var str in errors)
                {
                    sb.AppendLine(str);
                }

                Debug.WriteLine(sb.ToString());
                Assert.True(false, sb.ToString());
            }
        }

        public UrlEncoder Encoder { get; set; } = UrlEncoder.Default;

        public string Authority { get; set; }

        public string ClientId { get; set; } = Guid.NewGuid().ToString();

        public string RedirectUri { get; set; } = Guid.NewGuid().ToString();

        public OpenIdConnectRequestType RequestType { get; set; } = OpenIdConnectRequestType.AuthenticationRequest;

        public string Resource { get; set; } = Guid.NewGuid().ToString();

        public string ResponseMode { get; set; } = OpenIdConnectResponseModes.FormPost;

        public string ResponseType { get; set; } = Guid.NewGuid().ToString();

        public string Scope { get; set; } = Guid.NewGuid().ToString();

        public string State { get; set; } = Guid.NewGuid().ToString();

        public string ExpectedAuthority
        {
            get
            {
                if (RequestType == OpenIdConnectRequestType.TokenRequest)
                {
                    return Configuration?.EndSessionEndpoint ?? Authority + @"/oauth2/token";
                }
                else if (RequestType == OpenIdConnectRequestType.LogoutRequest)
                {
                    return Configuration?.TokenEndpoint ?? Authority + @"/oauth2/logout";
                }

                return Configuration?.AuthorizationEndpoint ?? Authority + (@"/oauth2/authorize");
            }
        }

        public OpenIdConnectConfiguration Configuration { get; set; }

        public string ExpectedClientId
        {
            get { return OpenIdConnectParameterNames.ClientId + "=" + Encoder.UrlEncode(ClientId); }
        }

        public string ExpectedRedirectUri
        {
            get { return OpenIdConnectParameterNames.RedirectUri + "=" + Encoder.UrlEncode(RedirectUri); }
        }

        public string ExpectedResource
        {
            get { return OpenIdConnectParameterNames.Resource + "=" + Encoder.UrlEncode(Resource); }
        }

        public string ExpectedResponseMode
        {
            get { return OpenIdConnectParameterNames.ResponseMode + "=" + Encoder.UrlEncode(ResponseMode); }
        }

        public string ExpectedScope
        {
            get { return OpenIdConnectParameterNames.Scope + "=" + Encoder.UrlEncode(Scope); }
        }

        public string ExpectedState
        {
            get { return Encoder.UrlEncode(State); }
        }
    }
}
