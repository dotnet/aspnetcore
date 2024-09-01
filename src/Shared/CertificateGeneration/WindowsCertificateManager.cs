// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Versioning;
using System.Security.AccessControl;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Security.Principal;

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

    internal override CheckCertificateStateResult CheckCertificateState(X509Certificate2 candidate)
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

    protected override TrustLevel TrustCertificateCore(X509Certificate2 certificate)
    {
        using var store = new X509Store(StoreName.Root, StoreLocation.CurrentUser);
        store.Open(OpenFlags.ReadWrite);

        if (TryFindCertificateInStore(store, certificate, out _))
        {
            Log.WindowsCertificateAlreadyTrusted();
            return TrustLevel.Full;
        }

        try
        {
            Log.WindowsAddCertificateToRootStore();

            using var publicCertificate = X509CertificateLoader.LoadCertificate(certificate.Export(X509ContentType.Cert));
            publicCertificate.FriendlyName = certificate.FriendlyName;
            store.Add(publicCertificate);
            return TrustLevel.Full;
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

    public override TrustLevel GetTrustLevel(X509Certificate2 certificate)
    {
        var isTrusted = ListCertificates(StoreName.Root, StoreLocation.CurrentUser, isValid: true, requireExportable: false)
            .Any(c => AreCertificatesEqual(c, certificate));
        return isTrusted ? TrustLevel.Full : TrustLevel.None;
    }

    protected override IList<X509Certificate2> GetCertificatesToRemove(StoreName storeName, StoreLocation storeLocation)
    {
        return ListCertificates(storeName, storeLocation, isValid: false);
    }

    protected override void CreateDirectoryWithPermissions(string directoryPath)
    {
        var dirInfo = new DirectoryInfo(directoryPath);

        if (!dirInfo.Exists)
        {
            // We trust the default permissions on Windows enough not to apply custom ACLs.
            // We'll warn below if things seem really off.
            dirInfo.Create();
        }

        var currentUser = WindowsIdentity.GetCurrent();
        var currentUserSid = currentUser.User;
        var systemSid = new SecurityIdentifier(WellKnownSidType.LocalSystemSid, domainSid: null);
        var adminGroupSid = new SecurityIdentifier(WellKnownSidType.BuiltinAdministratorsSid, domainSid: null);

        var dirSecurity = dirInfo.GetAccessControl();
        var accessRules = dirSecurity.GetAccessRules(true, true, typeof(SecurityIdentifier));

        foreach (FileSystemAccessRule rule in accessRules)
        {
            var idRef = rule.IdentityReference;
            if (rule.AccessControlType == AccessControlType.Allow &&
                !idRef.Equals(currentUserSid) &&
                !idRef.Equals(systemSid) &&
                !idRef.Equals(adminGroupSid))
            {
                // This is just a heuristic - determining whether the cumulative effect of the rules
                // is to allow access to anyone other than the current user, system, or administrators
                // is very complicated.  We're not going to do anything but log, so an approximation
                // is fine.
                Log.DirectoryPermissionsNotSecure(dirInfo.FullName);
                break;
            }
        }
    }
}
