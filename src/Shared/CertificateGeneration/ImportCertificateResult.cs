// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Certificates.Generation;

internal enum ImportCertificateResult
{
    Succeeded = 1,
    CertificateFileMissing,
    InvalidCertificate,
    NoDevelopmentHttpsCertificate,
    ExistingCertificatesPresent,
    ErrorSavingTheCertificateIntoTheCurrentUserPersonalStore,
}

