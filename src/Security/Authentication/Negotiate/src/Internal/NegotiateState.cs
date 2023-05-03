// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net.Security;
using System.Security.Claims;
using System.Security.Principal;

namespace Microsoft.AspNetCore.Authentication.Negotiate;

internal sealed class NegotiateState : INegotiateState
{
    private static readonly NegotiateAuthenticationServerOptions _serverOptions = new();
    private readonly NegotiateAuthentication _instance;

    public NegotiateState()
    {
        _instance = new NegotiateAuthentication(_serverOptions);
    }

    public string? GetOutgoingBlob(string incomingBlob, out BlobErrorType status, out Exception? error)
    {
        var outgoingBlob = _instance.GetOutgoingBlob(incomingBlob, out var authStatus);

        if (authStatus == NegotiateAuthenticationStatusCode.Completed ||
            authStatus == NegotiateAuthenticationStatusCode.ContinueNeeded)
        {
            status = BlobErrorType.None;
            error = null;
        }
        else
        {
            error = new AuthenticationFailureException(authStatus.ToString());
            if (IsCredentialError(authStatus))
            {
                status = BlobErrorType.CredentialError;
            }
            else if (IsClientError(authStatus))
            {
                status = BlobErrorType.ClientError;
            }
            else
            {
                status = BlobErrorType.Other;
            }
        }

        return outgoingBlob;
    }

    public bool IsCompleted
    {
        get => _instance.IsAuthenticated;
    }

    public string Protocol
    {
        get => _instance.Package;
    }

    public IIdentity GetIdentity()
    {
        var remoteIdentity = _instance.RemoteIdentity;
        return remoteIdentity is ClaimsIdentity claimsIdentity ? claimsIdentity.Clone() : remoteIdentity;
    }

    public void Dispose()
    {
        _instance.Dispose();
    }

    private static bool IsCredentialError(NegotiateAuthenticationStatusCode error)
    {
        return error == NegotiateAuthenticationStatusCode.UnknownCredentials ||
            error == NegotiateAuthenticationStatusCode.CredentialsExpired ||
            error == NegotiateAuthenticationStatusCode.BadBinding;
    }

    private static bool IsClientError(NegotiateAuthenticationStatusCode error)
    {
        return error == NegotiateAuthenticationStatusCode.InvalidToken ||
            error == NegotiateAuthenticationStatusCode.QopNotSupported ||
            error == NegotiateAuthenticationStatusCode.UnknownCredentials ||
            error == NegotiateAuthenticationStatusCode.MessageAltered ||
            error == NegotiateAuthenticationStatusCode.OutOfSequence ||
            error == NegotiateAuthenticationStatusCode.InvalidCredentials;
    }
}
