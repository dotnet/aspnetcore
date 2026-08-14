// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma warning disable ASP0031 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.

using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.Authentication.Dbsc;

internal sealed class PostConfigureDbscOptions : IPostConfigureOptions<DbscOptions>
{
    private readonly IOptions<DbscSourceSchemes> _sourceSchemes;

    public PostConfigureDbscOptions(IOptions<DbscSourceSchemes> sourceSchemes)
    {
        _sourceSchemes = sourceSchemes;
    }

    public void PostConfigure(string? name, DbscOptions options)
    {
        ArgumentNullException.ThrowIfNull(name);

        var sourceSchemes = _sourceSchemes.Value;
        if (!sourceSchemes.DbscSchemes.Contains(name))
        {
            return;
        }

        options.Validate(name);
        sourceSchemes.ClaimSourceScheme(name, options.SourceScheme);
    }
}