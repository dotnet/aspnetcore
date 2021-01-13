// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.DataProtection
{
    public static partial class DataProtectionCommonExtensions
    {
        public static Microsoft.AspNetCore.DataProtection.IDataProtector CreateProtector(this Microsoft.AspNetCore.DataProtection.IDataProtectionProvider provider, System.Collections.Generic.IEnumerable<string> purposes) { throw null; }
        public static Microsoft.AspNetCore.DataProtection.IDataProtector CreateProtector(this Microsoft.AspNetCore.DataProtection.IDataProtectionProvider provider, string purpose, params string[] subPurposes) { throw null; }
        public static Microsoft.AspNetCore.DataProtection.IDataProtectionProvider GetDataProtectionProvider(this System.IServiceProvider services) { throw null; }
        public static Microsoft.AspNetCore.DataProtection.IDataProtector GetDataProtector(this System.IServiceProvider services, System.Collections.Generic.IEnumerable<string> purposes) { throw null; }
        public static Microsoft.AspNetCore.DataProtection.IDataProtector GetDataProtector(this System.IServiceProvider services, string purpose, params string[] subPurposes) { throw null; }
        public static string Protect(this Microsoft.AspNetCore.DataProtection.IDataProtector protector, string plaintext) { throw null; }
        public static string Unprotect(this Microsoft.AspNetCore.DataProtection.IDataProtector protector, string protectedData) { throw null; }
    }
    public partial interface IDataProtectionProvider
    {
        Microsoft.AspNetCore.DataProtection.IDataProtector CreateProtector(string purpose);
    }
    public partial interface IDataProtector : Microsoft.AspNetCore.DataProtection.IDataProtectionProvider
    {
        byte[] Protect(byte[] plaintext);
        byte[] Unprotect(byte[] protectedData);
    }
}
namespace Microsoft.AspNetCore.DataProtection.Infrastructure
{
    [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
    public partial interface IApplicationDiscriminator
    {
        string Discriminator { get; }
    }
}
