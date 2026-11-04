// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.DataProtection;
using System;
using System.IO;
using System.Security.Cryptography.X509Certificates;

var keyDirectory = new DirectoryInfo(AppContext.BaseDirectory);
File.WriteAllText(Path.Combine(keyDirectory.FullName, Constants.KeyRingXmlFileName), Constants.KeyRingXmlContents);

var cert = new X509Certificate2(Convert.FromBase64String(Constants.Key), Constants.Password);
var dpProvider = DataProtectionProvider.Create(keyDirectory, cert);
var protector = dpProvider.CreateProtector(purpose: "Test trimming");

var protectedSecret = @"CfDJ8IjUFZwXRKtJrjntLzap6-OgblGi63sK6HDtOtu-IVhtuoLSTJl4fIbwX4vCtc8fefqPrr41QzGjHXwP-1HaCi9qlJFjvaloQ5KFPxBO2s-s1cAK9I5kl-lfjhyYrEtJRNtvgawKREAp2cZ9udM_Kog";
var unprotectedSecret = protector.Unprotect(protectedSecret);

if (unprotectedSecret != "This is a secret.")
{
    return -1;
}

return 100;
