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

        public abstract bool TryInstallCertificate(string name, PemCertificateFile pemFile);

        public abstract void DeleteCertificate(string name);

        public abstract bool HasCertificate(string name, X509Certificate2 pemContent);

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