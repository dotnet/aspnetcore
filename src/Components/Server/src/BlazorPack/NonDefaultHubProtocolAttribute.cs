// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.SignalR.Internal;

// Tells SignalR not to add the IHubProtocol with this attribute to all hubs by default
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
internal sealed class NonDefaultHubProtocolAttribute : Attribute
{
}
