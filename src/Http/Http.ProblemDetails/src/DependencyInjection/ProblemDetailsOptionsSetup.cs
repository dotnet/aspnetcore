// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

namespace Microsoft.Extensions.DependencyInjection;

internal sealed class ProblemDetailsOptionsSetup : IConfigureOptions<ProblemDetailsOptions>
{
    public void Configure(ProblemDetailsOptions options)
    {
        ArgumentNullException.ThrowIfNull(nameof(options));
        ConfigureProblemDetailsErrorMapping(options);
    }

    // Internal for unit testing
    internal static void ConfigureProblemDetailsErrorMapping(ProblemDetailsOptions options)
    {
        foreach (var (statusCode, value) in ProblemDetailsDefaults.Defaults)
        {
            options.ProblemDetailsErrorMapping[statusCode] = new()
            {
                Link = value.Type,
                Title = value.Title,
            };
        }
    }
}
