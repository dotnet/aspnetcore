// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Testing;

namespace Interop.FunctionalTests;

[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = false)]
public class SkipIfChromeUnavailableAttribute : Attribute, ITestCondition
{
    public bool IsMet
    {
        get
        {
            // Skip if Jenkins
            if (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable("JENKINS_HOME")))
            {
                return false;
            }

            // Otherwise, run in all CI scenarios
            if (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable("CI")))
            {
                return true;
            }

            // That just leaves local runs - check if chrome is available
            if (OperatingSystem.IsLinux())
            {
                return File.Exists(Path.Combine("/usr", "bin", "google-chrome"));
            }

            if (OperatingSystem.IsWindows())
            {
                return File.Exists(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "Google", "Chrome", "Application", "chrome.exe")) ||
                    File.Exists(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Google", "Chrome", "Application", "chrome.exe"));
            }
            
            throw new PlatformNotSupportedException();
        }
    }

    public string SkipReason => "This is running on Jenkins or Chrome/Chromium is not installed and this is a dev environment.";
}
