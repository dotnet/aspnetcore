// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net.Http;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.Authentication.Twitter;

/// <summary>
/// Used to setup defaults for all <see cref="TwitterOptions"/>.
/// </summary>
public class TwitterPostConfigureOptions : IPostConfigureOptions<TwitterOptions>
{
    private readonly IDataProtectionProvider _dp;

    /// <summary>
    /// Initializes the <see cref="TwitterPostConfigureOptions"/>.
    /// </summary>
    /// <param name="dataProtection">The <see cref="IDataProtectionProvider"/>.</param>
    public TwitterPostConfigureOptions(IDataProtectionProvider dataProtection)
    {
        _dp = dataProtection;
    }

    /// <summary>
    /// Invoked to post configure a TOptions instance.
    /// </summary>
    /// <param name="name">The name of the options instance being configured.</param>
    /// <param name="options">The options instance to configure.</param>
    public void PostConfigure(string? name, TwitterOptions options)
    {
        ArgumentNullException.ThrowIfNull(name);

        options.DataProtectionProvider = options.DataProtectionProvider ?? _dp;

        if (options.StateDataFormat == null)
        {
            var dataProtector = options.DataProtectionProvider.CreateProtector(
                typeof(TwitterHandler).FullName!, name, "v1");
            options.StateDataFormat = new SecureDataFormat<RequestToken>(
                new RequestTokenSerializer(),
                dataProtector);
        }

        if (options.Backchannel == null)
        {
            options.Backchannel = new HttpClient(options.BackchannelHttpHandler ?? new HttpClientHandler());
            options.Backchannel.Timeout = options.BackchannelTimeout;
            options.Backchannel.MaxResponseContentBufferSize = 1024 * 1024 * 10; // 10 MB
            options.Backchannel.DefaultRequestHeaders.Accept.ParseAdd("*/*");
            options.Backchannel.DefaultRequestHeaders.UserAgent.ParseAdd("Microsoft ASP.NET Core Twitter handler");
            options.Backchannel.DefaultRequestHeaders.ExpectContinue = false;
        }
    }
}
