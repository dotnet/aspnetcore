// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Security.Cryptography.X509Certificates;

namespace Microsoft.AspNetCore.Certificates.Generation;

internal sealed class UnixCertificateManager : CertificateManager
{
    public UnixCertificateManager()
    {
    }

    internal UnixCertificateManager(string subject, int version)
        : base(subject, version)
    {
    }

    public override bool IsTrusted(X509Certificate2 certificate)
    {
        using X509Chain chain = new X509Chain();
        // This is just a heuristic for whether or not we should prompt the user to re-run with `--trust`
        // so we don't need to check revocation (which doesn't really make sense for dev certs anyway)
        chain.ChainPolicy.RevocationMode = X509RevocationMode.NoCheck;
        return chain.Build(certificate);
    }

    protected override X509Certificate2 SaveCertificateCore(X509Certificate2 certificate, StoreName storeName, StoreLocation storeLocation)
    {
        var export = certificate.Export(X509ContentType.Pkcs12, "");
        certificate.Dispose();
        certificate = new X509Certificate2(export, "", X509KeyStorageFlags.PersistKeySet | X509KeyStorageFlags.Exportable);
        Array.Clear(export, 0, export.Length);

        using (var store = new X509Store(storeName, storeLocation))
        {
            store.Open(OpenFlags.ReadWrite);
            store.Add(certificate);
            store.Close();
        };

        return certificate;
    }

    internal override CheckCertificateStateResult CheckCertificateState(X509Certificate2 candidate, bool interactive)
    {
        // Return true as we don't perform any check.
        return new CheckCertificateStateResult(true, null);
    }

    internal override void CorrectCertificateState(X509Certificate2 candidate)
    {
        // Do nothing since we don't have anything to check here.
    }

    protected override bool IsExportable(X509Certificate2 c) => true;

    protected override void TrustCertificateCore(X509Certificate2 certificate) =>
        throw new InvalidOperationException("Trusting the certificate is not supported on linux");

    protected override void RemoveCertificateFromTrustedRoots(X509Certificate2 certificate)
    {
        // No-op here as is benign
    }

    protected override IList<X509Certificate2> GetCertificatesToRemove(StoreName storeName, StoreLocation storeLocation)
    {
        return ListCertificates(StoreName.My, StoreLocation.CurrentUser, isValid: false, requireExportable: false);
    }
}
