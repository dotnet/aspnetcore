// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Reflection;
using System.Text.Json;

namespace Templates.Test.Helpers;

public readonly struct DevelopmentCertificate(string certificatePath, string certificatePassword, string certificateThumbprint)
{
    public readonly string CertificatePath { get; } = certificatePath;
    public readonly string CertificatePassword { get; } = certificatePassword;
    public readonly string CertificateThumbprint { get; } = certificateThumbprint;

    public static DevelopmentCertificate Get(Assembly assembly)
    {
        var json = Path.Combine(AppContext.BaseDirectory, "aspnetcore-https.json");
        if (!File.Exists(json))
        {
            throw new InvalidOperationException($"The aspnetcore-https.json file does not exist in {json}.");
        }

        using var file = File.OpenRead(json);
        var certificateAttributes = JsonSerializer.Deserialize<CertificateAttributes>(file) ??
            throw new InvalidOperationException($"The aspnetcore-https.json file does not contain valid JSON.");

        var path = Path.ChangeExtension(json, ".pfx");
        var password = certificateAttributes.Password;
        var thumbprint = certificateAttributes.Thumbprint;

        return new DevelopmentCertificate(path, password, thumbprint);
    }

    private class CertificateAttributes
    {
        public string Password { get; set; }
        public string Thumbprint { get; set; }
    }
}
