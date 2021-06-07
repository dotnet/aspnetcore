using System;
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

        public abstract bool CheckDependencies();

        public abstract bool TryInstallCertificate(X509Certificate2 certificate);

        public abstract void DeleteCertificate(X509Certificate2 certificate);

        public abstract bool HasCertificate(X509Certificate2 certificate);

        protected bool CheckProgramDependency(string program)
        {
            if (!ProcessRunner.HasProgram(program))
            {
                // TODO reporter?.Warn($"Cannot use '{StoreName}' because '{program}' is not installed.");
                return false;
            }
            return true;
        }

        protected bool CheckProgramDependency(ProcessRunOptions runOptions)
        {
            return CheckProgramDependency(runOptions.Command[0])
                & (!runOptions.Elevate || CheckProgramDependency("sudo"));
        }

        protected bool ContainsCertificate(string storeContent, string certificateContent)
        {
            return false;
        }
    }
}