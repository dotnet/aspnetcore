// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Text;

namespace NuGet
{
    /// <summary>
    /// A hybrid implementation of SemVer that supports semantic versioning as described at http://semver.org while not strictly enforcing it to 
    /// allow older 4-digit versioning schemes to continue working.
    /// </summary>
    internal sealed class SemanticVersion : IComparable, IComparable<SemanticVersion>, IEquatable<SemanticVersion>
    {
        private string _normalizedVersionString;

        public SemanticVersion(string version)
            : this(Parse(version))
        {
        }

        public SemanticVersion(int major, int minor, int build, int revision)
            : this(new Version(major, minor, build, revision))
        {
        }

        public SemanticVersion(int major, int minor, int build, string specialVersion)
            : this(new Version(major, minor, build), specialVersion)
        {
        }

        public SemanticVersion(Version version)
            : this(version, string.Empty)
        {
        }

        public SemanticVersion(Version version, string specialVersion)
        {
            if (version == null)
            {
                throw new ArgumentNullException(nameof(version));
            }
            Version = NormalizeVersionValue(version);
            SpecialVersion = specialVersion ?? string.Empty;
        }

        internal SemanticVersion(SemanticVersion semVer)
        {
            Version = semVer.Version;
            SpecialVersion = semVer.SpecialVersion;
        }

        /// <summary>
        /// Gets the normalized version portion.
        /// </summary>
        public Version Version
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets the optional special version.
        /// </summary>
        public string SpecialVersion
        {
            get;
            private set;
        }

        private static string[] SplitAndPadVersionString(string version)
        {
            string[] a = version.Split('.');
            if (a.Length == 4)
            {
                return a;
            }
            else
            {
                // if 'a' has less than 4 elements, we pad the '0' at the end 
                // to make it 4.
                var b = new string[4] { "0", "0", "0", "0" };
                Array.Copy(a, 0, b, 0, a.Length);
                return b;
            }
        }

        /// <summary>
        /// Parses a version string using loose semantic versioning rules that allows 2-4 version components followed by an optional special version.
        /// </summary>
        public static SemanticVersion Parse(string version)
        {
            if (string.IsNullOrEmpty(version))
            {
                throw new ArgumentNullException(nameof(version));
            }

            SemanticVersion semVer;
            if (!TryParse(version, out semVer))
            {
                throw new ArgumentException(nameof(version));
            }
            return semVer;
        }

        /// <summary>
        /// Parses a version string using loose semantic versioning rules that allows 2-4 version components followed by an optional special version.
        /// </summary>
        public static bool TryParse(string version, out SemanticVersion value)
        {
            return TryParseInternal(version, strict: false, semVer: out value);
        }

        /// <summary>
        /// Parses a version string using strict semantic versioning rules that allows exactly 3 components and an optional special version.
        /// </summary>
        public static bool TryParseStrict(string version, out SemanticVersion value)
        {
            return TryParseInternal(version, strict: true, semVer: out value);
        }

        private static bool TryParseInternal(string version, bool strict, out SemanticVersion semVer)
        {
            semVer = null;
            if (string.IsNullOrEmpty(version))
            {
                return false;
            }

            version = version.Trim();
            var versionPart = version;

            string specialVersion = string.Empty;
            if (version.IndexOf('-') != -1)
            {
                var parts = version.Split(new char[] { '-' }, 2, StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length != 2)
                {
                    return false;
                }

                versionPart = parts[0];
                specialVersion = parts[1];
            }

            Version versionValue;
            if (!Version.TryParse(versionPart, out versionValue))
            {
                return false;
            }

            if (strict)
            {
                // Must have major, minor and build only.
                if (versionValue.Major == -1 ||
                    versionValue.Minor == -1 ||
                    versionValue.Build == -1 ||
                    versionValue.Revision != -1)
                {
                    return false;
                }
            }

            semVer = new SemanticVersion(NormalizeVersionValue(versionValue), specialVersion);
            return true;
        }

        /// <summary>
        /// Attempts to parse the version token as a SemanticVersion.
        /// </summary>
        /// <returns>An instance of SemanticVersion if it parses correctly, null otherwise.</returns>
        public static SemanticVersion ParseOptionalVersion(string version)
        {
            SemanticVersion semVer;
            TryParse(version, out semVer);
            return semVer;
        }

        private static Version NormalizeVersionValue(Version version)
        {
            return new Version(version.Major,
                               version.Minor,
                               Math.Max(version.Build, 0),
                               Math.Max(version.Revision, 0));
        }

        public int CompareTo(object obj)
        {
            if (Object.ReferenceEquals(obj, null))
            {
                return 1;
            }
            SemanticVersion other = obj as SemanticVersion;
            if (other == null)
            {
                throw new ArgumentException(nameof(obj));
            }
            return CompareTo(other);
        }

        public int CompareTo(SemanticVersion other)
        {
            if (Object.ReferenceEquals(other, null))
            {
                return 1;
            }

            int result = Version.CompareTo(other.Version);

            if (result != 0)
            {
                return result;
            }

            bool empty = string.IsNullOrEmpty(SpecialVersion);
            bool otherEmpty = string.IsNullOrEmpty(other.SpecialVersion);
            if (empty && otherEmpty)
            {
                return 0;
            }
            else if (empty)
            {
                return 1;
            }
            else if (otherEmpty)
            {
                return -1;
            }
            return StringComparer.OrdinalIgnoreCase.Compare(SpecialVersion, other.SpecialVersion);
        }

        public static bool operator ==(SemanticVersion version1, SemanticVersion version2)
        {
            if (Object.ReferenceEquals(version1, null))
            {
                return Object.ReferenceEquals(version2, null);
            }
            return version1.Equals(version2);
        }

        public static bool operator !=(SemanticVersion version1, SemanticVersion version2)
        {
            return !(version1 == version2);
        }

        public static bool operator <(SemanticVersion version1, SemanticVersion version2)
        {
            if (version1 == null)
            {
                throw new ArgumentNullException(nameof(version1));
            }
            return version1.CompareTo(version2) < 0;
        }

        public static bool operator <=(SemanticVersion version1, SemanticVersion version2)
        {
            return (version1 == version2) || (version1 < version2);
        }

        public static bool operator >(SemanticVersion version1, SemanticVersion version2)
        {
            if (version1 == null)
            {
                throw new ArgumentNullException(nameof(version1));
            }
            return version2 < version1;
        }

        public static bool operator >=(SemanticVersion version1, SemanticVersion version2)
        {
            return (version1 == version2) || (version1 > version2);
        }

        public override string ToString()
        {
            if (_normalizedVersionString == null)
            {
                var builder = new StringBuilder();
                builder
                    .Append(Version.Major)
                    .Append('.')
                    .Append(Version.Minor)
                    .Append('.')
                    .Append(Math.Max(0, Version.Build));

                if (Version.Revision > 0)
                {
                    builder
                        .Append('.')
                        .Append(Version.Revision);
                }

                if (!string.IsNullOrEmpty(SpecialVersion))
                {
                    builder
                        .Append('-')
                        .Append(SpecialVersion);
                }
                
                _normalizedVersionString = builder.ToString();
            }

            return _normalizedVersionString;
        }

        public bool Equals(SemanticVersion other)
        {
            return !Object.ReferenceEquals(null, other) &&
                   Version.Equals(other.Version) &&
                   SpecialVersion.Equals(other.SpecialVersion, StringComparison.OrdinalIgnoreCase);
        }

        public override bool Equals(object obj)
        {
            SemanticVersion semVer = obj as SemanticVersion;
            return !Object.ReferenceEquals(null, semVer) && Equals(semVer);
        }

        public override int GetHashCode()
        {
            int hashCode = Version.GetHashCode();
            if (SpecialVersion != null)
            {
                hashCode = hashCode * 4567 + SpecialVersion.GetHashCode();
            }

            return hashCode;
        }
    }
}
