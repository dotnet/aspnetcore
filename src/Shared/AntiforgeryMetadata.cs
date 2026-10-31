// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using Microsoft.AspNetCore.Antiforgery;

namespace Microsoft.AspNetCore.Http.Metadata;

internal sealed class AntiforgeryMetadata(bool required) : IAntiforgeryMetadata
{
    public static readonly IAntiforgeryMetadata ValidationRequired = new AntiforgeryMetadata(true);
    public static readonly IAntiforgeryMetadata ValidationNotRequired = new AntiforgeryMetadata(false);

    public bool RequiresValidation { get; } = required;
}
