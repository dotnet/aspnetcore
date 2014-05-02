// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

using System;

namespace Microsoft.Net.Server
{
    internal static class Utilities
    {
        internal static WebListener CreateHttpServer()
        {
            return CreateServer("http", "localhost", "8080", string.Empty);
        }

        internal static WebListener CreateHttpsServer()
        {
            return CreateServer("https", "localhost", "9090", string.Empty);
        }

        internal static WebListener CreateAuthServer(AuthenticationType authType)
        {
            return CreateServer("http", "localhost", "8080", string.Empty, authType);
        }

        internal static WebListener CreateServer(string scheme, string host, string port, string path)
        {
            return CreateServer(scheme, host, port, path, AuthenticationType.None);
        }

        internal static WebListener CreateServer(string scheme, string host, string port, string path, AuthenticationType authType)
        {
            WebListener listener = new WebListener();
            listener.UrlPrefixes.Add(UrlPrefix.Create(scheme, host, port, path));
            listener.AuthenticationManager.AuthenticationTypes = authType;
            listener.Start();
            return listener;
        }
    }
}
