// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Reflection;

namespace Templates.Test.Helpers;

public readonly struct DevelopmentCertificate(string certificatePath, string certificatePassword, string certificateThumbprint)
{
    public readonly string CertificatePath { get; } = certificatePath;
    public readonly string CertificatePassword { get; } = certificatePassword;
    public readonly string CertificateThumbprint { get; } = certificateThumbprint;

    public static DevelopmentCertificate Get(Assembly assembly)
    {
        // Read the assembly metadata attributes
        // DevCertPath, DevCertPassword, DevCertThumbprint
        string path = null;
        string password = null;
        string thumbprint = null;
        foreach (var attribute in assembly.GetCustomAttributes<AssemblyMetadataAttribute>())
        {
            if (attribute.Key == "DevCertPath")
            {
                path = attribute.Value;
            }
            else if (attribute.Key == "DevCertPassword")
            {
                password = attribute.Value;
            }
            else if (attribute.Key == "DevCertThumbprint")
            {
                thumbprint = attribute.Value;
            }
        }

        return path == null || password == null || thumbprint == null
            ? throw new InvalidOperationException("The assembly does not contain the required metadata attributes.")
            : new DevelopmentCertificate(path, password, thumbprint);
    }
}
