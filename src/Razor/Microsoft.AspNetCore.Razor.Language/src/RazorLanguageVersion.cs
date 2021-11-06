// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;

namespace Microsoft.AspNetCore.Razor.Language;

// Note: RazorSDK is aware of version monikers such as "latest", and "experimental". Update it if we introduce new monikers.
[DebuggerDisplay("{" + nameof(DebuggerToString) + "(),nq}")]
public sealed class RazorLanguageVersion : IEquatable<RazorLanguageVersion>, IComparable<RazorLanguageVersion>
{
    public static readonly RazorLanguageVersion Version_1_0 = new RazorLanguageVersion(1, 0);

    public static readonly RazorLanguageVersion Version_1_1 = new RazorLanguageVersion(1, 1);

    public static readonly RazorLanguageVersion Version_2_0 = new RazorLanguageVersion(2, 0);

    public static readonly RazorLanguageVersion Version_2_1 = new RazorLanguageVersion(2, 1);

    public static readonly RazorLanguageVersion Version_3_0 = new RazorLanguageVersion(3, 0);

    public static readonly RazorLanguageVersion Version_5_0 = new RazorLanguageVersion(5, 0);

    public static readonly RazorLanguageVersion Version_6_0 = new RazorLanguageVersion(6, 0);

    public static readonly RazorLanguageVersion Latest = Version_6_0;

    public static readonly RazorLanguageVersion Experimental = new RazorLanguageVersion(1337, 1337);

    public static bool TryParse(string languageVersion, out RazorLanguageVersion version)
    {
        if (languageVersion == null)
        {
            throw new ArgumentNullException(nameof(languageVersion));
        }

        if (string.Equals(languageVersion, "latest", StringComparison.OrdinalIgnoreCase))
        {
            version = Latest;
            return true;
        }
        else if (string.Equals(languageVersion, "experimental", StringComparison.OrdinalIgnoreCase))
        {
            version = Experimental;
            return true;
        }
        else if (languageVersion == "6.0")
        {
            version = Version_6_0;
            return true;
        }
        else if (languageVersion == "5.0")
        {
            version = Version_5_0;
            return true;
        }
        else if (languageVersion == "3.0")
        {
            version = Version_3_0;
            return true;
        }
        else if (languageVersion == "2.1")
        {
            version = Version_2_1;
            return true;
        }
        else if (languageVersion == "2.0")
        {
            version = Version_2_0;
            return true;
        }
        else if (languageVersion == "1.1")
        {
            version = Version_1_1;
            return true;
        }
        else if (languageVersion == "1.0")
        {
            version = Version_1_0;
            return true;
        }

        version = null;
        return false;
    }

    public static RazorLanguageVersion Parse(string languageVersion)
    {
        if (languageVersion == null)
        {
            throw new ArgumentNullException(nameof(languageVersion));
        }

        if (TryParse(languageVersion, out var parsed))
        {
            return parsed;
        }

        throw new ArgumentException(
            Resources.FormatRazorLanguageVersion_InvalidVersion(languageVersion),
            nameof(languageVersion));
    }

    // Don't want anyone else constructing language versions.
    private RazorLanguageVersion(int major, int minor)
    {
        Major = major;
        Minor = minor;
    }

    public int Major { get; }

    public int Minor { get; }

    public int CompareTo(RazorLanguageVersion other)
    {
        if (other == null)
        {
            throw new ArgumentNullException(nameof(other));
        }

        var result = Major.CompareTo(other.Major);
        if (result != 0)
        {
            return result;
        }

        return Minor.CompareTo(other.Minor);
    }

    public bool Equals(RazorLanguageVersion other)
    {
        if (other == null)
        {
            return false;
        }

        // We're the only one who can create RazorLanguageVersions so reference equality is sufficient.
        return ReferenceEquals(this, other);
    }

    public override int GetHashCode()
    {
        // We don't need to do anything special for our hash code since reference equality is what we're going for.
        return base.GetHashCode();
    }

    public override string ToString() => $"{Major}.{Minor}";

    private string DebuggerToString() => $"Razor '{Major}.{Minor}'";
}
