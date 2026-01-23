// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Identity.EntityFrameworkCore;

internal static class IdentityUserPasskeyExtensions
{
    extension<TKey>(IdentityUserPasskey<TKey> passkey)
        where TKey : IEquatable<TKey>
    {
        public void UpdateFromUserPasskeyInfo(UserPasskeyInfo passkeyInfo)
        {
            passkey.Data.Name = passkeyInfo.Name;
            passkey.Data.SignCount = passkeyInfo.SignCount;
            passkey.Data.IsBackedUp = passkeyInfo.IsBackedUp;
            passkey.Data.IsUserVerified = passkeyInfo.IsUserVerified;
        }

        public UserPasskeyInfo ToUserPasskeyInfo()
            => new(
                passkey.CredentialId,
                passkey.Data.PublicKey,
                passkey.Data.CreatedAt,
                passkey.Data.SignCount,
                passkey.Data.Transports,
                passkey.Data.IsUserVerified,
                passkey.Data.IsBackupEligible,
                passkey.Data.IsBackedUp,
                passkey.Data.AttestationObject,
                passkey.Data.ClientDataJson)
            {
                Name = passkey.Data.Name
            };
    }
}
