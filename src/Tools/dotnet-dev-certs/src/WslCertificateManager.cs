// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Certificates.Generation;

namespace Microsoft.AspNetCore.Certificates.Generation;

internal sealed class WslCertificateManager : WindowsCertificateManager
{
    public WslCertificateManager()
    {
        TempFolder = Path.Combine(Path.GetTempPath(), "dotnet-dev-certs").Replace("\\", "/");
        StandaloneToolPath = Path.Combine(TempFolder, "publish", "WslCertificates").Replace("\\", "/");
        UnpackContents();
    }

    private void UnpackContents()
    {
        //Debugger.Launch();
        if (Directory.Exists(TempFolder))
        {
            Directory.Delete(TempFolder, true);
        }

        Directory.CreateDirectory(TempFolder);

        UnpackFile("scripts.check-prerequisites.sh", "check-prerequisites.sh");
        UnpackFile("scripts.check-trust.sh", "check-trust.sh");
        UnpackFile("scripts.trust-certificate.sh", "trust-certificate.sh");
        UnpackFile("scripts.untrust-certificate.sh", "untrust-certificate.sh");
        UnpackFile("scripts.WslProject.txt", "WslCertificates.csproj");
        UnpackFile("scripts.WslStub.txt", "WslCertificates.cs");

        var publishFolder = Path.Combine(TempFolder, "publish");

        var info = new ProcessStartInfo(
            "dotnet",
            $"publish -o {publishFolder}")
        {
            WorkingDirectory = TempFolder
        };

        var buildStandaloneTool = Process.Start(info);
        buildStandaloneTool.WaitForExit();

        void UnpackFile(string resourceName, string fileName)
        {
            using var resource = typeof(WslCertificateManager)
                .Assembly
                .GetManifestResourceStream($"Microsoft.AspNetCore.DeveloperCertificates.Tools.{resourceName}");

            using var fileStream = File.OpenWrite(Path.Combine(TempFolder, fileName));
            fileStream.SetLength(0);
            resource.CopyTo(fileStream);
        }
    }

    public string TempFolder { get; }

    public string StandaloneToolPath { get; set; }

    public override bool IsTrusted(X509Certificate2 certificate)
    {
#pragma warning disable CA1416 // Validate platform compatibility
        if (!base.IsTrusted(certificate))
        {
            return false;
        }
#pragma warning restore CA1416 // Validate platform compatibility

        if (!RunWslScript("check-prerequisites.sh", "bash", ""))
        {
            return false;
        }

        var path = EnsureCertificateFile(certificate);
        return RunWslScript("check-trust.sh", "bash", WslPath(path));
    }

    private string EnsureCertificateFile(X509Certificate2 certificate)
    {
        var path = Path.Combine(TempFolder, $"aspnetcore-localhost-{certificate.Thumbprint}.crt").Replace("\\", "/");
        if (!File.Exists(path))
        {
            File.WriteAllText(path, certificate.ExportCertificatePem());
        }

        return path;
    }

    private bool RunWslScript(string scriptName, string command, string scriptArguments)
    {
        var info = new ProcessStartInfo("wsl", $"{command} {scriptName} {scriptArguments}");
        info.WorkingDirectory = TempFolder;
        using var process = Process.Start(info);
        process.WaitForExit();
        return process.ExitCode == 0;
    }

    private static string WslPath(string windowsPath) => $"$(wslpath {windowsPath})";

    protected override void RemoveCertificateFromTrustedRoots(X509Certificate2 certificate)
    {
#pragma warning disable CA1416 // Validate platform compatibility
        base.RemoveCertificateFromTrustedRoots(certificate);
#pragma warning restore CA1416 // Validate platform compatibility
        var path = EnsureCertificateFile(certificate);
        RunWslScript("untrust-certificate.sh", "sudo bash", WslPath(Path.GetFileName(path)));
    }

    protected override void RemoveCertificateFromUserStore(X509Certificate2 certificate)
    {
        base.RemoveCertificateFromUserStore(certificate);
        var path = EnsureCertificateFile(certificate);
        RunWslScript(WslPath(StandaloneToolPath), "", $"--clean {WslPath(path)}");
    }

    protected override X509Certificate2 SaveCertificateCore(X509Certificate2 certificate, StoreName storeName, StoreLocation storeLocation)
    {
#pragma warning disable CA1416 // Validate platform compatibility
        var result = base.SaveCertificateCore(certificate, storeName, storeLocation);
#pragma warning restore CA1416 // Validate platform compatibility
        if (storeName == StoreName.My && storeLocation == StoreLocation.CurrentUser)
        {
            var path = EnsureCertificateFile(result);
            RunWslScript(WslPath(StandaloneToolPath), "", $"--install {WslPath(path)}");
        }

        return result;
    }

    protected override void TrustCertificateCore(X509Certificate2 certificate)
    {
#pragma warning disable CA1416 // Validate platform compatibility
        base.TrustCertificateCore(certificate);
#pragma warning restore CA1416 // Validate platform compatibility
        if (!RunWslScript("check-prerequisites.sh", "bash", ""))
        {
            return;
        }

        var path = EnsureCertificateFile(certificate);
        RunWslScript("trust-certificate.sh", "sudo bash", $"{WslPath(path)}");
    }
}
