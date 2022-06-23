// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;

namespace Microsoft.AspNetCore.RateLimiting;
internal class PolicyTypeInfo
{
    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)]
    public required Type PolicyType { get; init; }
    public required Type PartitionKeyType { get; init; }
}
