// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.AspNetCore.Components.Server.BlazorPack
{
    // Tells SignalR not to add the IHubProtocol with this attribute to all hubs by default
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
    internal class NonDefaultHubProtocol : Attribute
    {
    }
}
