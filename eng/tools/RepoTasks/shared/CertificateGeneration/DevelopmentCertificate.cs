// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Certificates.Generation;

namespace RepoTasks;

public readonly struct DevelopmentCertificate
{
    public DevelopmentCertificate(string certificatePath, string certificatePassword, string certificateThumbprint)
    {
        CertificatePath = certificatePath;
        CertificatePassword = certificatePassword;
        CertificateThumbprint = certificateThumbprint;
    }

    public readonly string CertificatePath { get; }
    public readonly string CertificatePassword { get; }
    public readonly string CertificateThumbprint { get; }

    public static DevelopmentCertificate Create(string certificatePath)
    {
        var certificatePassword = "";
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
