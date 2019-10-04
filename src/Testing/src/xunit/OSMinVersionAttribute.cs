// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Runtime.InteropServices;

namespace Microsoft.AspNetCore.Testing
{
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class | AttributeTargets.Assembly, AllowMultiple = true)]
    public class OSMinVersionAttribute : Attribute, ITestCondition
    {
        private readonly OperatingSystems _targetOS;
        private readonly Version _minVersion;
        private readonly OperatingSystems _currentOS;
        private readonly Version _currentVersion;
        private readonly bool _skip;

        /// <summary>
        /// Used to indicate the minimum version a test can run on for the given operating system. 
        /// Also add <see cref="OSSkipConditionAttribute"/> to skip other operating systems.
        /// </summary>
        /// <param name="targetOS">The OS to check for a version. Only Windows is currently supported.</param>
        /// <param name="minVersion">The minimum OS version NOT to skip.</param>
        public OSMinVersionAttribute(OperatingSystems targetOS, string minVersion) :
            this(targetOS, Version.Parse(minVersion), GetCurrentOS(), GetCurrentOSVersion())
        {
        }

        // to enable unit testing
        internal OSMinVersionAttribute(OperatingSystems targetOS, Version minVersion, OperatingSystems currentOS, Version currentVersion)
        {
            if (targetOS != OperatingSystems.Windows)
            {
                throw new NotImplementedException(targetOS.ToString());
            }

            _targetOS = targetOS;
            _minVersion = minVersion;
            _currentOS = currentOS;
            _currentVersion = currentVersion;

            _skip = _targetOS == _currentOS && _minVersion > _currentVersion;
            SkipReason = $"The test cannot run on this operating system version '{currentVersion}'.";
        }

        // Since a test would be excuted only if 'IsMet' is true, return false if we want to skip
        public bool IsMet => !_skip;

        public string SkipReason { get; set; }

        static private OperatingSystems GetCurrentOS()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                return OperatingSystems.Windows;
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                return OperatingSystems.Linux;
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                return OperatingSystems.MacOSX;
            }
            throw new PlatformNotSupportedException();
        }

        static private Version GetCurrentOSVersion()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                return Environment.OSVersion.Version;
            }
            else
            {
                // Not implmeneted, but this will still be called before the OS check happens so don't throw.
                return new Version(0, 0);
            }
        }
    }
}
