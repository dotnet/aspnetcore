// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.DataProtection.Internal;

internal sealed class DataProtectionOptionsSetup : IConfigureOptions<DataProtectionOptions>
{
    private readonly IServiceProvider _services;

    public DataProtectionOptionsSetup(IServiceProvider provider)
    {
        _services = provider;
    }

    public void Configure(DataProtectionOptions options)
    {
        options.ApplicationDiscriminator = _services.GetApplicationUniqueIdentifier();
    }
}
