using System;
using System.Diagnostics;
using System.Security.Cryptography.X509Certificates;

#nullable enable

namespace Microsoft.AspNetCore.Certificates.Generation
{
    internal abstract class CertificateStore
    {
        public string StoreName { get; }

        protected CertificateStore(string name)
        {
            StoreName = name;
        }

        public abstract bool TryInstallCertificate(X509Certificate2 certificate);

        public abstract void DeleteCertificate(X509Certificate2 certificate);

        public abstract bool HasCertificate(X509Certificate2 certificate);

        protected bool ContainsCertificate(string storeContent, string certificateContent)
        {
            return false;
        }
    }
}