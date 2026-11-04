// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Session;

internal static class CookieProtection
{
    internal static string Protect(IDataProtector protector, string data)
    {
        ArgumentNullException.ThrowIfNull(protector);
        if (string.IsNullOrEmpty(data))
        {
            return data;
        }

        var userData = Encoding.UTF8.GetBytes(data);

        var protectedData = protector.Protect(userData);
        return Convert.ToBase64String(protectedData).TrimEnd('=');
    }

    internal static string Unprotect(IDataProtector protector, string? protectedText, ILogger logger)
    {
        try
        {
            if (string.IsNullOrEmpty(protectedText))
            {
                return string.Empty;
            }

            var protectedData = Convert.FromBase64String(Pad(protectedText));
            if (protectedData == null)
            {
                return string.Empty;
            }

            var userData = protector.Unprotect(protectedData);
            if (userData == null)
            {
                return string.Empty;
            }

            return Encoding.UTF8.GetString(userData);
        }
        catch (Exception ex)
        {
            // Log the exception, but do not leak other information
            logger.ErrorUnprotectingSessionCookie(ex);
            return string.Empty;
        }
    }

    private static string Pad(string text)
    {
        var padding = 3 - ((text.Length + 3) % 4);
        if (padding == 0)
        {
            return text;
        }
        return text + new string('=', padding);
    }
}
