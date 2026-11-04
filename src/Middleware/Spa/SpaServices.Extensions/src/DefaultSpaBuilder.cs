// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Builder;

namespace Microsoft.AspNetCore.SpaServices;

internal sealed class DefaultSpaBuilder : ISpaBuilder
{
    public IApplicationBuilder ApplicationBuilder { get; }

    public SpaOptions Options { get; }

    public DefaultSpaBuilder(IApplicationBuilder applicationBuilder, SpaOptions options)
    {
        ApplicationBuilder = applicationBuilder
            ?? throw new ArgumentNullException(nameof(applicationBuilder));

        Options = options
            ?? throw new ArgumentNullException(nameof(options));
    }
}
