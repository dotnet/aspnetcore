using System;
using System.Diagnostics;
using System.IO;
using System.Security.Cryptography.X509Certificates;
using Microsoft.AspNetCore.Certificates.Generation;

#nullable enable

namespace Microsoft.AspNetCore.Certificates.Generation
{
    internal class SystemCertificateFolderStore : CertificateStore
    {
        enum LinuxFlavor
        {
            Fedora,
            Debian
        }

        private readonly LinuxFlavor _linuxFlavor;

        private string FolderPath =>
            _linuxFlavor switch
            {
                LinuxFlavor.Fedora => "/etc/pki/tls/certs",
                LinuxFlavor.Debian => "/usr/local/share/ca-certificates",
                _ => throw new IndexOutOfRangeException()
            };

        private string Extension =>
            _linuxFlavor switch
            {
                LinuxFlavor.Fedora => ".pem",
                LinuxFlavor.Debian => ".crt",
                _ => throw new IndexOutOfRangeException()
            };

        public static SystemCertificateFolderStore? Instance { get; } = CreateSystemCertificateFolderStore();

        private static SystemCertificateFolderStore? CreateSystemCertificateFolderStore()
        {
            if (ProcessHelper.HasProgram("yum"))
            {
                return new SystemCertificateFolderStore(LinuxFlavor.Fedora);
            }
            else if (ProcessHelper.HasProgram("apt"))
            {
                return new SystemCertificateFolderStore(LinuxFlavor.Debian);
            }
            return null;
        }

        private SystemCertificateFolderStore(LinuxFlavor linuxFlavor) :
            base("System certificates")
        {
            _linuxFlavor = linuxFlavor;
        }

        public override bool TryInstallCertificate(X509Certificate2 certificate)
        {
            if (!TryCopyToCertificateFolder(certificate))
            {
                return false;
            }

            ProcessStartInfo psi = _linuxFlavor switch
            {
                LinuxFlavor.Fedora => new ProcessStartInfo()
                    {
                        FileName = "sudo",
                        ArgumentList = { "update-ca-trust" }
                    },
                LinuxFlavor.Debian => new ProcessStartInfo()
                    {
                        FileName = "sudo",
                        ArgumentList = { "update-ca-certificates" }
                    },
                _ => throw new IndexOutOfRangeException()
            };
            psi.RedirectStandardOutput = true;
            psi.RedirectStandardError = true;

            Process updateStoreProcess = Process.Start(psi)!;
            var stderr = updateStoreProcess.StandardError.ReadToEnd();
            updateStoreProcess.WaitForExit();

            if (updateStoreProcess.ExitCode != 0)
            {
                string cmdline = ProcessHelper.GetCommandLine(updateStoreProcess.StartInfo);
                CertificateManager.Log.LinuxCertificateInstallCommandFailed(cmdline, updateStoreProcess.ExitCode, stderr);
            }

            return updateStoreProcess.ExitCode != 0;
        }

        private bool TryCopyToCertificateFolder(X509Certificate2 certificate)
        {
            string pemFile = Paths.GetUserTempFile(".pem");
            try
            {
                CertificateManager.ExportCertificate(certificate, pemFile, includePrivateKey: false, password: null, CertificateKeyExportFormat.Pem);

                string sourceFileName = pemFile;
                string destFileName = GetCertificatePath(certificate);

                DeleteFile(destFileName);

                var copyProcess = Process.Start(new ProcessStartInfo()
                {
                    FileName = "sudo",
                    ArgumentList = { "cp", sourceFileName, destFileName },
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                })!;
                var stderr = copyProcess.StandardError.ReadToEnd();
                copyProcess.WaitForExit();

                if (copyProcess.ExitCode != 0)
                {
                    string cmdline = ProcessHelper.GetCommandLine(copyProcess.StartInfo);
                    CertificateManager.Log.LinuxCertificateInstallCommandFailed(cmdline, copyProcess.ExitCode, stderr);
                    return false;
                }

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
            => Path.Combine(FolderPath, "aspnet-" + certificate.Thumbprint + Extension);

        private void DeleteFile(string path)
        {
            if (!File.Exists(path))
            {
                return;
            }

            var process = Process.Start(new ProcessStartInfo()
            {
                FileName = "sudo",
                ArgumentList = { "rm", path },
                RedirectStandardOutput = true,
                RedirectStandardError = true,
            })!;
            process.WaitForExit();
        }
    }
}
