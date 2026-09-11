// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Components.AI.Tests.TestFramework;

internal sealed class ComponentRender
{
    internal ComponentRender(int batchIndex, string html)
    {
        BatchIndex = batchIndex;
        Html = html;
    }

    public int BatchIndex { get; }

    public string Html { get; }
}
