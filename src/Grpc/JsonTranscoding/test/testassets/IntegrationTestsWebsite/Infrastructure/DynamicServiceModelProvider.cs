// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using Grpc.AspNetCore.Server.Model;

namespace IntegrationTestsWebsite.Infrastructure;

public class DynamicServiceModelProvider : IServiceMethodProvider<DynamicService>
{
    public Action<ServiceMethodProviderContext<DynamicService>>? CreateMethod { get; set; }

    public void OnServiceMethodDiscovery(ServiceMethodProviderContext<DynamicService> context)
    {
        Debug.Assert(CreateMethod != null);

        CreateMethod(context);
    }
}
