// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.DataProtection
{
    public static partial class DataProtectionAdvancedExtensions
    {
        public static byte[] Protect(this Microsoft.AspNetCore.DataProtection.ITimeLimitedDataProtector protector, byte[] plaintext, System.TimeSpan lifetime) { throw null; }
        public static string Protect(this Microsoft.AspNetCore.DataProtection.ITimeLimitedDataProtector protector, string plaintext, System.DateTimeOffset expiration) { throw null; }
        public static string Protect(this Microsoft.AspNetCore.DataProtection.ITimeLimitedDataProtector protector, string plaintext, System.TimeSpan lifetime) { throw null; }
        public static Microsoft.AspNetCore.DataProtection.ITimeLimitedDataProtector ToTimeLimitedDataProtector(this Microsoft.AspNetCore.DataProtection.IDataProtector protector) { throw null; }
        public static string Unprotect(this Microsoft.AspNetCore.DataProtection.ITimeLimitedDataProtector protector, string protectedData, out System.DateTimeOffset expiration) { throw null; }
    }
    public static partial class DataProtectionProvider
    {
        public static Microsoft.AspNetCore.DataProtection.IDataProtectionProvider Create(System.IO.DirectoryInfo keyDirectory) { throw null; }
        public static Microsoft.AspNetCore.DataProtection.IDataProtectionProvider Create(System.IO.DirectoryInfo keyDirectory, System.Action<Microsoft.AspNetCore.DataProtection.IDataProtectionBuilder> setupAction) { throw null; }
        public static Microsoft.AspNetCore.DataProtection.IDataProtectionProvider Create(System.IO.DirectoryInfo keyDirectory, System.Action<Microsoft.AspNetCore.DataProtection.IDataProtectionBuilder> setupAction, System.Security.Cryptography.X509Certificates.X509Certificate2 certificate) { throw null; }
        public static Microsoft.AspNetCore.DataProtection.IDataProtectionProvider Create(System.IO.DirectoryInfo keyDirectory, System.Security.Cryptography.X509Certificates.X509Certificate2 certificate) { throw null; }
        public static Microsoft.AspNetCore.DataProtection.IDataProtectionProvider Create(string applicationName) { throw null; }
        public static Microsoft.AspNetCore.DataProtection.IDataProtectionProvider Create(string applicationName, System.Security.Cryptography.X509Certificates.X509Certificate2 certificate) { throw null; }
    }
    public partial interface ITimeLimitedDataProtector : Microsoft.AspNetCore.DataProtection.IDataProtectionProvider, Microsoft.AspNetCore.DataProtection.IDataProtector
    {
        new Microsoft.AspNetCore.DataProtection.ITimeLimitedDataProtector CreateProtector(string purpose);
        byte[] Protect(byte[] plaintext, System.DateTimeOffset expiration);
        byte[] Unprotect(byte[] protectedData, out System.DateTimeOffset expiration);
    }
}
