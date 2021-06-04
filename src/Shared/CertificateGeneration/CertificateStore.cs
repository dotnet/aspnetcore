using System;
using System.Security.Cryptography.X509Certificates;
using Microsoft.Extensions.Tools.Internal;

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

        public abstract bool CheckDependencies(IReporter? reporter);

        public abstract bool TryInstallCertificate(string name, PemCertificateFile pemFile, IReporter? reporter, bool isInteractive);

        public abstract void DeleteCertificate(string name, IReporter? reporter, bool isInteractive);

        public abstract bool HasCertificate(string name, X509Certificate2 pemContent);

        protected bool CheckProgramDependency(string program, IReporter? reporter)
        {
            if (!ProcessRunner.HasProgram(program))
            {
                reporter?.Warn($"Cannot use '{StoreName}' because '{program}' is not installed.");
                return false;
            }
            return true;
        }

        protected bool CheckProgramDependency(ProcessRunOptions runOptions, IReporter? reporter)
        {
            return CheckProgramDependency(runOptions.Command[0], reporter)
                & (!runOptions.Elevate || CheckProgramDependency("sudo", reporter));
        }

        protected bool ContainsCertificate(string storeContent, string certificateContent)
        {
            return false;
        }
    }
}