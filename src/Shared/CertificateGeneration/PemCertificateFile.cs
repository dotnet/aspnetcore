using System;
using System.IO;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

#nullable enable

namespace Microsoft.AspNetCore.Certificates.Generation
{
    internal class PemCertificateFile : IDisposable
    {
        public string FilePath { get; }
        public X509Certificate2 Certificate { get; }

        public PemCertificateFile(X509Certificate2 certificate)
        {
            Certificate = certificate;
            string directory = Paths.XdgRuntimeDir ?? Paths.Home ?? Path.GetTempPath();
            FilePath = Path.Combine(directory, Guid.NewGuid() + ".pem");
            string pem = new string(PemEncoding.Write("CERTIFICATE", certificate.Export(X509ContentType.Cert)));
            File.WriteAllText(FilePath, pem);
        }

        public void Dispose()
        {
            try
            {
                File.Delete(FilePath);
            }
            finally
            { }
        }
    }
}