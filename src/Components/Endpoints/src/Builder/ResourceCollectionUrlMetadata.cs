﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Components.Endpoints;

internal class ResourceCollectionUrlMetadata(string url)
{
    public string Url { get; } = url;
}
