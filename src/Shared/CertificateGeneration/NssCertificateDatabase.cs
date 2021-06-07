using System;
using System.Diagnostics;
using System.IO;
using System.Security.Cryptography.X509Certificates;

#nullable enable

namespace Microsoft.AspNetCore.Certificates.Generation
{
    internal class NssCertificateDatabase : CertificateStore
    {
        public string DatabasePath { get; }

        public NssCertificateDatabase(string name, string path) :
            base(name)
        {
            DatabasePath = path;
        }

        public override bool TryInstallCertificate(X509Certificate2 certificate)
        {
            string pemFile = Paths.GetUserTempFile(".pem");
            try
            {
                CertificateManager.ExportCertificate(certificate, pemFile, includePrivateKey: false, password: null, CertificateKeyExportFormat.Pem);
                string name = GetCertificateNickname(certificate);
                Process process = Process.Start(new ProcessStartInfo() {
                    FileName = "certutil",
                    ArgumentList = { "-d", DatabasePath, "-A", "-t", "C,,", "-n", name, "-i", pemFile },
                    RedirectStandardOutput = true,
                    RedirectStandardError = true })!;
                var stderr = process.StandardError.ReadToEnd();
                process.WaitForExit();
                bool success = process.ExitCode == 0;
                if (!success)
                {
                    string cmdline = ProcessHelper.GetCommandLine(process.StartInfo);
                    CertificateManager.Log.LinuxCertificateInstallCommandFailed(cmdline, process.ExitCode, stderr);
                }
                return success;
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

        public override bool HasCertificate(X509Certificate2 certificate)
        {
            string name = GetCertificateNickname(certificate);
            Process process = Process.Start(new ProcessStartInfo()
            {
                FileName = "cerutil",
                ArgumentList = { "-d", DatabasePath, "-L", "-n", name, "-a" },
                RedirectStandardOutput = true,
                RedirectStandardError = true
            })!;
            string stdout = process.StandardOutput.ReadToEnd();
            process.WaitForExit();
            if (process.ExitCode == 0)
            {
                stdout = stdout.Replace("\r\n", "\n");
                const string BeginCertificate = "-----BEGIN CERTIFICATE-----";
                var pemCertificates = stdout.Split(BeginCertificate, StringSplitOptions.RemoveEmptyEntries);
                foreach (var pem in pemCertificates)
                {
                    X509Certificate2 cert = X509Certificate2.CreateFromPem(BeginCertificate + "\n" + pem);
                    if (cert.Equals(certificate))
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        public override void DeleteCertificate(X509Certificate2 certificate)
        {
            string name = GetCertificateNickname(certificate);
            var process = Process.Start(new ProcessStartInfo()
            {
                FileName = "certutil",
                ArgumentList = { "-d", DatabasePath, "-D", "-n", name },
                RedirectStandardOutput = true,
                RedirectStandardError = true
            })!;
            process.WaitForExit();
        }

        private string GetCertificateNickname(X509Certificate2 certificate)
            => "aspnet-" + certificate.Thumbprint;
    }
}