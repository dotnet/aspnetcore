using System.IO;
using System.Security.Cryptography.X509Certificates;
using Microsoft.AspNetCore.Certificates.Generation;
using Microsoft.Extensions.Tools.Internal;

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

        private void ReportElevationNeeded(IReporter? reporter)
        {
            if (Elevate)
            {
                reporter?.Output($"Changing '{StoreName}' requires root priviledges. You may be prompted for your password.");
            }
        }

        public override bool CheckDependencies(IReporter? reporter)
        {
            return CheckProgramDependency(_updateStore, reporter);
        }

        public override bool TryInstallCertificate(string name, PemCertificateFile pemFile, IReporter? reporter, bool isInteractive)
        {
            ReportElevationNeeded(reporter);
            CopyFile(pemFile.FilePath, GetCertificatePath(name), isInteractive);
            ProcessRunner.Run(_updateStore with { IsInteractive = isInteractive });
            return true;
        }

        public override void DeleteCertificate(string name, IReporter? reporter, bool isInteractive)
        {
            ReportElevationNeeded(reporter);
            DeleteFile(GetCertificatePath(name), isInteractive);
        }

        public override bool HasCertificate(string name, X509Certificate2 certificate)
        {
            string certificatePath = GetCertificatePath(name);
            if (!File.Exists(certificatePath))
            {
                return false;
            }
            X509Certificate2 storeCertificate = X509Certificate2.CreateFromPem(File.ReadAllText(certificatePath));
            return storeCertificate.Equals(certificate);
        }

        private string GetCertificatePath(string name)
            => Path.Combine(FolderPath, name + ".pem");

        private void DeleteFile(string path, bool isInteractive)
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
                    Elevate = true,
                    IsInteractive = isInteractive
                });
            }
            else
            {
                File.Delete(path);
            }
        }

        private void CopyFile(string sourceFileName, string destFileName, bool isInteractive)
        {
            DeleteFile(destFileName, isInteractive);

            if (Elevate)
            {
                ProcessRunner.Run(new()
                {
                    Command = { "cp", sourceFileName, destFileName },
                    Elevate = true,
                    IsInteractive = isInteractive
                });
            }
            else
            {
                File.Copy(sourceFileName, destFileName);
            }
        }
    }
}
