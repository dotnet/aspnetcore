// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

#if NETCOREAPP2_0 || NETCOREAPP2_1 || NETCOREAPP2_2 || NETCOREAPP3_0

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
        UserCancelledTrustStep
    }
}

#endif
