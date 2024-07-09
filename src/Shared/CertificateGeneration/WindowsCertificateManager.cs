// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Versioning;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace Microsoft.AspNetCore.Certificates.Generation;

[SupportedOSPlatform("windows")]
internal sealed class WindowsCertificateManager : CertificateManager
{
    private const int UserCancelledErrorCode = 1223;

    public WindowsCertificateManager()
    {
    }

    // For testing purposes only
    internal WindowsCertificateManager(string subject, int version)
        : base(subject, version)
    {
    }

    protected override bool IsExportable(X509Certificate2 c)
    {
#if XPLAT
        // For the first run experience we don't need to know if the certificate can be exported.
        return true;
#else
        using var key = c.GetRSAPrivateKey();
        return (key is RSACryptoServiceProvider rsaPrivateKey &&
                rsaPrivateKey.CspKeyContainerInfo.Exportable) ||
            (key is RSACng cngPrivateKey &&
                cngPrivateKey.Key.ExportPolicy == CngExportPolicies.AllowExport);
#endif
    }

    internal override CheckCertificateStateResult CheckCertificateState(X509Certificate2 candidate, bool interactive)
    {
        return new CheckCertificateStateResult(true, null);
    }

    internal override void CorrectCertificateState(X509Certificate2 candidate)
    {
        // Do nothing since we don't have anything to check here.
    }

    protected override X509Certificate2 SaveCertificateCore(X509Certificate2 certificate, StoreName storeName, StoreLocation storeLocation)
    {
        // On non OSX systems we need to export the certificate and import it so that the transient
        // key that we generated gets persisted.
        var export = certificate.Export(X509ContentType.Pkcs12, "");
        certificate.Dispose();
        certificate = new X509Certificate2(export, "", X509KeyStorageFlags.PersistKeySet | X509KeyStorageFlags.Exportable);
        Array.Clear(export, 0, export.Length);
        certificate.FriendlyName = AspNetHttpsOidFriendlyName;

        using (var store = new X509Store(storeName, storeLocation))
        {
            store.Open(OpenFlags.ReadWrite);
            store.Add(certificate);
            store.Close();
        };

        return certificate;
    }

    protected override void TrustCertificateCore(X509Certificate2 certificate)
    {
        using var store = new X509Store(StoreName.Root, StoreLocation.CurrentUser);
        store.Open(OpenFlags.ReadWrite);

        if (TryFindCertificateInStore(store, certificate, out _))
        {
            Log.WindowsCertificateAlreadyTrusted();
            return;
        }

        try
        {
            Log.WindowsAddCertificateToRootStore();

            using var publicCertificate = X509CertificateLoader.LoadCertificate(certificate.Export(X509ContentType.Cert));
            publicCertificate.FriendlyName = certificate.FriendlyName;
            store.Add(publicCertificate);
        }
        catch (CryptographicException exception) when (exception.HResult == UserCancelledErrorCode)
        {
            Log.WindowsCertificateTrustCanceled();
            throw new UserCancelledTrustException();
        }
    }

    protected override void RemoveCertificateFromTrustedRoots(X509Certificate2 certificate)
    {
        Log.WindowsRemoveCertificateFromRootStoreStart();

        using var store = new X509Store(StoreName.Root, StoreLocation.CurrentUser);
        store.Open(OpenFlags.ReadWrite);

        if (TryFindCertificateInStore(store, certificate, out var matching))
        {
            store.Remove(matching);
        }
        else
        {
            Log.WindowsRemoveCertificateFromRootStoreNotFound();
        }

        Log.WindowsRemoveCertificateFromRootStoreEnd();
    }

    public override bool IsTrusted(X509Certificate2 certificate)
    {
        return ListCertificates(StoreName.Root, StoreLocation.CurrentUser, isValid: true, requireExportable: false)
            .Any(c => AreCertificatesEqual(c, certificate));
    }

    protected override IList<X509Certificate2> GetCertificatesToRemove(StoreName storeName, StoreLocation storeLocation)
    {
        return ListCertificates(storeName, storeLocation, isValid: false);
    }
}
