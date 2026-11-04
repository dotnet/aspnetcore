// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.InteropServices;
using Microsoft.AspNetCore.DataProtection;

namespace NonDISample;

public class Program
{
    public static void Main(string[] args)
    {
        var keysFolder = Path.Combine(Directory.GetCurrentDirectory(), "temp-keys");

        // instantiate the data protection system at this folder
        var dataProtectionProvider = DataProtectionProvider.Create(
            new DirectoryInfo(keysFolder),
            configuration =>
            {
                configuration.SetApplicationName("my app name");
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    configuration.ProtectKeysWithDpapi();
                }
            });

        var protector = dataProtectionProvider.CreateProtector("Program.No-DI");

        // protect the payload
        var protectedPayload = protector.Protect("Hello World!");
        Console.WriteLine($"Protect returned: {protectedPayload}");

        // unprotect the payload
        var unprotectedPayload = protector.Unprotect(protectedPayload);
        Console.WriteLine($"Unprotect returned: {unprotectedPayload}");
    }
}
