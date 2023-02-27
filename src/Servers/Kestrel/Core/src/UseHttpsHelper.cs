// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Server.Kestrel.Core;

namespace Microsoft.AspNetCore.Hosting;

internal sealed class UseHttpsHelper : IUseHttpsHelper // TODO (acasey): name
{
    public ListenOptions UseHttps(ListenOptions listenOptions)
    {
        return listenOptions.UseHttps();
    }
}
