using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Identity;

namespace BlazorWebCSharp._1.Components.Account;

// Maps passkey AAGUIDs to authenticator names.
// The AAGUID data is sourced from the community-maintained list at
// https://github.com/passkeydeveloper/passkey-authenticator-aaguids.
// You can add or modify authenticators as needed.
public static class PasskeyAuthenticators
{
    private static readonly Dictionary<Guid, string> KnownAuthenticators = new()
    {
        // Google Password Manager
        [new("ea9b8d66-4d01-1d21-3ce4-b6b48cb575d4")] = "Google Password Manager",

        // Apple iCloud Keychain
        [new("fbfc3007-154e-4ecc-8c0b-6e020557d7bd")] = "iCloud Keychain",
        [new("dd4ec289-e01d-41c9-bb89-70fa845d4bf2")] = "iCloud Keychain",

        // Microsoft Windows Hello
        [new("08987058-cadc-4b81-b6e1-30de50dcbe96")] = "Windows Hello",
        [new("9ddd1817-af5a-4672-a2b9-3e3dd95000a9")] = "Windows Hello",
        [new("6028b017-b1d4-4c02-b4b3-afcdafc96bb2")] = "Windows Hello",

        // 1Password
        [new("bada5566-a7aa-401f-bd96-45619a55120d")] = "1Password",
        [new("b5397571-8af2-4d30-9d48-eeb8eee6e9c6")] = "1Password",

        // Bitwarden
        [new("d548826e-79b4-db40-a3d8-11116f7e8349")] = "Bitwarden",
        [new("cc45f64e-52a2-451b-831a-4edd8022a202")] = "Bitwarden",
    };

    public static bool TryGetDefaultDisplayName(UserPasskeyInfo passkey, [NotNullWhen(true)] out string? defaultName)
    {
        if (passkey.Aaguid is { Length: 16 } aaguid &&
            KnownAuthenticators.TryGetValue(new Guid(aaguid, bigEndian: true), out var name))
        {
            defaultName = name;
            return true;
        }

        defaultName = null;
        return false;
    }

    public static string GetDisplayName(UserPasskeyInfo passkey)
    {
        if (!string.IsNullOrEmpty(passkey.Name))
        {
            return passkey.Name;
        }

        if (TryGetDefaultDisplayName(passkey, out var name))
        {
            return name;
        }

        return "Unnamed passkey";
    }
}
