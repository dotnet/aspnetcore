// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using Microsoft.AspNetCore.Http.Features;

namespace Microsoft.AspNetCore.Hosting;

/// <summary>
/// Default implementation for <see cref="IHttpActivityFeature"/>.
/// </summary>
internal sealed class HttpActivityFeature : IHttpActivityFeature
{
    internal HttpActivityFeature(Activity activity)
    {
        Activity = activity;
    }

    /// <inheritdoc />
    public Activity Activity { get; set; }
}
