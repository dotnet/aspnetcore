// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Security.Claims;
using System.Text;

namespace Microsoft.AspNetCore.Authentication;

/// <summary>
/// Maps UTF-8 encoded <see cref="ClaimTypes"/> and <see cref="ClaimValueTypes"/> constants to the
/// existing string constants so deserializing them does not allocate a new string.
/// </summary>
internal static class WellKnownClaimStrings
{
    private static readonly string[] Values =
    [
        ClaimTypes.AuthenticationInstant,
        ClaimTypes.AuthenticationMethod,
        ClaimTypes.CookiePath,
        ClaimTypes.DenyOnlyPrimarySid,
        ClaimTypes.DenyOnlyPrimaryGroupSid,
        ClaimTypes.DenyOnlyWindowsDeviceGroup,
        ClaimTypes.Dsa,
        ClaimTypes.Expiration,
        ClaimTypes.Expired,
        ClaimTypes.GroupSid,
        ClaimTypes.IsPersistent,
        ClaimTypes.PrimaryGroupSid,
        ClaimTypes.PrimarySid,
        ClaimTypes.Role,
        ClaimTypes.SerialNumber,
        ClaimTypes.UserData,
        ClaimTypes.Version,
        ClaimTypes.WindowsAccountName,
        ClaimTypes.WindowsDeviceClaim,
        ClaimTypes.WindowsDeviceGroup,
        ClaimTypes.WindowsUserClaim,
        ClaimTypes.WindowsFqbnVersion,
        ClaimTypes.WindowsSubAuthority,
        ClaimTypes.Anonymous,
        ClaimTypes.Authentication,
        ClaimTypes.AuthorizationDecision,
        ClaimTypes.Country,
        ClaimTypes.DateOfBirth,
        ClaimTypes.Dns,
        ClaimTypes.DenyOnlySid,
        ClaimTypes.Email,
        ClaimTypes.Gender,
        ClaimTypes.GivenName,
        ClaimTypes.Hash,
        ClaimTypes.HomePhone,
        ClaimTypes.Locality,
        ClaimTypes.MobilePhone,
        ClaimTypes.Name,
        ClaimTypes.NameIdentifier,
        ClaimTypes.OtherPhone,
        ClaimTypes.PostalCode,
        ClaimTypes.Rsa,
        ClaimTypes.Sid,
        ClaimTypes.Spn,
        ClaimTypes.StateOrProvince,
        ClaimTypes.StreetAddress,
        ClaimTypes.Surname,
        ClaimTypes.System,
        ClaimTypes.Thumbprint,
        ClaimTypes.Upn,
        ClaimTypes.Uri,
        ClaimTypes.Webpage,
        ClaimTypes.X500DistinguishedName,
        ClaimTypes.Actor,
        ClaimValueTypes.Base64Binary,
        ClaimValueTypes.Base64Octet,
        ClaimValueTypes.Boolean,
        ClaimValueTypes.Date,
        ClaimValueTypes.DateTime,
        ClaimValueTypes.Double,
        ClaimValueTypes.Fqbn,
        ClaimValueTypes.HexBinary,
        ClaimValueTypes.Integer,
        ClaimValueTypes.Integer32,
        ClaimValueTypes.Integer64,
        ClaimValueTypes.Sid,
        ClaimValueTypes.String,
        ClaimValueTypes.Time,
        ClaimValueTypes.UInteger32,
        ClaimValueTypes.UInteger64,
        ClaimValueTypes.DnsName,
        ClaimValueTypes.Email,
        ClaimValueTypes.Rsa,
        ClaimValueTypes.UpnName,
        ClaimValueTypes.DsaKeyValue,
        ClaimValueTypes.KeyInfo,
        ClaimValueTypes.RsaKeyValue,
        ClaimValueTypes.DaytimeDuration,
        ClaimValueTypes.YearMonthDuration,
        ClaimValueTypes.Rfc822Name,
        ClaimValueTypes.X500Name,
    ];

    // Candidates indexed by their length. All values are ASCII, so the UTF-8 byte count equals the length.
    private static readonly string[]?[] ValuesByLength = CreateValuesByLength();

    public static string? Find(ReadOnlySpan<byte> utf8)
    {
        var valuesByLength = ValuesByLength;
        if ((uint)utf8.Length >= (uint)valuesByLength.Length || valuesByLength[utf8.Length] is not { } candidates)
        {
            return null;
        }

        foreach (var candidate in candidates)
        {
            if (Ascii.Equals(utf8, candidate))
            {
                return candidate;
            }
        }

        return null;
    }

    private static string[]?[] CreateValuesByLength()
    {
        var maxLength = 0;
        foreach (var value in Values)
        {
            maxLength = Math.Max(maxLength, value.Length);
        }

        var valuesByLength = new string[]?[maxLength + 1];
        foreach (var value in Values)
        {
            var candidates = valuesByLength[value.Length] ?? [];
            if (Array.IndexOf(candidates, value) < 0)
            {
                Array.Resize(ref candidates, candidates.Length + 1);
                candidates[^1] = value;
            }
            valuesByLength[value.Length] = candidates;
        }

        return valuesByLength;
    }
}
