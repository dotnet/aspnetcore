// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Sets up MVC default options for <see cref="ProblemDetailsOptions"/>.
/// </summary>
internal sealed class MvcCoreProblemDetailsOptionsSetup : IConfigureOptions<ProblemDetailsOptions>
{
    private readonly ApiBehaviorOptions _apiBehaviorOptions;

    public MvcCoreProblemDetailsOptionsSetup(IOptions<ApiBehaviorOptions> options)
    {
        _apiBehaviorOptions = options.Value;
    }

    /// <summary>
    /// Configures the <see cref="ProblemDetailsOptions"/>.
    /// </summary>
    /// <param name="options">The <see cref="ProblemDetailsOptions"/>.</param>
    public void Configure(ProblemDetailsOptions options)
    {
        ArgumentNullException.ThrowIfNull(nameof(options));

        options.SuppressMapClientErrors = _apiBehaviorOptions.SuppressMapClientErrors;
        options.SuppressMapExceptions = _apiBehaviorOptions.SuppressMapClientErrors;

        foreach (var item in _apiBehaviorOptions.ClientErrorMapping)
        {
            options.ProblemDetailsErrorMapping[item.Key] = new()
            {
                Title = item.Value.Title,
                Link = item.Value.Link
            };
        }
    }
}
