// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Certificates.Generation;

internal enum EnsureCertificateResult
{
    Succeeded = 1,
    ValidCertificatePresent,
    ErrorCreatingTheCertificate,
    ErrorSavingTheCertificateIntoTheCurrentUserPersonalStore,
    ErrorExportingTheCertificate,
    ErrorExportingTheCertificateToNonExistentDirectory,
    FailedToTrustTheCertificate,
    PartiallyFailedToTrustTheCertificate,
    UserCancelledTrustStep,
    FailedToMakeKeyAccessible,
    ExistingHttpsCertificateTrusted,
    NewHttpsCertificateTrusted
}

