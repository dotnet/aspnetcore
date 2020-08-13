// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.DataProtection
{
    public static partial class DataProtectionProvider
    {
        internal static Microsoft.AspNetCore.DataProtection.IDataProtectionProvider CreateProvider(System.IO.DirectoryInfo keyDirectory, System.Action<Microsoft.AspNetCore.DataProtection.IDataProtectionBuilder> setupAction, System.Security.Cryptography.X509Certificates.X509Certificate2 certificate) { throw null; }
    }
    internal sealed partial class TimeLimitedDataProtector : Microsoft.AspNetCore.DataProtection.IDataProtectionProvider, Microsoft.AspNetCore.DataProtection.IDataProtector, Microsoft.AspNetCore.DataProtection.ITimeLimitedDataProtector
    {
        public TimeLimitedDataProtector(Microsoft.AspNetCore.DataProtection.IDataProtector innerProtector) { }
        public Microsoft.AspNetCore.DataProtection.ITimeLimitedDataProtector CreateProtector(string purpose) { throw null; }
        Microsoft.AspNetCore.DataProtection.IDataProtector Microsoft.AspNetCore.DataProtection.IDataProtectionProvider.CreateProtector(string purpose) { throw null; }
        byte[] Microsoft.AspNetCore.DataProtection.IDataProtector.Protect(byte[] plaintext) { throw null; }
        byte[] Microsoft.AspNetCore.DataProtection.IDataProtector.Unprotect(byte[] protectedData) { throw null; }
        public byte[] Protect(byte[] plaintext, System.DateTimeOffset expiration) { throw null; }
        public byte[] Unprotect(byte[] protectedData, out System.DateTimeOffset expiration) { throw null; }
        internal byte[] UnprotectCore(byte[] protectedData, System.DateTimeOffset now, out System.DateTimeOffset expiration) { throw null; }
    }
}
namespace Microsoft.AspNetCore.DataProtection.Extensions
{
    internal static partial class Resources
    {
        internal static string CryptCommon_GenericError { get { throw null; } }
        internal static System.Globalization.CultureInfo Culture { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        internal static System.Resources.ResourceManager ResourceManager { get { throw null; } }
        internal static string TimeLimitedDataProtector_PayloadExpired { get { throw null; } }
        internal static string TimeLimitedDataProtector_PayloadInvalid { get { throw null; } }
        internal static string FormatTimeLimitedDataProtector_PayloadExpired(object p0) { throw null; }
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]internal static string GetResourceString(string resourceKey, string defaultValue = null) { throw null; }
    }
}