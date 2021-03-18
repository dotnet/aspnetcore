// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using Microsoft.AspNetCore.Certificates.Generation;

namespace Templates.Test.Helpers
{
    public class DevelopmentCertificate
    {
        public DevelopmentCertificate(string certificatePath, string certificatePassword, string certificateThumbprint)
        {
            CertificatePath = certificatePath;
            CertificatePassword = certificatePassword;
            CertificateThumbprint = certificateThumbprint;
        }

        public string CertificatePath { get; }
        public string CertificatePassword { get; }
        public string CertificateThumbprint { get; }

        public static DevelopmentCertificate Create(string workingDirectory)
        {
            var certificatePath = Path.Combine(workingDirectory, $"{Guid.NewGuid()}.pfx");
            var certificatePassword = Guid.NewGuid().ToString();
            var certificateThumbprint = EnsureDevelopmentCertificates(certificatePath, certificatePassword);

            return new DevelopmentCertificate(certificatePath, certificatePassword, certificateThumbprint);
        }

        private static string EnsureDevelopmentCertificates(string certificatePath, string certificatePassword)
        {
            var now = DateTimeOffset.Now;
            var manager = CertificateManager.Instance;
            var certificate = manager.CreateAspNetCoreHttpsDevelopmentCertificate(now, now.AddYears(1));
            var certificateThumbprint = certificate.Thumbprint;
            manager.ExportCertificate(certificate, path: certificatePath, includePrivateKey: true, certificatePassword, CertificateKeyExportFormat.Pfx);

            return certificateThumbprint;
        }
    }
}
