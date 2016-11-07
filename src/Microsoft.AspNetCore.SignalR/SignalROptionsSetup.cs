// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Options;

namespace Microsoft.Extensions.DependencyInjection
{
    public class SignalROptionsSetup : IConfigureOptions<SignalROptions>
    {
        public void Configure(SignalROptions options)
        {
            options.RegisterInvocationAdapter<JsonNetInvocationAdapter>("json");
        }
    }
}