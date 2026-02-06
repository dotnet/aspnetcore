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
            // We only mutate properties that can be update after passkey creation.
            // See https://www.w3.org/TR/webauthn-3/#authn-ceremony-update-credential-record
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
                Name = passkey.Data.Name,
                Aaguid = passkey.Data.Aaguid,
            };
    }
}
