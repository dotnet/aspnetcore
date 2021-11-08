// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using Microsoft.AspNetCore.Testing;

namespace Interop.FunctionalTests;

[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = false)]
public class SkipIfChromeUnavailableAttribute : Attribute, ITestCondition
{
    public bool IsMet => string.IsNullOrEmpty(Environment.GetEnvironmentVariable("JENKINS_HOME")) && (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable("CI")) || File.Exists(ResolveChromeExecutablePath()));

    public string SkipReason => "This is running on Jenkins or Chrome/Chromium is not installed and this is a dev environment.";

    private static string ResolveChromeExecutablePath()
    {
        if (OperatingSystem.IsWindows())
        {
            return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "Google", "Chrome", "Application", "chrome.exe");
        }
        else if (OperatingSystem.IsLinux())
        {
            return Path.Combine("/usr", "bin", "google-chrome");
        }

        throw new PlatformNotSupportedException();
    }
}
