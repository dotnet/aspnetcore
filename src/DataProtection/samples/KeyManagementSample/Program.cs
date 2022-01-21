// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.InteropServices;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.DataProtection.KeyManagement;
using Microsoft.Extensions.DependencyInjection;

namespace KeyManagementSample;

public class Program
{
    public static void Main(string[] args)
    {
        var keysFolder = Path.Combine(Directory.GetCurrentDirectory(), "temp-keys");
        var serviceCollection = new ServiceCollection();
        var builder = serviceCollection
            .AddDataProtection()
            // point at a specific folder and use DPAPI to encrypt keys
            .PersistKeysToFileSystem(new DirectoryInfo(keysFolder));
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            builder.ProtectKeysWithDpapi();
        }

        using (var services = serviceCollection.BuildServiceProvider())
        {
            // perform a protect operation to force the system to put at least
            // one key in the key ring
            services.GetDataProtector("Sample.KeyManager.v1").Protect("payload");
            Console.WriteLine("Performed a protect operation.");

            // get a reference to the key manager
            var keyManager = services.GetService<IKeyManager>();

            // list all keys in the key ring
            var allKeys = keyManager.GetAllKeys();
            Console.WriteLine($"The key ring contains {allKeys.Count} key(s).");
            foreach (var key in allKeys)
            {
                Console.WriteLine($"Key {key.KeyId:B}: Created = {key.CreationDate:u}, IsRevoked = {key.IsRevoked}");
            }

            // revoke all keys in the key ring
            keyManager.RevokeAllKeys(DateTimeOffset.Now, reason: "Revocation reason here.");
            Console.WriteLine("Revoked all existing keys.");

            // add a new key to the key ring with immediate activation and a 1-month expiration
            keyManager.CreateNewKey(
                activationDate: DateTimeOffset.Now,
                expirationDate: DateTimeOffset.Now.AddMonths(1));
            Console.WriteLine("Added a new key.");

            // list all keys in the key ring
            allKeys = keyManager.GetAllKeys();
            Console.WriteLine($"The key ring contains {allKeys.Count} key(s).");
            foreach (var key in allKeys)
            {
                Console.WriteLine($"Key {key.KeyId:B}: Created = {key.CreationDate:u}, IsRevoked = {key.IsRevoked}");
            }
        }
    }
}
