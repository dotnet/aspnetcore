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
        string[] locations = [
            Path.Combine(AppContext.BaseDirectory, "aspnetcore-https.json"),
            Path.Combine(Environment.CurrentDirectory, "aspnetcore-https.json"),
            Path.Combine(AppContext.BaseDirectory, "aspnetcore-https.json"),
        ];

        var json = TryGetExistingFile(locations)
            ?? throw new InvalidOperationException($"The aspnetcore-https.json file does not exist. Searched locations: {Environment.NewLine}{string.Join(Environment.NewLine, locations)}");

        using var file = File.OpenRead(json);
        var certificateAttributes = JsonSerializer.Deserialize<CertificateAttributes>(file) ??
            throw new InvalidOperationException($"The aspnetcore-https.json file does not contain valid JSON.");

        var path = Path.ChangeExtension(json, ".pfx");

        if (!File.Exists(path))
        {
            throw new InvalidOperationException($"The certificate file does not exist. Expected at: '{path}'.");
        }

        var password = certificateAttributes.Password;
        var thumbprint = certificateAttributes.Thumbprint;

        return new DevelopmentCertificate(path, password, thumbprint);
    }

    private static string TryGetExistingFile(string[] locations)
    {
        foreach (var location in locations)
        {
            if (File.Exists(location))
            {
                return location;
            }
        }

        return null;
    }

    private sealed class CertificateAttributes
    {
        public string Password { get; set; }
        public string Thumbprint { get; set; }
    }
}
