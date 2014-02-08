//------------------------------------------------------------------------------
// <copyright file="HttpListenerContext.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//------------------------------------------------------------------------------

using System;
using System.Security.Principal;

namespace Microsoft.AspNet.Security.Windows
{
    // TODO: At what point does a user need to be cleaned up?
    internal sealed class HttpListenerContext 
    {
        private WindowsAuthMiddleware _winAuth;
        private IPrincipal _user = null;

        internal const string NTLM = "NTLM";

        internal HttpListenerContext(WindowsAuthMiddleware httpListener)
        {
            _winAuth = httpListener;
        }
                
        internal void Close()
        {
            if (Logging.On)
            {
                Logging.Enter(Logging.HttpListener, this, "Close()", string.Empty);
            }

            IDisposable user = _user == null ? null : _user.Identity as IDisposable;

            // TODO: At what point does a user need to be cleaned up?

            // For unsafe connection ntlm auth we dont dispose this identity as yet since its cached
            if ((user != null) &&
                (_user.Identity.AuthenticationType != NTLM) && 
                (!_winAuth.UnsafeConnectionNtlmAuthentication)) 
            {
                    user.Dispose();
            }
            if (Logging.On)
            {
                Logging.Exit(Logging.HttpListener, this, "Close", string.Empty);
            }
        }

        internal void Abort()
        {
            if (Logging.On)
            {
                Logging.Enter(Logging.HttpListener, this, "Abort", string.Empty);
            }

            IDisposable user = _user == null ? null : _user.Identity as IDisposable;
            if (user != null)
            {
                user.Dispose();
            }
            if (Logging.On)
            {
                Logging.Exit(Logging.HttpListener, this, "Abort", string.Empty);
            }
        }
    }
}
