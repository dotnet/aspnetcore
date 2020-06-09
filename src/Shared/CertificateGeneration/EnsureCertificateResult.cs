// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.Certificates.Generation
{
    internal enum EnsureCertificateResult
    {
        Succeeded = 1,
        ValidCertificatePresent,
        ErrorCreatingTheCertificate,
        ErrorSavingTheCertificateIntoTheCurrentUserPersonalStore,
        ErrorExportingTheCertificate,
        FailedToTrustTheCertificate,
        UserCancelledTrustStep,
        FailedToMakeKeyAccessible,
    }
}

