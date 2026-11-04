// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Mvc.ApplicationModels;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Options;

namespace Microsoft.Extensions.DependencyInjection;

internal sealed class RazorPagesOptionsSetup : IConfigureOptions<RazorPagesOptions>
{
    private readonly IServiceProvider _serviceProvider;

    public RazorPagesOptionsSetup(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
    }

    public void Configure(RazorPagesOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        options.Conventions = new PageConventionCollection(_serviceProvider);
    }
}
