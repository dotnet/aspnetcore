// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.Identity;

// Set TimeProvider from DI on all options instances, if not already set by tests.
internal sealed class PostConfigureDataProtectionTokenProviderOptions : IPostConfigureOptions<DataProtectionTokenProviderOptions>
{
    public PostConfigureDataProtectionTokenProviderOptions(TimeProvider? timeProvider = null)
    {
        TimeProvider = timeProvider;
    }

    private TimeProvider? TimeProvider { get; }

    public void PostConfigure(string? name, DataProtectionTokenProviderOptions options)
    {
        options.TimeProvider ??= TimeProvider;
    }
}
