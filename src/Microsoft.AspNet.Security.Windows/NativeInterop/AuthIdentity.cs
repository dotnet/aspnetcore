// -----------------------------------------------------------------------
// <copyright file="AuthIdentity.cs" company="Microsoft">
//      Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System.Runtime.InteropServices;

namespace Microsoft.AspNet.Security.Windows
{
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
    internal struct AuthIdentity
    {
        // see SEC_WINNT_AUTH_IDENTITY_W
        internal string UserName;
        internal int UserNameLength;
        internal string Domain;
        internal int DomainLength;
        internal string Password;
        internal int PasswordLength;
        internal int Flags;

        internal AuthIdentity(string userName, string password, string domain)
        {
            UserName = userName;
            UserNameLength = userName == null ? 0 : userName.Length;
            Password = password;
            PasswordLength = password == null ? 0 : password.Length;
            Domain = domain;
            DomainLength = domain == null ? 0 : domain.Length;
            // Flags are 2 for Unicode and 1 for ANSI. We use 2 on NT and 1 on Win9x.
            Flags = 2;
        }

        public override string ToString()
        {
            return ValidationHelper.ToString(Domain) + "\\" + ValidationHelper.ToString(UserName);
        }
    }
}
