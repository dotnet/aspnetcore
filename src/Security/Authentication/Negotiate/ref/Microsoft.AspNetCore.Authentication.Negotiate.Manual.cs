// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.Authentication.Negotiate
{
    internal enum BlobErrorType
    {
        None = 0,
        CredentialError = 1,
        ClientError = 2,
        Other = 3,
    }
    internal partial interface INegotiateState : System.IDisposable
    {
        bool IsCompleted { get; }
        string Protocol { get; }
        System.Security.Principal.IIdentity GetIdentity();
        string GetOutgoingBlob(string incomingBlob, out Microsoft.AspNetCore.Authentication.Negotiate.BlobErrorType status, out System.Exception error);
    }
    internal partial interface INegotiateStateFactory
    {
        Microsoft.AspNetCore.Authentication.Negotiate.INegotiateState CreateInstance();
    }
    public partial class NegotiateOptions : Microsoft.AspNetCore.Authentication.AuthenticationSchemeOptions
    {
        internal bool DeferToServer { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        internal Microsoft.AspNetCore.Authentication.Negotiate.INegotiateStateFactory StateFactory { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
    }
}
