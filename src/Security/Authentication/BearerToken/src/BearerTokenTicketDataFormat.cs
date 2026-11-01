// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers.Text;
using Microsoft.AspNetCore.DataProtection;

namespace Microsoft.AspNetCore.Authentication.BearerToken;

// The default BearerTokenOptions.BearerTokenProtector. It lets BearerTokenHandler unprotect the token straight from the
// Authorization header value without first allocating a substring for the token.
internal sealed class BearerTokenTicketDataFormat : TicketDataFormat
{
    private readonly IDataProtector _protector;

    public BearerTokenTicketDataFormat(IDataProtector protector)
        : base(protector)
    {
        _protector = protector;
    }

    public AuthenticationTicket? UnprotectSpan(ReadOnlySpan<char> protectedText)
    {
        // Base64UrlTextEncoder.Decode also accepts standard Base64 input. Leave anything that could be
        // standard Base64 to the base implementation so the accepted inputs stay exactly the same.
        if (protectedText.ContainsAny('+', '/'))
        {
            return Unprotect(protectedText.ToString());
        }

        try
        {
            var userData = _protector.Unprotect(Base64Url.DecodeFromChars(protectedText));
            return userData is null ? null : TicketSerializer.Default.Deserialize(userData);
        }
        catch
        {
            // Match SecureDataFormat.Unprotect which treats any failure as an invalid token.
            return null;
        }
    }
}
