using System;
using System.Collections.Generic;
using System.Security.Cryptography.X509Certificates;

#nullable enable

namespace Microsoft.AspNetCore.Certificates.Generation
{
    internal class UnixCertificateManager : CertificateManager
    {
        private List<CertificateStore>? _certificateStores;

        private List<CertificateStore> CertificateStores
            => _certificateStores ??= CertificateStoreFinder.FindCertificateStores();

        public UnixCertificateManager()
        {
        }

        internal UnixCertificateManager(string subject, int version)
            : base(subject, version)
        {
        }

        public override bool IsTrusted(X509Certificate2 certificate)
        {
            // TODO: support 'partial' return.

            // Return true when all stores trust the cert.
            foreach (var store in CertificateStores)
            {
                if (!store.HasCertificate(certificate))
                {
                    return false;
                }
            }
            return CertificateStores.Count > 0;
        }

        public override bool SupportsTrust => CertificateStores.Count > 0;

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

        protected override void TrustCertificateCore(X509Certificate2 certificate)
        {
            foreach (var store in CertificateStores)
            {
                CertificateManager.Log.LinuxTrustCertificate(CertificateManager.GetDescription(certificate), store.StoreName);
                store.TryInstallCertificate(certificate);
                // TODO: handle failure.
            }
        }

        protected override void RemoveCertificateFromTrustedRoots(X509Certificate2 certificate)
        {
            foreach (var store in CertificateStores)
            {
                store.DeleteCertificate(certificate);
            }
        }

        protected override IList<X509Certificate2> GetCertificatesToRemove(StoreName storeName, StoreLocation storeLocation)
        {
            return ListCertificates(StoreName.My, StoreLocation.CurrentUser, isValid: false, requireExportable: false);
        }
    }
}
