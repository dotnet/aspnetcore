// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Linq;
using System.Threading;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

namespace RepoTasks;

/// <summary>
/// Generates the test HTTPs certificate used by the template tests
/// </summary>
public class GenerateTestDevCert : Task
{
    [Required]
    public string Path { get; private set; }

    [Output]
    public string Password { get; private set; }

    [Output]
    public string Thumbprint { get; private set; }

    public override bool Execute()
    {
        Mutex mutex = null;
        try
        {
            // MSBuild will potentially invoke this task in parallel across different subprocesses/nodes.
            // The build is configured to generate the certificate in a single location, but multiple projects
            // import the same targets that will use this task, which will result in multiple calls.
            // To avoid issues where we try to generate multiple certificates on the same location, we wrap the
            // usage in a named mutex which guarantees that only one instance will run at a time.
            mutex = new(initiallyOwned: true, "Global\\GenerateTestDevCert", out var createdNew);
            if (!createdNew)
            {
                // The mutex already exists, wait for it to be released.
                mutex.WaitOne();
            }

            if (File.Exists(Path))
            {
                Log.LogMessage(MessageImportance.Normal, $"A test certificate already exists at {Path}");
                return true;
            }

            var cert = DevelopmentCertificate.Create(Path);
            Password = cert.CertificatePassword;
            Thumbprint = cert.CertificateThumbprint;
        }
        catch (Exception e)
        {
            Log.LogErrorFromException(e, showStackTrace: true);
        }
        finally
        {
            mutex.ReleaseMutex();
        }

        return !Log.HasLoggedErrors;
    }
}
