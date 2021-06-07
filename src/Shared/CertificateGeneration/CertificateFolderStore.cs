using System.IO;
using System.Security.Cryptography.X509Certificates;
using Microsoft.AspNetCore.Certificates.Generation;

#nullable enable

namespace Microsoft.AspNetCore.Certificates.Generation
{
    internal class CertificateFolderStore : CertificateStore
    {
        public string FolderPath { get; }

        private readonly ProcessRunOptions _updateStore;

        private bool Elevate => _updateStore.Elevate;

        public CertificateFolderStore(string name, string path, ProcessRunOptions updateStore) :
            base(name)
        {
            FolderPath = path;
            _updateStore = updateStore;
        }

        public override bool CheckDependencies()
        {
            return CheckProgramDependency(_updateStore);
        }

        public override bool TryInstallCertificate(X509Certificate2 certificate)
        {
            string pemFile = Paths.GetUserTempFile(".pem");
            try
            {
                CertificateManager.ExportCertificate(certificate, pemFile, includePrivateKey: false, password: null, CertificateKeyExportFormat.Pem);
                CopyFile(pemFile, GetCertificatePath(certificate));
                ProcessRunner.Run(_updateStore);
                return true;
            }
            finally
            {
                try
                {
                    File.Delete(pemFile);
                }
                catch
                { }
            }
        }

        public override void DeleteCertificate(X509Certificate2 certificate)
        {
            DeleteFile(GetCertificatePath(certificate));
        }

        public override bool HasCertificate(X509Certificate2 certificate)
        {
            string certificatePath = GetCertificatePath(certificate);
            if (!File.Exists(certificatePath))
            {
                return false;
            }
            X509Certificate2 storeCertificate = X509Certificate2.CreateFromPem(File.ReadAllText(certificatePath));
            return storeCertificate.Equals(certificate);
        }

        private string GetCertificatePath(X509Certificate2 certificate)
            => Path.Combine(FolderPath, "aspnet-" + certificate.Thumbprint + ".pem");

        private void DeleteFile(string path)
        {
            if (!File.Exists(path))
            {
                return;
            }

            if (Elevate)
            {
                ProcessRunner.Run(new()
                {
                    Command = { "rm", path },
                    Elevate = true
                });
            }
            else
            {
                File.Delete(path);
            }
        }

        private void CopyFile(string sourceFileName, string destFileName)
        {
            DeleteFile(destFileName);

            if (Elevate)
            {
                ProcessRunner.Run(new()
                {
                    Command = { "cp", sourceFileName, destFileName },
                    Elevate = true
                });
            }
            else
            {
                File.Copy(sourceFileName, destFileName);
            }
        }
    }
}
