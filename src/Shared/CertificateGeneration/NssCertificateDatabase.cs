using System;
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

        public override bool CheckDependencies()
        {
            return CheckProgramDependency("certutil");
        }

        public override bool TryInstallCertificate(X509Certificate2 certificate)
        {
            string pemFile = Paths.GetUserTempFile(".pem");
            try
            {
                CertificateManager.ExportCertificate(certificate, pemFile, includePrivateKey: false, password: null, CertificateKeyExportFormat.Pem);
                string name = GetCertificateNickname(certificate);
                var result = ProcessRunner.Run(new() {
                    Command = { "certutil", "-d", DatabasePath, "-A", "-t", "C,,", "-n", name, "-i", pemFile },
                    ThrowOnFailure = false });
                if (!result.IsSuccess)
                {
                    CertificateManager.Log.LinuxCertutilInstallFailed(result.CommandLine, result.ExitCode, result.StandardError!.ToString());
                }
                return result.IsSuccess;
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
            ProcessRunResult runResult = ProcessRunner.Run(new()
            {
                Command = { "certutil", "-d", DatabasePath, "-L", "-n", name, "-a" },
                ReadStandardOutput = true,
                ThrowOnFailure = false
            });
            if (runResult.IsSuccess)
            {
                runResult.StandardOutput!.Replace("\r\n", "\n");
                const string BeginCertificate = "-----BEGIN CERTIFICATE-----";
                var pemCertificates = runResult.StandardOutput.ToString()
                    .Split(BeginCertificate, StringSplitOptions.RemoveEmptyEntries);
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
            ProcessRunner.Run(new()
            {
                Command = { "certutil", "-d", DatabasePath, "-D", "-n", name },
                ThrowOnFailure = false
            });
        }

        private string GetCertificateNickname(X509Certificate2 certificate)
            => "aspnet-" + certificate.Thumbprint;
    }
}