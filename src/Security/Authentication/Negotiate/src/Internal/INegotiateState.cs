// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Security.Principal;

namespace Microsoft.AspNetCore.Authentication.Negotiate;

// For testing
internal interface INegotiateState : IDisposable
{
    string? GetOutgoingBlob(string incomingBlob, out BlobErrorType status, out Exception? error);

    bool IsCompleted { get; }

    string Protocol { get; }

    IIdentity GetIdentity();
}

internal enum BlobErrorType
{
    None,
    CredentialError,
    ClientError,
    Other
}
