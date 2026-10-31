// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Builder;

var builder = WebApplication.CreateSlimBuilder(args);
var app = builder.Build();

if (X509Utilities.HasCertificateType)
{
    return -1;
}

return 100; // Success