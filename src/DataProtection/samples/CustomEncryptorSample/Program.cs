// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace CustomEncryptorSample;

public class Program
{
    public static void Main(string[] args)
    {
        var keysFolder = Path.Combine(Directory.GetCurrentDirectory(), "temp-keys");
        using (var services = new ServiceCollection()
            .AddLogging(o => o.AddConsole().SetMinimumLevel(LogLevel.Debug))
            .AddDataProtection()
            .PersistKeysToFileSystem(new DirectoryInfo(keysFolder))
            .UseXmlEncryptor(s => new CustomXmlEncryptor(s))
            .Services.BuildServiceProvider())
        {
            var protector = services.GetDataProtector("SamplePurpose");

            // protect the payload
            var protectedPayload = protector.Protect("Hello World!");
            Console.WriteLine($"Protect returned: {protectedPayload}");

            // unprotect the payload
            var unprotectedPayload = protector.Unprotect(protectedPayload);
            Console.WriteLine($"Unprotect returned: {unprotectedPayload}");
        }
    }
}
