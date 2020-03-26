using System;
using Microsoft.AspNetCore.Certificates.Generation;

namespace Microsoft.AspNetCore.DeveloperCertificates.XPlat
{
    public static class CertificateGenerator
    {
        public static void GenerateAspNetHttpsCertificate()
        {
            var manager = new CertificateManager();
            var now = DateTimeOffset.Now;
            manager.EnsureAspNetCoreHttpsDevelopmentCertificate(now, now.AddYears(1), isInteractive: false);
        }
    }
}
