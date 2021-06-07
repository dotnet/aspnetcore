using System;
using System.Diagnostics;
using System.IO;
using System.Security.Cryptography.X509Certificates;
using Microsoft.AspNetCore.Certificates.Generation;

#nullable enable

namespace Microsoft.AspNetCore.Certificates.Generation
{
    internal class CertificateFolderStore : CertificateStore
    {
        public string FolderPath { get; }

        private readonly ProcessStartInfo _updateStore;

        private readonly bool _elevate;

        public CertificateFolderStore(string name, string path, ProcessStartInfo updateStore) :
            base(name)
        {
            if (!updateStore.RedirectStandardError)
            {
                throw new ArgumentException(nameof(updateStore));
            }

            FolderPath = path;
            _elevate = updateStore.FileName == "sudo";
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

                string sourceFileName = pemFile;
                string destFileName = GetCertificatePath(certificate);
                DeleteFile(destFileName);
                if (_elevate)
                {
                    var copyProcess = Process.Start(new ProcessStartInfo()
                    {
                        FileName = "sudo",
                        ArgumentList = { "cp", sourceFileName, destFileName },
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                    })!;
                    copyProcess.WaitForExit();
                    var stderr = copyProcess.StandardError.ReadToEnd();
                    copyProcess.WaitForExit();
                    if (copyProcess.ExitCode != 0)
                    {
                        string cmdline = ProcessHelper.GetCommandLine(copyProcess.StartInfo);
                        CertificateManager.Log.LinuxCertificateInstallCommandFailed(cmdline, copyProcess.ExitCode, stderr);
                        return false;
                    }
                }
                else
                {
                    File.Copy(sourceFileName, destFileName);
                }

                {
                    Process updateStoreProcess = Process.Start(_updateStore)!;
                    var stderr = updateStoreProcess.StandardError.ReadToEnd();
                    updateStoreProcess.WaitForExit();
                    if (updateStoreProcess.ExitCode != 0)
                    {
                        string cmdline = ProcessHelper.GetCommandLine(updateStoreProcess.StartInfo);
                        CertificateManager.Log.LinuxCertificateInstallCommandFailed(cmdline, updateStoreProcess.ExitCode, stderr);
                    }
                    return updateStoreProcess.ExitCode != 0;
                }
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

            if (_elevate)
            {
                var process = Process.Start(new ProcessStartInfo()
                {
                    FileName = "sudo",
                    ArgumentList = { "rm", path },
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                })!;
                process.WaitForExit();
            }
            else
            {
                File.Delete(path);
            }
        }
    }
}
